# AGENTS.md

Instructions for AI coding agents (Claude Code, etc.) working in this repository.

## Project overview

`AlgorandARC76Account` is a small .NET 8 library implementing the
[ARC76 account standard](https://github.com/algorandfoundation/ARCs/blob/main/ARCs/arc-0076.md):
deterministic account/key derivation from a password (optionally salted with an email),
using PBKDF2-HMAC-SHA256 (999999 iterations, 32-byte output) as the seed for chain-specific keys.

It currently derives accounts for two chains:
- **Algorand (AVM)** — `Algorand.Algod.Model.Account` (via the `Algorand4` package)
- **EVM** — `Nethereum.Signer.EthECKey`

It also ships `ARC76Signer`, an `IAlgorandSigner` implementation meant to be registered as a
singleton in ASP.NET Core apps to sign Algorand transactions with a password-derived key
configured via `ARC76Config`.

Published to NuGet as `AlgorandARC76Account`.

## Repo layout

- `AlgorandARC76AccountDotNet/` — the library project
  - `ARC76.cs` — core derivation logic (`GetAccount`, `GetEmailAccount`, `GetEVMAccount`, `GetEVMEmailAccount`)
  - `Utils/ARC76Signer.cs` — `IOptionsMonitor<ARC76Config>`-backed transaction signer
  - `Utils/IAlgorandSigner.cs` — signer interface
  - `Model/Config/ARC76Config.cs` — options POCO (`Data` = password)
- `TestARC76/` — NUnit test project
  - `AVMTests.cs`, `EVMTests.cs` — known-answer tests (fixed password → fixed address/key)
  - `ARC76SignerTests.cs` — signer wiring + transaction-signing test
- `README.md` — public usage docs (keep in sync with `ARC76.cs`'s public API; it's packed into the NuGet package)

No CI workflow exists yet (no `.github/workflows`). No `.editorconfig`.

## Build & test

```
dotnet build
dotnet test
```

Both projects target `net8.0`. Run the full test suite before considering any change to
`ARC76.cs` complete — the AVM/EVM tests are known-answer tests, so a passing suite is strong
evidence the derivation logic is unchanged/correct.

## Coding conventions

- Follow the existing style in `ARC76.cs`: static methods on `ARC76`, one method per
  chain/variant, PBKDF2 salt/input strings built with the `ARC-0076-...-PBKDF2-999999` format
  exactly as specified by ARC76 — **do not alter salt/input string construction or iteration
  count/hash size** without confirming against the ARC76 spec, since it would silently change
  every derived address for existing users.
- `slot` parameter (default `0`) lets one password derive multiple independent accounts —
  preserve this parameter when adding new chain variants.
- Nullable reference types and implicit usings are enabled; match that in new files.
- Keep the library dependency-light. Adding a new chain means adding exactly one new signer
  package (e.g. Nethereum for EVM), not a whole SDK if a lighter key-derivation-only package exists.

## Adding support for a new chain

This is the most common future task shape (the EVM addition followed this exact pattern). Use
the `add-chain-support` skill (`.claude/skills/add-chain-support/SKILL.md`) when asked to add a
new chain — it walks through the four things that must change together: `ARC76.cs`, tests,
README, and the NuGet package's `PackageTags`/description if relevant.

## Security notes

- This library derives cryptographic keys from user passwords. The README already tells
  consumers to require ≥50-char passwords — don't weaken that guidance.
- Never log, print, or persist derived seeds/private keys/passwords in code or examples.
- Any change to PBKDF2 parameters (iterations, hash algorithm, salt format) is a breaking
  change to every existing derived address — treat it as a major version bump and call it out
  loudly in the PR description, not just the changelog.

## Release process

Version is set in `AlgorandARC76AccountDotNet.csproj` (`<Version>`). The project has
`GeneratePackageOnBuild=True`, so `dotnet build` produces the `.nupkg`. Bump `<Version>` and
update `README.md` if the public API changed before publishing.
