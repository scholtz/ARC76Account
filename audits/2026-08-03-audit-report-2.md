# ARC76 Security Audit Report — 2026-08-03 (audit #2)

Performed per `audits/AUDITS-INSTRUCTIONS.md`. Second audit of the day — the first is
`audits/2026-08-03-audit-report.md`; this one runs the full methodology fresh and re-verifies
every finding the first one recorded.

## Auditor

`claude-fable-5` (Claude Fable 5, Anthropic), invoked by Ludovit Scholtz via Claude Code.

## Executive summary

The cryptography itself is in good shape: the Critical UTF-8 email-salt fix from audit #1 is
**verified live** in the published NuGet `ARC76Account.Core 1.0.2.2026080318` (confirmed by
executing the downloaded, shipped DLL and matching its output byte-for-byte against an
independent Node.js PBKDF2 implementation for four test vectors), the 16-character password
minimum is enforced in the shipped DLL, both test suites pass in full, and both vulnerability
scanners are clean.

The problems this audit found are all in **release engineering, and both are live on the
registries today**. **High #1**: all five published `@arc76/*` npm packages are uninstallable —
they depend on `@arc76/core@^1.0.0`, which was never published (the per-package
change-detection skipped it), so every `npm install @arc76/<chain>` fails with a 404.
**High #2**: `ARC76Account.Bitcoin`/`.BitcoinCash`/`.Solana` were not republished alongside
Core, and their nuspec pins Core `>= 1.0.1.2026080317`; because NuGet resolves the *lowest*
version satisfying a range, a fresh install of any of those three chain packages today was
**proven to pull the pre-fix, vulnerable Core** (ASCII email-salt Critical bug, no password
minimum). Both are fixable with a single manual `workflow_dispatch` run of each publish
workflow. No evidence of tampering or third-party typosquatting was found.

**12 findings**: 0 Critical, 2 High, 1 Medium, 5 Low, 4 Informational.

## Fixes applied (same session)

| Severity | Finding | Resolution |
|---|---|---|
| Low | npm per-package READMEs lack the shared-key warning | Fixed in source — chain-appropriate warning appended to all six `typescript/packages/*/README.md`. Live tarballs unchanged until republish. |
| Low | `dotnet/AI_INTEGRATION.md` describes a fictional version history ("correct as of 3.1.0", "rename at 2.0, split at 3.0") | Fixed — rewritten to the real history (`AlgorandARC76Account` ≤ 1.1.0 → per-chain `ARC76Account.*` 1.0.x), and its stale "(`arc76`, Algorand only)" TypeScript pointer corrected to `@arc76/*`. `typescript/AI_INTEGRATION.md`'s stale `arc76-bitcoin` naming also corrected. |
| — | Methodology gap | Added a Phase 2 check to `AUDITS-INSTRUCTIONS.md`: verify published dependents' declared dependencies (a) exist on the registry and (b) have version floors that resolve to fix-containing versions, via a real fresh install — both halves of this failed this round. Deployed-versions table refreshed. |

The two High findings **cannot be fixed from source** — they require re-running the publish
pipelines (a maintainer action; see each finding's recommendation).

## Scope & versions audited

- Source: `scholtz/ARC76Account` @ commit `54de90f77af122aab39485abbbb1252f393e3596`, `master`,
  working tree clean at audit start.
- NuGet (all downloaded as raw `.nupkg` from nuget.org and extracted/inspected):
  `ARC76Account.Core` `1.0.2.2026080318`, `.Algorand` `1.0.2.2026080318`, `.Ethereum`
  `1.0.2.2026080318`, `.Bitcoin` `1.0.1.2026080317`, `.BitcoinCash` `1.0.1.2026080317`,
  `.Solana` `1.0.1.2026080317`.
- npm (all downloaded as raw tarballs via `npm pack` and extracted/inspected):
  `@arc76/algorand`, `.ethereum`, `.bitcoin`, `.bitcoincash`, `.solana`, all `1.0.32026080318`
  (published 2026-08-03 18:59 UTC). `@arc76/core`: **not published** (registry 404).
- `python/`: confirmed still unimplemented per its README — out of scope.

## Methodology

All nine phases of `AUDITS-INSTRUCTIONS.md` were performed:

1. **Crypto core**: `Arc76Seed.cs` and `packages/core/src/index.ts` read line by line — correct
   iterations (999999), SHA-256, UTF-8 salt/input construction, 32-byte output, real primitives
   (`Rfc2898DeriveBytes.Pbkdf2` / WebCrypto `SubtleCrypto`), no fallback paths, no randomness.
2. **Published artifacts**: all 6 nupkgs + all 5 available npm tarballs downloaded raw,
   extracted; nuspec/package.json dependency lists diffed against source (all match); shipped
   Core DLL executed directly; npm dist JS read and matched against source; metadata
   (author/repo/license `scholtz`/`ARC76Account`/MIT) verified. Fresh-install dependency
   resolution tested for both registries (see findings 1–2).
3. **Cross-language equivalence**: four seed vectors (password slot 0, slot 1, email, non-ASCII
   email) derived from the shipped Core 1.0.2 DLL matched an independent Node `crypto.pbkdf2Sync`
   run exactly; all known-answer literals in `dotnet/TestARC76/*.cs` vs
   `typescript/packages/*/src/index.test.ts` diffed — identical for all five chains; slot 0 ≠
   slot 1 and email ≠ no-email confirmed by actual derivation.
4. **Chain-specific correctness**: re-verified via the matching cross-language known-answer sets
   (which both languages' real chain SDKs validate at test time) plus audit #1's independent
   decodes; address formats unchanged (SegWit bech32, CashAddr, checked in shipped dist/tests).
5. **Entropy & guidance**: ≥50-char guidance present in root/dotnet/typescript READMEs and all
   six TS package READMEs; 16-char minimum enforced in both languages' shipped code (verified by
   executing the shipped DLL); shared-key warnings verified in 3 READMEs + both
   AI_INTEGRATION.md files (audit #1's doc fix re-verified — see regression section).
6. **Dependencies**: `dotnet list package --vulnerable --include-transitive` clean for all 7
   projects (Newtonsoft.Json 13.0.4 pin verified live in the published Ethereum 1.0.2 nuspec);
   `pnpm audit` clean; upstream release dates re-checked for all direct npm deps;
   `tiny-secp256k1`/`ecpair` `require()` workaround re-exercised by the passing build+tests.
7. **Pipelines**: both workflow files re-read in full; all six third-party actions verified
   still SHA-pinned; `audits/` confirmed excluded from both path triggers; secrets scoped to the
   minimum steps. The intentional "dependents don't republish on Core change" design flagged
   (finding 3).
8. **Tests**: `dotnet test` 21/21 passed; `pnpm test` 20/20 passed (5 core + 3×5 chains).
9. **Regression**: every audit #1 finding independently re-verified — see table below.

## Findings

### [HIGH] All five published `@arc76/*` npm packages are uninstallable — they depend on `@arc76/core`, which was never published

- **Location**: npm registry — `@arc76/algorand`, `.ethereum`, `.bitcoin`, `.bitcoincash`,
  `.solana` `1.0.32026080318` (all declare `"@arc76/core": "^1.0.0"`); `@arc76/core` returns
  404. Root cause in `.github/workflows/publish-npm.yml`'s per-package change detection.
- **Description**: The first (and so far only) npm publish run happened on a push that changed
  the five chain package folders but not `typescript/packages/core/`, so the matrix job for
  `core` skipped itself while the five dependents published. Every published tarball's
  `package.json` (inspected directly) depends on `@arc76/core@^1.0.0`, which doesn't exist.
- **Evidence**:
  ```
  $ npm view @arc76/core version
  npm error 404 Not Found - GET https://registry.npmjs.org/@arc76%2fcore

  $ npm install @arc76/algorand@1.0.32026080318
  npm error 404 The requested resource '@arc76/core@^1.0.0' could not be found ...
  ```
- **Impact**: No user can install any ARC76 npm package at all — the npm half of this project is
  currently 100% broken at install time. (Silver lining: because the install fails outright,
  nobody can be deriving keys from a wrong/partial npm implementation.) The unpublished-but-
  depended-on name is inside the maintainer-owned `@arc76` scope, so it is *not* squattable by
  third parties — this is an availability/integrity-of-release problem, not a
  dependency-confusion exposure.
- **Recommendation**: Run `publish-npm.yml` via `workflow_dispatch` (which treats every package
  as changed and publishes all six, core included). Then re-verify with a real
  `npm install @arc76/algorand` from a clean directory. Additionally, add a pre-publish guard to
  the workflow: after `pnpm pack`, check that every dependency in the tarball's `package.json`
  resolves on the public registry (or publish `core` first via a matrix ordering/needs
  dependency) so dependents can never again publish ahead of their own dependency.
- **Status**: **Open** — requires a publish run this audit cannot (and should not) trigger
  itself.

### [HIGH] Fresh NuGet installs of `ARC76Account.Bitcoin`/`.BitcoinCash`/`.Solana` still resolve the pre-fix, vulnerable `ARC76Account.Core 1.0.1.2026080317`

- **Location**: nuget.org — `ARC76Account.Bitcoin`, `.BitcoinCash`, `.Solana` `1.0.1.2026080317`
  nuspecs each declare `<dependency id="ARC76Account.Core" version="1.0.1.2026080317" />`.
  Root cause in `.github/workflows/publish-nuget.yml`'s per-project change detection (the
  audit-#1 fix run republished only Core/Algorand/Ethereum, the projects whose folders changed).
- **Description**: NuGet resolves the *lowest* version satisfying a dependency range. The three
  chain packages that weren't republished pin a floor of Core `1.0.1.2026080317` — the version
  containing the audit-#1 **Critical** ASCII email-salt bug and lacking the password minimum. A
  fixed Core `1.0.2.2026080318` being live on nuget.org does not help these packages' users at
  all: they get the vulnerable Core by default.
- **Evidence**: Real fresh restore performed in a clean project:
  ```
  $ dotnet add package ARC76Account.Bitcoin && dotnet list package --include-transitive
     > ARC76Account.Bitcoin   1.0.1.2026080317   1.0.1.2026080317
     > ARC76Account.Core                          1.0.1.2026080317   <-- pre-fix Core
  ```
- **Impact**: A .NET user installing the Bitcoin, Bitcoin Cash, or Solana package today can
  still (a) derive a spec-nonconformant account for any non-ASCII email (the Critical bug audit
  #1 found — live again through this path) and (b) derive accounts from arbitrarily short
  passwords with no error. The fix that was published is silently bypassed for 3 of the 5
  chains.
- **Recommendation**: Run `publish-nuget.yml` via `workflow_dispatch` (publishes all six
  projects at one new version, with Core floors updated to match). Then re-verify with a fresh
  `dotnet add package` + `--include-transitive` listing for each of the three packages. Longer
  term, see finding 3.
- **Status**: **Open** — requires a publish run.

### [MEDIUM] Both publish pipelines never propagate a Core change to dependent packages — by documented design — so every future Core security fix will silently repeat findings 1–2

- **Location**: `.github/workflows/publish-nuget.yml` ("A project depending on Core ... does
  NOT get republished just because Core changed") and `publish-npm.yml` (equivalent comment);
  the change-detection steps diff only each project's own folder.
- **Description**: The behavior is intentional and clearly documented, but this audit is the
  demonstration of its failure mode: a Critical fix in Core reached only the dependents that
  happened to have their own changes in the same push. On the NuGet side the exact-version
  dependency floor makes this worse (lowest-wins pins users to the vulnerable Core); on the npm
  side the first publish shipped dependents whose dependency didn't exist at all.
- **Evidence**: Findings 1 and 2 above are the evidence — both are direct consequences of this
  design meeting a Core-touching security release.
- **Impact**: Any future security fix to `Arc76Seed`/`@arc76/core` will again require someone to
  remember to manually republish all dependents; forgetting reproduces finding 2 exactly, for
  however long nobody notices.
- **Recommendation**: Include `ARC76Account.Core/` (resp. `typescript/packages/core/`) in every
  chain package's change-detection paths, so a Core change republishes all dependents with a
  floor bump — or at minimum add a loud workflow warning/failure when Core is in the changed
  set but dependents aren't. Keep `workflow_dispatch` as the manual full-republish escape hatch.
- **Status**: Open (workflow design change — maintainer decision).

### [INFORMATIONAL] Initial "no README in .NET packages" finding was a false positive — corrected after re-inspection

- **Description**: An earlier draft of this report flagged the NuGet packages as shipping no
  README. Re-inspection of the actually-extracted nupkg contents (`find` over the raw extracted
  files, not just the nuspec) showed this was wrong: `Directory.Build.props` sets
  `<PackageReadmeFile>README.md</PackageReadmeFile>` and packs `dotnet/README.md` (the shared
  top-level one, resolved via `$(MSBuildThisFileDirectory)README.md` relative to the props
  file's own folder) into every project's nupkg root. Verified present, with the ≥50-character
  guidance and the same-curve shared-key warning both in the shipped text, in both the `Core`
  and `Bitcoin` nupkgs (representative pre- and post-fix versions).
- **Why this happened**: The original check read `<ItemGroup>` in `Directory.Build.props` and
  incorrectly assumed a `README.md` referenced via `$(MSBuildThisFileDirectory)` without a
  per-project override meant no file existed at that path per-project, rather than checking
  what actually got packed.
- **Status**: No action needed — noted for the record per this project's audit ground rules
  ("verify, don't assume"; corrected here since I didn't independently re-verify before
  initially writing it as a Low finding).

### [LOW] npm per-package READMEs lacked the same-curve shared-key warning — fixed in source; live tarballs still lack it

- **Location**: `typescript/packages/{core,algorand,ethereum,bitcoin,bitcoincash,solana}/README.md`
  (and identical copies inside the five published tarballs, verified by extraction).
- **Description**: Audit #1's High shared-key finding was fixed in the three top-level READMEs
  and both AI_INTEGRATION.md files, but the per-package READMEs — what npmjs.com actually
  displays for each package — were not in that fix's scope and carried only the ≥50-char note.
- **Recommendation / Status**: **Fixed in source this session** (chain-appropriate warning
  appended to all six; re-read after editing). The published tarballs keep the old README until
  the next publish — rides the finding-1 republish.

### [LOW] `dotnet/AI_INTEGRATION.md` asserted a version history that never existed on the registry

- **Location**: `dotnet/AI_INTEGRATION.md` (intro and namespace note); root/typescript
  AI_INTEGRATION pointer lines.
- **Description**: The file told AI assistants its snippets are "correct as of package version
  3.1.0" and described a "rename at 2.0, split into one-package-per-chain at 3.0", and its
  cross-reference described the TypeScript side as "(`arc76`, Algorand only)". The registries'
  full version indexes show no 2.x or 3.x has ever existed for any `ARC76Account.*` package
  (only 1.0.1/1.0.2; the legacy package is `AlgorandARC76Account` ≤ 1.1.0), and the TS side is
  five `@arc76/*` chain packages. For a file whose stated purpose is "prefer this over your
  training data", fabricated-looking version facts are exactly the kind of guidance drift the
  audit instructions call a security-relevant doc defect.
- **Evidence**: `https://api.nuget.org/v3-flatcontainer/arc76account/index.json` → 404 (never
  existed); `.../algorandarc76account/index.json` → `1.0.0…1.1.0`; package indexes for the six
  real packages show only `1.0.1`/`1.0.2` lines.
- **Recommendation / Status**: **Fixed this session** — intro rewritten to the real history,
  both stale pointers corrected (`@arc76/*`), `typescript/AI_INTEGRATION.md`'s `arc76-bitcoin`
  naming corrected.

### [LOW] Published npm version string `1.0.32026080318` comes from the pre-fix versioning scheme

- **Location**: all five published `@arc76/*` packages.
- **Description**: The live versions were produced by the older scheme
  (`1.0.{increment}{yyyyMMddHH}` concatenated) that commit `bb4bb45` replaced with
  `1.{increment}.{yyyyMMddHH}`. Verified against the current workflow: the next publish will
  produce e.g. `1.{run}.{date}`, which compares **higher** than `1.0.32026080318` (minor
  segment wins), so semver ordering is safe going forward — this is a cosmetic/consistency
  defect, not an ordering hazard.
- **Recommendation**: Nothing to do beyond the finding-1 republish; verify the next published
  version matches the documented `1.{run}.{yyyyMMddHH}` shape.
- **Status**: Open until next publish (self-corrects).

### [LOW] (carried from audit #1) `tweetnacl`, `cashaddrjs`, `Chaos.NaCl.Standard` remain stale

- **Description**: Re-checked this round: `tweetnacl` last modified 2023-04-12, `cashaddrjs`
  2022-04-12 (npm registry), `Chaos.NaCl.Standard` unchanged single 2020 release. No CVEs
  (`pnpm audit` and `dotnet list package --vulnerable` both clean this round).
- **Status**: Open (monitoring, unchanged from audit #1).

### [LOW] (carried from audit #1) Example/test password is a low-entropy public digit string

- **Description**: Unchanged; still `"1234...890"` throughout docs and known-answer tests, with
  the ≥50-char guidance adjacent in every case.
- **Status**: Open (low priority, unchanged).

### [INFORMATIONAL] The maintainer's own legacy packages (`arc76` on npm, `AlgorandARC76Account` on NuGet) are still live with no deprecation pointer

- **Description**: The typosquat sweep (below) found `arc76@1.0.2` (npm, maintainer `scholtz`,
  repo `scholtz/arc76-npm`, last modified 2025-08-06) and `AlgorandARC76Account 1.1.0` (NuGet,
  owner `Scholtz`). Both predate this repo's packages and none of the audit-#1 fixes; a user
  searching "arc76" may land on them instead of `@arc76/*`/`ARC76Account.*` — and today, with
  `@arc76/*` uninstallable (finding 1), legacy `arc76` is the only npm package that installs.
- **Recommendation**: Publish a deprecation notice on both legacy packages
  (`npm deprecate arc76 ...`; NuGet legacy-flag/deprecation on `AlgorandARC76Account`) pointing
  at the current packages, once finding 1's republish makes the new ones installable.
- **Status**: Open (maintainer action on the registries).

### [INFORMATIONAL] Dedicated typosquat sweep (deferred from audit #1): no third-party lookalikes found

- **Description**: Searched both registries for `arc76`-related names. npm: `arc76`,
  `@arc76/*` (all maintainer-owned), plus unrelated `arc14`/`algorand-authentication-component-vue`
  (also maintainer-ecosystem). NuGet: all `ARC76Account.*` + `AlgorandARC76Account` owned by
  `Scholtz`; no third-party lookalike ids. `@arc76/core`'s unpublished state is not squattable
  (scope-protected).
- **Status**: Resolved for this round — repeat each audit.

### [INFORMATIONAL] (carried from audit #1) GitHub secret/environment configuration unverifiable

- **Description**: Unchanged — workflow YAML scoping re-verified (secrets used only in the
  steps that need them; OIDC trusted publishing on the NuGet side), but repo-settings-level
  configuration and Actions run logs remain outside this audit's access.
- **Status**: Tracked — needs maintainer confirmation.

## Regression check against previous audit (2026-08-03, audit #1)

| Audit #1 finding | Status after independent re-verification |
|---|---|
| **Critical** — ASCII email salt | **Resolved and verified live in Core 1.0.2** — the downloaded, shipped `ARC76Account.Core.dll 1.0.2.2026080318` was executed directly; the non-ASCII-email seed now matches an independent Node UTF-8 PBKDF2 run exactly (`ED15E3FB…`), and matches TypeScript. **But effectively still reachable** for fresh installs of `Bitcoin`/`BitcoinCash`/`Solana` via the stale Core floor — see High finding 2. The maintainer decision on major-version signaling for the behavior change remains open. |
| **High** — same-curve shared keys | Doc fix **re-verified**: warning present in `README.md`, `dotnet/README.md`, `typescript/README.md`, and both `AI_INTEGRATION.md` files (audit #1's claim confirmed; an initial narrow grep this round produced a false alarm, resolved by matching the files' actual wording). Extended this round to the six npm package READMEs (Low finding, fixed). Architectural option (per-chain salt) still open, maintainer decision. |
| **High** — Newtonsoft.Json CVE | **Resolved and verified live**: published `ARC76Account.Ethereum 1.0.2.2026080318` nuspec contains the direct `Newtonsoft.Json 13.0.4` dependency; fresh `dotnet list package --vulnerable --include-transitive` clean for all projects. |
| **Medium** — no .NET password minimum | **Resolved and verified live**: shipped Core 1.0.2 DLL throws `ArgumentException` for a short password (executed directly). Same stale-floor caveat as above for the three unrepublished chain packages. |
| **Medium** — mutable action tags | **Resolved, re-verified**: all six action references in both workflows still SHA-pinned. Dependabot/Renovate for action bumps still not configured (follow-up stands). |
| **Medium** — no slot≠0 tests | **Resolved, re-verified**: slot-1 known-answer tests present in all five chains × both languages; suites pass (21/21, 20/20); literals identical cross-language. |
| **Low** — signer singleton docs | **Resolved, re-verified**: XML doc comment present on `ARC76Signer`. |
| **Low** — stale deps | Still open (re-checked; unchanged, no CVEs). |
| **Low** — example password | Still open. |
| **Info** — npm not published | **Superseded**: partially published since audit #1, and the partial state is worse than none — see High finding 1. |
| **Info** — typosquat sweep deferred | **Done this round** — no third-party lookalikes; legacy own-packages noted as a new Informational. |
| **Info** — secrets unverifiable | Unchanged. |

## Items explicitly out of scope or unable to verify

- `@arc76/core`'s published artifact — nothing exists on the registry to verify; the npm-side
  seed derivation can only be audited in source until it ships. Repeat Phase 2 for npm after
  the republish.
- Triggering the two `workflow_dispatch` republish runs that both High findings need — an
  outward-facing publish action deliberately left to the maintainer.
- GitHub Actions run logs and repo-settings secret/environment configuration — no access
  (unchanged from audit #1).
- Chain-specific independent re-derivation (audit #1's from-scratch WIF/address decodes) was
  not re-executed from scratch this round; this round's chain-level assurance rests on the
  byte-identical shipped-DLL/Node seed cross-check, the unchanged cross-language known-answer
  literals (diffed in full), and both suites passing against real chain SDKs. The audit-#1
  independent decodes remain valid for those unchanged literals.
