# AGENTS.md (.NET)

Instructions for AI coding agents (Claude Code, etc.) working on the **.NET** libraries in this
repo, which live under `dotnet/` (this file's directory). This repo also contains a TypeScript
implementation (`typescript/`, see `typescript/AGENTS.md`) and a planned Python one
(`python/`) — see the repo-root [`AGENTS.md`](../AGENTS.md) for the cross-language overview.
Paths below are relative to `dotnet/` unless stated otherwise.

## Project overview

`ARC76Account` implements the
[ARC76 account standard](https://github.com/algorandfoundation/ARCs/blob/main/ARCs/arc-0076.md):
deterministic account/key derivation from a password (optionally salted with an email), using
PBKDF2-HMAC-SHA256 (999999 iterations, 32-byte output) as the shared seed for every
chain-specific key.

The GitHub repo is `scholtz/ARC76Account` (renamed from `scholtz/AlgorandARC76AccountDotNet`;
`PackageProjectUrl`/`RepositoryUrl` in `Directory.Build.props` point at the new name). As of v3.0, the
library is split into **one .NET project/NuGet package per chain**, plus a shared `Core`
package — this is deliberate: installing `ARC76Account.Bitcoin` must not drag in `Algorand4`,
`Nethereum.Signer`, or `NBitcoin.Altcoins`. Don't reintroduce a single merged assembly.

It currently derives accounts for five chains:
- **Algorand (AVM)** — package `ARC76Account.Algorand`, `ARC76Account.Algorand.ARC76` → `Algorand.Algod.Model.Account` (via the `Algorand4` package). Also ships `ARC76Account.Utils.ARC76Signer`/`IAlgorandSigner` (an ASP.NET Core-friendly transaction signer) and `ARC76Account.Model.Config.ARC76Config` — these are Algorand-specific, so they live here rather than in Core.
- **Ethereum / EVM** — package `ARC76Account.Ethereum`, `ARC76Account.Ethereum.ARC76` → `Nethereum.Signer.EthECKey`
- **Bitcoin** — package `ARC76Account.Bitcoin`, `ARC76Account.Bitcoin.ARC76` → `NBitcoin.Key` (native SegWit/bech32 address via `key.PubKey.GetAddress(ScriptPubKeyType.Segwit, network)`, WIF private key via `key.GetWif(network)`)
- **Bitcoin Cash** — package `ARC76Account.BitcoinCash`, `ARC76Account.BitcoinCash.ARC76` → `NBitcoin.Key` (via the `NBitcoin.Altcoins` package; CashAddr address via `key.PubKey.GetAddress(ScriptPubKeyType.Legacy, NBitcoin.Altcoins.BCash.Instance.Mainnet)` — BCH has no SegWit, so `Legacy` is its only P2PKH type)
- **Solana** — package `ARC76Account.Solana`, `ARC76Account.Solana.ARC76` → `Solnet.Wallet.Account` (via `Solnet.Wallet` + `Chaos.NaCl.Standard`; the 32-byte ARC76 seed is expanded directly into an Ed25519 keypair via `Chaos.NaCl.Ed25519.KeyPairFromSeed` — no BIP32/BIP44 hierarchical derivation, same "raw seed" approach as the other chains). Address is `.PublicKey` (base58), private key is `.PrivateKey` (base58 of the 64-byte expanded secret key, the format wallets like Phantom export).

Every chain exposes the same shape: `GetAccount(password, slot = 0)` / `GetEmailAccount(email, password, slot = 0)`.

`ARC76Account.Core` has no chain SDK dependencies at all — it only contains the shared,
public `Arc76Seed.Derive(...)` PBKDF2 call. Every chain package takes a `ProjectReference` on
it (which becomes a `PackageReference` on the published `ARC76Account.Core` NuGet package once
packed).

## Repo layout

- `ARC76Account.Core/` (`ARC76Account.Core.csproj`, package `ARC76Account.Core`)
  - `Arc76Seed.cs` — **public**, namespace `ARC76Account.Core`. Shared PBKDF2 seed derivation.
    Public (not internal) because each chain now lives in its own assembly and calls this from
    outside `ARC76Account.Core`.
- `ARC76Account.Algorand/` (package `ARC76Account.Algorand`) — depends on `ARC76Account.Core`, `Algorand4`, `Microsoft.Extensions.Options`
  - `ARC76.cs` — `ARC76Account.Algorand.ARC76`
  - `Utils/ARC76Signer.cs`, `Utils/IAlgorandSigner.cs` — namespace `ARC76Account.Utils`
  - `Model/Config/ARC76Config.cs` — namespace `ARC76Account.Model.Config`
- `ARC76Account.Ethereum/` (package `ARC76Account.Ethereum`) — depends on `ARC76Account.Core`, `Nethereum.Signer`
  - `ARC76.cs` — `ARC76Account.Ethereum.ARC76`
- `ARC76Account.Bitcoin/` (package `ARC76Account.Bitcoin`) — depends on `ARC76Account.Core`, `NBitcoin`
  - `ARC76.cs` — `ARC76Account.Bitcoin.ARC76`
- `ARC76Account.BitcoinCash/` (package `ARC76Account.BitcoinCash`) — depends on `ARC76Account.Core`, `NBitcoin`, `NBitcoin.Altcoins`
  - `ARC76.cs` — `ARC76Account.BitcoinCash.ARC76`
- `ARC76Account.Solana/` (package `ARC76Account.Solana`) — depends on `ARC76Account.Core`, `Solnet.Wallet`, `Chaos.NaCl.Standard`
  - `ARC76.cs` — `ARC76Account.Solana.ARC76`
- `TestARC76/` — NUnit test project, `ProjectReference`s all six of the above
  - `AVMTests.cs`, `EVMTests.cs`, `BitcoinTests.cs`, `BitcoinCashTests.cs`, `SolanaTests.cs` — known-answer tests (fixed password → fixed address/key), one file per chain
  - `ARC76SignerTests.cs` — signer wiring + transaction-signing test
- `Directory.Build.props` — shared MSBuild/NuGet metadata (TFM, nullable, `GeneratePackageOnBuild`, `Authors`, `PackageProjectUrl`, icon/readme/license packing, **`<Version>`**) for every project in the repo. **Bump the version here, once, for all packages** — don't set per-project `<Version>` overrides unless a single package needs an out-of-band patch release.
- `README.md` — public usage docs, migration guides, and "adding a new chain" pointer (packed into every NuGet package — keep in sync with the actual namespaces/methods)
- `AI_INTEGRATION.md` — API reference aimed at AI assistants writing *consumer* code against these packages (not packed into the NuGet packages)

No `.editorconfig`. There is a publish workflow, `.github/workflows/publish-nuget.yml` (repo
root, not under `dotnet/`) — see the root `AGENTS.md`'s "CI/CD" section. It's a NuGet Trusted
Publishing (OIDC) pipeline triggered only by `dotnet/**` changes; there's no separate
test-on-PR workflow.

## Namespace gotcha

`ARC76Account.Algorand` (the namespace) shares its simple name with the external `Algorand`
package's root namespace (`Algorand.Algod.Model...`, `Algorand.Address`). Any code written
*inside* an `ARC76Account.*` namespace block that references the external SDK by a
bare/qualified name starting with `Algorand.` must either:
- rely on `using Algorand.Algod.Model;`-style directives placed **above** the `namespace`
  declaration (file-scoped usings resolve against the global namespace and are unaffected), or
- fully qualify with `global::Algorand....` when referencing it inline (see
  `ARC76Account.Algorand/Utils/ARC76Signer.cs` and `Utils/IAlgorandSigner.cs` for examples).

Forgetting this produces confusing "type not found" errors pointing at
`ARC76Account.Algorand.Address` etc. instead of the SDK type. This does not affect the
`Ethereum`/`Bitcoin`/`BitcoinCash`/`Solana` namespaces since `Nethereum`/`NBitcoin`/`Solnet`
don't share a name with them.

## Build & test

```
dotnet build
dotnet test
```

Every project targets `net10.0` (via `Directory.Build.props`). Run the full test suite before
considering any change to account derivation complete — the per-chain tests are known-answer
tests, so a passing suite is strong evidence the derivation logic is unchanged/correct.

## Coding conventions

- Follow the existing per-chain pattern: each chain gets its own **project** (not just
  namespace) and a static `ARC76` class with `GetAccount`/`GetEmailAccount`, both calling into
  the shared, public `ARC76Account.Core.Arc76Seed.Derive(...)` rather than duplicating the
  PBKDF2 call. Do not reintroduce copy-pasted PBKDF2 logic per chain — that's the exact
  duplication the `Core` split was meant to remove.
- Never add a `PackageReference` for one chain's SDK (e.g. `NBitcoin`) to another chain's
  project (e.g. `ARC76Account.Ethereum`) — that's precisely the dependency bleed the v3.0
  per-chain package split was done to prevent. Only `ARC76Account.Core` may be referenced by
  every chain project.
- PBKDF2 salt/input strings use the `ARC-0076-...-PBKDF2-999999` format exactly as specified by
  ARC76 — **do not alter salt/input string construction or iteration count/hash size** without
  confirming against the ARC76 spec, since it would silently change every derived address for
  existing users, on every chain at once (it's shared code).
- `slot` parameter (default `0`) lets one password derive multiple independent accounts —
  preserve this parameter when adding new chain variants.
- Nullable reference types and implicit usings are enabled repo-wide via `Directory.Build.props`.
- Keep each chain package dependency-light. Adding a new chain means adding exactly one new
  signer package (e.g. Nethereum for EVM) to that chain's own `.csproj`, not a whole node SDK if
  a lighter key-derivation-only package exists, and never to another chain's project.
- There is deliberately no chain-agnostic interface/factory across chain namespaces — don't add
  one unless asked; see README.md → "Adding a new chain".

## Adding support for a new chain

This is the most common future task shape (Bitcoin, Bitcoin Cash, and Solana were all added
this way). Use the `add-chain-support` skill (repo root: `.claude/skills/add-chain-support/SKILL.md`)
when asked to add a new chain — it walks through creating a new `ARC76Account.<Chain>/`
project, the tests, the README, and the `.sln`/`Directory.Build.props` wiring.

## Security notes

- This library derives cryptographic keys from user passwords. The README already tells
  consumers to require ≥50-char passwords — don't weaken that guidance.
- Never log, print, or persist derived seeds/private keys/passwords in code or examples.
- Any change to PBKDF2 parameters (iterations, hash algorithm, salt format) is a breaking
  change to every existing derived address, on every chain — treat it as a major version bump
  and call it out loudly in the PR description, not just the changelog.

## Release process

The shared `<Version>` lives in `Directory.Build.props` and applies to every package
(`ARC76Account.Core`, `ARC76Account.Algorand`, `ARC76Account.Ethereum`, `ARC76Account.Bitcoin`,
`ARC76Account.BitcoinCash`, `ARC76Account.Solana`) — bump it there once per release rather than
per-project. Each project has `GeneratePackageOnBuild=True`, so `dotnet build` produces all six
`.nupkg` files. Update `README.md` (and add a migration guide entry, if the change is breaking)
before publishing.

History: package was `AlgorandARC76Account` (single package, namespace
`AlgorandARC76AccountDotNet`) through v1.x; renamed to `ARC76Account` (single package) at
v2.0.0; split into `ARC76Account.Core` + one package per chain at v3.0.0. All of this is
documented in `README.md`'s migration guides and should not be repeated/reverted without a
clear reason.
