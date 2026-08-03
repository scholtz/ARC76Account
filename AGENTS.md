# AGENTS.md

Instructions for AI coding agents (Claude Code, etc.) working in this repository.

## Project overview

`ARC76Account` is a small .NET 10 library implementing the
[ARC76 account standard](https://github.com/algorandfoundation/ARCs/blob/main/ARCs/arc-0076.md):
deterministic account/key derivation from a password (optionally salted with an email), using
PBKDF2-HMAC-SHA256 (999999 iterations, 32-byte output) as the shared seed for every
chain-specific key.

The GitHub repo is still named `AlgorandARC76AccountDotNet` (unchanged); the .NET
project/namespace/NuGet package were renamed to `ARC76Account` in v2.0.

It currently derives accounts for three chains, one namespace per chain:
- **Algorand (AVM)** — `ARC76Account.Algorand.ARC76` → `Algorand.Algod.Model.Account` (via the `Algorand4` package)
- **Ethereum / EVM** — `ARC76Account.Ethereum.ARC76` → `Nethereum.Signer.EthECKey`
- **Bitcoin** — `ARC76Account.Bitcoin.ARC76` → `NBitcoin.Key` (native SegWit/bech32 address via `key.PubKey.GetAddress(ScriptPubKeyType.Segwit, network)`, WIF private key via `key.GetWif(network)`)

Both expose the same shape: `GetAccount(password, slot = 0)` / `GetEmailAccount(email, password, slot = 0)`.

It also ships `ARC76Signer` (`ARC76Account.Utils`), an `IAlgorandSigner` implementation meant to
be registered as a singleton in ASP.NET Core apps to sign Algorand transactions with a
password-derived key configured via `ARC76Config`.

Published to NuGet as `ARC76Account`.

## Repo layout

- `ARC76Account/` — the library project (`ARC76Account.csproj`)
  - `Core/Arc76Seed.cs` — **internal**, shared PBKDF2 seed derivation used by every chain. Not
    part of the public API; chain classes call `Arc76Seed.Derive(...)` and turn the resulting
    32-byte seed into a chain-specific keypair.
  - `Algorand/ARC76.cs` — `ARC76Account.Algorand.ARC76`, Algorand/AVM account derivation
  - `Ethereum/ARC76.cs` — `ARC76Account.Ethereum.ARC76`, EVM account derivation
  - `Bitcoin/ARC76.cs` — `ARC76Account.Bitcoin.ARC76`, Bitcoin account derivation (via `NBitcoin`)
  - `Utils/ARC76Signer.cs` — `IOptionsMonitor<ARC76Config>`-backed transaction signer (Algorand only)
  - `Utils/IAlgorandSigner.cs` — signer interface
  - `Model/Config/ARC76Config.cs` — options POCO (`Data` = password)
- `TestARC76/` — NUnit test project
  - `AVMTests.cs`, `EVMTests.cs`, `BitcoinTests.cs` — known-answer tests (fixed password → fixed address/key), one file per chain
  - `ARC76SignerTests.cs` — signer wiring + transaction-signing test
- `README.md` — public usage docs, migration guide, and "adding a new chain" pointer (packed into the NuGet package — keep in sync with the actual namespaces/methods)
- `AI_INTEGRATION.md` — API reference aimed at AI assistants writing *consumer* code against this package (not packed into the NuGet package)

No CI workflow exists yet (no `.github/workflows`). No `.editorconfig`.

## Namespace gotcha

`ARC76Account.Algorand` is a nested namespace under `ARC76Account`, and it shares its simple
name with the external `Algorand` package's root namespace (`Algorand.Algod.Model...`,
`Algorand.Address`). Any code written *inside* an `ARC76Account.*` namespace block that
references the external SDK by a bare/qualified name starting with `Algorand.` must either:
- rely on `using Algorand.Algod.Model;`-style directives placed **above** the `namespace`
  declaration (file-scoped usings resolve against the global namespace and are unaffected), or
- fully qualify with `global::Algorand....` when referencing it inline (see `Utils/ARC76Signer.cs`
  and `Utils/IAlgorandSigner.cs` for examples).

Forgetting this produces confusing "type not found" errors pointing at
`ARC76Account.Algorand.Address` etc. instead of the SDK type. This does not affect the
`Ethereum` namespace since `Nethereum` doesn't share a name with it.

## Build & test

```
dotnet build
dotnet test
```

Both projects target `net10.0`. Run the full test suite before considering any change to
account derivation complete — the AVM/EVM tests are known-answer tests, so a passing suite is
strong evidence the derivation logic is unchanged/correct.

## Coding conventions

- Follow the existing per-chain pattern: each chain gets its own namespace and a static `ARC76`
  class with `GetAccount`/`GetEmailAccount`, both calling into the shared, internal
  `ARC76Account.Core.Arc76Seed.Derive(...)` rather than duplicating the PBKDF2 call. Do not
  reintroduce copy-pasted PBKDF2 logic per chain — that's the exact duplication the `Core`
  split was meant to remove.
- PBKDF2 salt/input strings use the `ARC-0076-...-PBKDF2-999999` format exactly as specified by
  ARC76 — **do not alter salt/input string construction or iteration count/hash size** without
  confirming against the ARC76 spec, since it would silently change every derived address for
  existing users, on every chain at once (it's shared code now).
- `slot` parameter (default `0`) lets one password derive multiple independent accounts —
  preserve this parameter when adding new chain variants.
- Nullable reference types and implicit usings are enabled; match that in new files.
- Keep the library dependency-light. Adding a new chain means adding exactly one new signer
  package (e.g. Nethereum for EVM), not a whole SDK if a lighter key-derivation-only package exists.
- There is deliberately no chain-agnostic interface/factory across chain namespaces — don't add
  one unless asked; see README.md → "Adding a new chain".

## Adding support for a new chain

This is the most common future task shape (the EVM addition followed this exact pattern before
the v2.0 restructure). Use the `add-chain-support` skill
(`.claude/skills/add-chain-support/SKILL.md`) when asked to add a new chain — it walks through
the things that must change together: a new `<Chain>/ARC76.cs`, tests, README, and the NuGet
package's `PackageTags`/description if relevant.

## Security notes

- This library derives cryptographic keys from user passwords. The README already tells
  consumers to require ≥50-char passwords — don't weaken that guidance.
- Never log, print, or persist derived seeds/private keys/passwords in code or examples.
- Any change to PBKDF2 parameters (iterations, hash algorithm, salt format) is a breaking
  change to every existing derived address, on every chain — treat it as a major version bump
  and call it out loudly in the PR description, not just the changelog.

## Release process

Version is set in `ARC76Account/ARC76Account.csproj` (`<Version>`). The project has
`GeneratePackageOnBuild=True`, so `dotnet build` produces the `.nupkg`. Bump `<Version>` and
update `README.md` (and its migration guide, if the change is breaking) before publishing.
The package was renamed `AlgorandARC76Account` → `ARC76Account` at v2.0.0 — that migration is
documented in `README.md` and should not be repeated/reverted without a clear reason.
