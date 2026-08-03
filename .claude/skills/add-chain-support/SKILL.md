---
name: add-chain-support
description: Use when asked to add ARC76 account derivation support for a new blockchain/chain (e.g. Solana, Bitcoin, another EVM-compatible variant) to the ARC76Account library. Walks through the files that must change together, following the existing Algorand/Ethereum per-chain-namespace pattern.
---

# Add chain support to ARC76Account

This library derives deterministic accounts for multiple chains from the same ARC76 seed
(PBKDF2-HMAC-SHA256, shared in `ARC76Account/Core/Arc76Seed.cs`). Each chain lives in its own
namespace (`ARC76Account.Algorand`, `ARC76Account.Ethereum`, ...) with a static `ARC76` class
exposing `GetAccount`/`GetEmailAccount`. Adding a new chain means adding one more
namespace/class following that exact shape — never invent a new derivation scheme, and never
duplicate the PBKDF2 logic per chain.

## Steps

1. **Find the chain's key-from-seed constructor.** Look for a minimal signer package
   (analogous to `Nethereum.Signer.EthECKey` for EVM) that can build a keypair directly from a
   32-byte seed/private key, without pulling in a full node SDK. Add it as a
   `PackageReference` in `ARC76Account/ARC76Account.csproj`.

2. **Create `ARC76Account/<Chain>/ARC76.cs`** with namespace `ARC76Account.<Chain>`, following
   `ARC76Account/Ethereum/ARC76.cs` as the template:
   ```c#
   using ARC76Account.Core;

   namespace ARC76Account.<Chain>
   {
       public static class ARC76
       {
           public static <ChainKeyType> GetAccount(string password, int slot = 0)
           {
               var seed = Arc76Seed.Derive(password, slot);
               return /* build chain key from seed */;
           }

           public static <ChainKeyType> GetEmailAccount(string email, string password, int slot = 0)
           {
               var seed = Arc76Seed.Derive(email, password, slot);
               return /* build chain key from seed */;
           }
       }
   }
   ```
   - Call `Arc76Seed.Derive(...)` — do not re-implement the PBKDF2 call inline.
   - Keep the `slot = 0` default parameter.
   - **Namespace collision check**: if the chain's SDK root namespace has the same simple name
     you're about to give the new folder/namespace (as happens with `Algorand` — see
     `AGENTS.md`'s "Namespace gotcha" section), either pick a non-colliding namespace name or
     qualify SDK references with `global::` inside that file. Verify with a build, not just by
     reading the code.

3. **Add known-answer tests** in a new `TestARC76/<Chain>Tests.cs` file, mirroring
   `EVMTests.cs`: one test for the password variant, one for the email variant, asserting the
   derived public address and private key against fixed expected values for the test password
   `"12345678901234567890123456789012345678901234567890"` (the convention used everywhere else
   in this repo). Reference the new chain via its full namespace, e.g.
   `ARC76Account.<Chain>.ARC76.GetAccount(...)`. Run `dotnet test` and copy the actual output
   into the assertions rather than hand-computing expected values — these are regression tests,
   not spec tests.

4. **Update `README.md`**:
   - Add a row to the "Supported chains" table.
   - Add a usage snippet in the same style as the existing "EVM" section (code block +
     `Assert.That` example lines showing real derived values).

5. **Double check nothing else changed.** `ARC76Account.Algorand.ARC76.*` and
   `ARC76Account.Ethereum.ARC76.*` must be byte-for-byte unchanged — a diff touching them is a
   sign the new chain's code was copy-pasted over the wrong file rather than added alongside it.

## Non-goals

- Don't add a generic chain-agnostic interface/factory across `ARC76Account.<Chain>.ARC76`
  classes unless explicitly asked — the flat, per-namespace pattern is deliberate (see
  `README.md` → "Adding a new chain").
- Don't touch `ARC76Account/Core/Arc76Seed.cs`'s PBKDF2 parameters (iterations, hash, seed
  size) — those are shared across all chains and are part of the ARC76 spec; changing them
  breaks every chain's addresses at once.
