# AGENTS.md

Instructions for AI coding agents (Claude Code, etc.) working in this repository.

## Repo overview

This repo implements the
[ARC76 account standard](https://github.com/algorandfoundation/ARCs/blob/main/ARCs/arc-0076.md)
(deterministic, password-derived blockchain accounts) in multiple languages, each in its own
top-level folder, independently versioned and published:

- **`dotnet/`** — .NET, published to NuGet as `ARC76Account.Core` + one package per chain
  (`ARC76Account.Algorand`, `.Ethereum`, `.Bitcoin`, `.BitcoinCash`, `.Solana`). See
  [`dotnet/AGENTS.md`](dotnet/AGENTS.md).
- **`typescript/`** — TypeScript/JS, a pnpm workspace publishing one npm package per chain
  (`arc76-core` + `arc76-algorand`, `.ethereum`, `.bitcoin`, `.bitcoincash`, `.solana`),
  mirroring `dotnet/`'s architecture. See [`typescript/AGENTS.md`](typescript/AGENTS.md).
- **`python/`** — reserved for a future PyPI package. Not implemented yet — no AGENTS.md there
  until there's something to document.

**When working on a specific language, read that folder's own `AGENTS.md` first** — it has the
actual build/test commands, coding conventions, and architecture notes for that codebase. This
root file is only the cross-language map; don't duplicate per-language detail here, and don't
let this file go stale relative to the nested ones.

The GitHub repo is still named `AlgorandARC76AccountDotNet` (historical; unchanged since this
was originally a .NET-only repo before the v3.x package split and this multi-language reorg).

## Shared ARC76 semantics (cross-language contract)

Every language implementation must derive account key material identically for the same
inputs: PBKDF2-HMAC-SHA256, 999,999 iterations, 32-byte seed, salt/input strings of the form
`ARC-0076-{email}-{password}-{slot}-PBKDF2-999999` / `ARC-0076-{email}-{slot}-PBKDF2-999999`
(the `-{email}` segment omitted when no email is given). **This is the one invariant that spans
every folder in this repo** — if you change it in one language, you must change it in all of
them (and bump every affected package's major version), or you silently break cross-language
compatibility. When in doubt, treat `dotnet/ARC76Account.Core/Arc76Seed.cs` as the reference
implementation and verify other languages against its known-answer test values.

## Repo layout

```
dotnet/       .NET libraries — see dotnet/AGENTS.md
typescript/   TypeScript/JS library — see typescript/AGENTS.md
python/       Reserved for a future PyPI package (not yet implemented)
LICENSE.txt   Single license for the whole repo (all languages)
README.md     Repo-level overview (this repo's front door)
AI_INTEGRATION.md  Repo-level index of per-language consumer-facing AI guides
.claude/skills/add-chain-support/  Currently .NET-specific (adding a new chain package) — see dotnet/AGENTS.md
```

No CI workflow exists yet (no `.github/workflows`). No root `.editorconfig`.

## History

- Through v1.x: single .NET package `AlgorandARC76Account`, namespace `AlgorandARC76AccountDotNet`.
- v2.0.0: renamed to a single .NET package `ARC76Account`.
- v3.0.0: split into `ARC76Account.Core` + one .NET package per chain (Algorand, Ethereum,
  Bitcoin, Bitcoin Cash; Solana added at 3.1.0).
- This reorg: repo restructured into `dotnet/`/`typescript/`/`python/` top-level folders, and
  the previously-separate `arc76` npm package's source was copied in under `typescript/` (as a
  fresh copy, not a history-preserving merge — its original repo/history is untouched and lives
  separately on GitHub).
- Immediately after: `typescript/` was itself split from the single `arc76` package into a
  pnpm workspace with one package per chain (`arc76-core` + `arc76-algorand`/`.ethereum`/
  `.bitcoin`/`.bitcoincash`/`.solana`), mirroring `dotnet/`'s per-chain package architecture,
  and switched from npm to pnpm as its package manager.
- A professional "network of chains" logo (`assets/arc76-logo-512.png`) was created and wired
  into every NuGet package's `PackageIcon` and every npm package's README.

All of this is documented in `dotnet/README.md`'s migration guides and should not be
repeated/reverted without a clear reason.
