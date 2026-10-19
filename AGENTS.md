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
  (`@arc76/core` + `@arc76/algorand`, `.ethereum`, `.bitcoin`, `.bitcoincash`, `.solana`),
  mirroring `dotnet/`'s architecture. See [`typescript/AGENTS.md`](typescript/AGENTS.md).
- **`python/`** — Python, one PyPI package per chain (`arc76-core` + `arc76-algorand`,
  `.ethereum`, `.bitcoin`, `.bitcoincash`, `.solana`, all under the shared `arc76.*` namespace
  package), mirroring `dotnet/`'s architecture. See [`python/AGENTS.md`](python/AGENTS.md).

**When working on a specific language, read that folder's own `AGENTS.md` first** — it has the
actual build/test commands, coding conventions, and architecture notes for that codebase. This
root file is only the cross-language map; don't duplicate per-language detail here, and don't
let this file go stale relative to the nested ones.

The GitHub repo is `scholtz/ARC76Account` (renamed from `scholtz/AlgorandARC76AccountDotNet` —
that was its name when this was originally a .NET-only repo, before the v3.x package split and
this multi-language reorg). If you find `AlgorandARC76AccountDotNet` in a URL anywhere in this
repo (outside of historical migration-guide text describing the old C# namespace of the same
name), it's stale — fix it to `ARC76Account`.

## Shared ARC76 semantics (cross-language contract)

Every language implementation must derive account key material identically for the same
inputs: PBKDF2-HMAC-SHA256, 999,999 iterations, 32-byte seed, salt/input strings of the form
`ARC-0076-{email}-{password}-{slot}-PBKDF2-999999` / `ARC-0076-{email}-{slot}-PBKDF2-999999`
(the `-{email}` segment omitted when no email is given). **This is the one invariant that spans
every folder in this repo** — if you change it in one language, you must change it in all of
them (and bump every affected package's major version), or you silently break cross-language
compatibility. When in doubt, treat `dotnet/ARC76Account.Core/Arc76Seed.cs` as the reference
implementation and verify other languages against its known-answer test values.

## Shared test vectors (cross-language known answers)

`test-vectors/arc76-vectors.json` is the **single source of truth** for known-answer test data
(seeds, and the Algorand/Falcon/Ethereum/Bitcoin/Bitcoin Cash/Solana accounts). The .NET,
Python and TypeScript test suites all load it (in addition to their own per-language tests) —
**never hard-code a known-answer value in only one language**. To add a case, add it to the JSON
(verify it against the .NET reference implementation) and all three suites pick it up. Changing
only `test-vectors/` deliberately does not trigger any publish workflow; instead
`.github/workflows/test-vectors.yml` runs all three languages' suites against the file.

## Repo layout

```
dotnet/       .NET libraries — see dotnet/AGENTS.md
typescript/   TypeScript/JS library — see typescript/AGENTS.md
python/       Python library — see python/AGENTS.md
test-vectors/ Shared cross-language known-answer test data (arc76-vectors.json)
audits/       Security audit reports + AUDITS-INSTRUCTIONS.md - see below
LICENSE.txt   Single license for the whole repo (all languages)
README.md     Repo-level overview (this repo's front door)
AI_INTEGRATION.md  Repo-level index of per-language consumer-facing AI guides
.claude/skills/add-chain-support/  Currently .NET-specific (adding a new chain package) — see dotnet/AGENTS.md
```

No root `.editorconfig`.

## CI/CD (`.github/workflows/`)

- `publish-nuget.yml` — path-triggered on `dotnet/**` only; diffs each of the six .NET
  projects' own folder against the previous commit and packs+pushes only the ones that
  changed, via NuGet Trusted Publishing (OIDC, no stored API key). `--skip-duplicate` is kept
  as a defensive fallback, not the primary gate.
- `publish-npm.yml` — path-triggered on `typescript/**` only; diffs each of the six `@arc76/*`
  packages' own folder independently and publishes only the ones that changed, via a classic
  npm Automation token (`NPM_TOKEN` secret) — not OIDC, since npm's Trusted Publishing requires
  a package to already exist, which doesn't hold for six brand-new `@arc76/*` scoped packages.
  See `docs/github-secrets-setup.md` for the migration path to OIDC once they exist.
- `publish-pypi.yml` — path-triggered on `python/**` only; diffs each of the six `arc76-*`
  packages' own folder independently and builds+publishes only the ones that changed, via PyPI
  **Trusted Publishing** (OIDC — no stored API token, unlike the npm workflow, since PyPI's
  Trusted Publishing doesn't require the project to already exist on the index the way npm's
  did at the time that workflow was written).

**Versions are computed by CI, not hand-maintained** — see `dotnet/AGENTS.md`,
`typescript/AGENTS.md`, and `python/AGENTS.md`'s "Versioning" sections. In short:
`1.0.{increment}.{yyyyMMddHH}` for NuGet packages (`{increment}.{MMdd}` for the compiled
AssemblyVersion/FileVersion, since that's a UInt16-per-component field), and
`1.0.{increment}{yyyyMMddHH}` (concatenated, not dotted — semver doesn't allow a 4th segment)
for npm packages. PyPI packages use the same 3-segment scheme as npm
(`1.{increment}.{yyyyMMddHH}`), since PEP 440 release segments have the same "no 4th dotted
date segment" constraint as semver. Either way, the exact publish date/hour is always readable
directly from the version number — that's the point of the scheme, so don't "simplify" it back
to a plain incrementing number without preserving that property.

**None of the three workflows is triggered by the `audits/` folder** (or anything outside
`dotnet/`/`typescript/`/`python/`) — that's deliberate, so running an audit (see
`audits/AUDITS-INSTRUCTIONS.md`; triggered by the user typing `do new audit`) never
force-publishes anything. If you add a fourth workflow or change these triggers, preserve that
property: publishing should only ever be a side effect of an actual `dotnet/`/`typescript/`/
`python/` code change, never of unrelated repo activity. See `docs/github-secrets-setup.md` for
the one-time nuget.org/npmjs.com/pypi.org/GitHub setup these workflows assume (the NuGet
Trusted Publishing policy, the `@arc76` npm org, the PyPI Trusted Publishing policies for each
`arc76-*` project, the `NUGET_USER`/`NPM_TOKEN` secrets, the optional `release` environment).

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
  pnpm workspace with one package per chain (`@arc76/core` + `@arc76/algorand`/`.ethereum`/
  `.bitcoin`/`.bitcoincash`/`.solana`), mirroring `dotnet/`'s per-chain package architecture,
  and switched from npm to pnpm as its package manager.
- A professional "network of chains" logo (`assets/arc76-logo-512.png`) was created and wired
  into every NuGet package's `PackageIcon` and every npm package's README.
- `python/` was implemented, mirroring `dotnet/`/`typescript/`'s per-chain package split from
  day one (`arc76-core` + `arc76-algorand`/`.ethereum`/`.bitcoin`/`.bitcoincash`/`.solana`,
  under the shared `arc76.*` namespace package) — there was never a single merged `arc76`
  Python package to migrate away from.

All of this is documented in `dotnet/README.md`'s migration guides and should not be
repeated/reverted without a clear reason.
