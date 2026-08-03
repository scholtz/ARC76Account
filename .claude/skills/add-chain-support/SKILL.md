---
name: add-chain-support
description: Use when asked to add ARC76 account derivation support for a new blockchain/chain (e.g. Solana, Bitcoin, another EVM-compatible variant) to AlgorandARC76AccountDotNet. Walks through the four places that must change together, following the existing AVM/EVM pattern.
---

# Add chain support to AlgorandARC76AccountDotNet

This library derives deterministic accounts for multiple chains from the same ARC76 seed
(PBKDF2-HMAC-SHA256 over `ARC-0076-...-PBKDF2-999999` strings). Adding a new chain means
plugging a new key type onto that same seed — never invent a new derivation scheme.

## Steps

1. **Find the chain's key-from-seed constructor.** Look for a minimal signer package
   (analogous to `Nethereum.Signer.EthECKey` for EVM) that can build a keypair directly from a
   32-byte seed/private key, without pulling in a full node SDK. Add it as a
   `PackageReference` in `AlgorandARC76AccountDotNet.csproj`.

2. **Add two methods to `ARC76.cs`** (password-only and email variants), copying the exact
   shape of `GetEVMAccount`/`GetEVMEmailAccount`:
   - Same salt/input string format: password variant uses
     `$"ARC-0076-{slot}-PBKDF2-999999"` salt and `$"ARC-0076-{password}-{slot}-PBKDF2-999999"`
     input; email variant folds `{email}` into both salt and input.
   - Same `Rfc2898DeriveBytes(input, salt, 999999, HashAlgorithmName.SHA256)` call, same
     `HASH_SIZE` (32) output.
   - Method names: `Get<Chain>Account` / `Get<Chain>EmailAccount`, matching the
     `GetEVMAccount`/`GetEVMEmailAccount` naming.
   - Keep the `slot = 0` default parameter.

3. **Add known-answer tests** in a new `TestARC76/<Chain>Tests.cs` file, mirroring
   `EVMTests.cs`: one test for the password variant, one for the email variant, asserting the
   derived public address and private key against fixed expected values for the test password
   `"12345678901234567890123456789012345678901234567890"` (the convention used everywhere else
   in this repo). Run `dotnet test` and copy the actual output into the assertions rather than
   hand-computing expected values — these are regression tests, not spec tests.

4. **Update `README.md`** with a usage snippet for the new chain in the same style as the
   existing "EVM ARC76 Account support" section (code block + `Assert.That` example lines showing
   real derived values).

5. **Double check nothing else changed.** `GetAccount`/`GetEmailAccount` (Algorand) and the
   existing EVM methods must be byte-for-byte unchanged — a diff touching them is a sign the
   new chain's code was copy-pasted over the wrong method rather than added alongside it.

## Non-goals

- Don't add a generic "multi-chain" abstraction/interface unless explicitly asked — the
  existing pattern is deliberately flat (one static method pair per chain), and that's fine
  for the number of chains this library supports.
- Don't touch PBKDF2 parameters (iterations, hash, `HASH_SIZE`) — those are shared across all
  chains and are part of the ARC76 spec; changing them breaks every chain's addresses at once.
