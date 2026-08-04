# ARC76 Security Audit Report — 2026-08-04

Performed per `audits/AUDITS-INSTRUCTIONS.md`. Third audit overall; the previous one is
`audits/2026-08-03-audit-report-2.md` (audit #2). This is a fresh, full pass over all three
now-published language implementations — including Python, which entered scope this round:
all six `arc76-*` PyPI packages were published 2026-08-03 (after audit #2 ran) and were
audited here for the first time.

## Auditor

`claude-fable-5` (Claude Fable 5, Anthropic), invoked by Ludovit Scholtz via Claude Code.

## Executive summary

The npm ecosystem is fully repaired and verified: all six `@arc76/*` packages (including the
previously-missing `@arc76/core`) are live at `1.5.2026080320`, a fresh `npm install`
resolves cleanly, and the shipped tarball code was executed directly and matched the .NET
known-answer literals and an independent Node PBKDF2 run byte-for-byte. The newly published
Python implementation is in excellent shape: shipped wheels are byte-identical to source,
all 26 shipped-artifact checks passed, and pipeline/test/doc hygiene matches the other two
languages from day one.

Two High findings this round. **Carried**: fresh NuGet installs of
`ARC76Account.Bitcoin`/`.BitcoinCash`/`.Solana` still resolve the pre-fix, vulnerable
`ARC76Account.Core 1.0.1.2026080317` (re-proven live today by a real restore) — the workflow
fix that would prevent this is merged but no NuGet publish has run since; one
`workflow_dispatch` of `publish-nuget.yml` clears it. **New**: the .NET seed derivation
interpolated `slot` with the current culture's number formatting, so a *negative* slot under
cultures like sv-SE/fi-FI/lt-LT (negative sign U+2212) or fa-IR/ar-SA (invisible directional
marks) derived a different key than the same inputs under en-US, in TypeScript, or in Python
— a determinism/cross-implementation break, though only reachable for `slot < 0`, which no
default, example, or test uses. Fixed in source this session (culture-invariant formatting +
regression test, byte-verified against the TypeScript reference under sv-SE); the shipped
NuGet Core still carries the bug until the same republish as the carried High.

**11 findings**: 0 Critical, 2 High, 0 Medium, 4 Low, 5 Informational.

## Fixes applied (same session)

| Severity | Finding | Resolution |
|---|---|---|
| High | .NET culture-sensitive negative-slot derivation | Fixed in `dotnet/ARC76Account.Core/Arc76Seed.cs` (invariant `SlotText`); regression test added (`DeriveIsCultureInvariantForNegativeSlots`, 5 hostile cultures × both overloads); full suite 22/22 passes; fixed output byte-verified against the TypeScript/Node reference for slot −1 under sv-SE. Shipped NuGet unchanged until republish. |
| — | Methodology/instructions staleness | `AUDITS-INSTRUCTIONS.md` deployed-versions table refreshed; the six published PyPI packages and `publish-pypi.yml` added to the standing scope. |

## Scope & versions audited

- Source: `scholtz/ARC76Account` @ commit `aa8378e8f748d495bd97683b383d1cbd590b6ba4`,
  `master`, working tree clean at audit start (the only working-tree changes at report time
  are this audit's own fix + test + docs, listed above).
- NuGet (all six downloaded as raw `.nupkg` from nuget.org, extracted, nuspecs inspected):
  `ARC76Account.Core` `1.0.2.2026080318`, `.Algorand` `1.0.2.2026080318`, `.Ethereum`
  `1.0.2.2026080318`, `.Bitcoin` `1.0.1.2026080317`, `.BitcoinCash` `1.0.1.2026080317`,
  `.Solana` `1.0.1.2026080317` — unchanged since audit #2.
- npm (all six downloaded as raw tarballs via `npm pack`, extracted, inspected, and the
  shipped code executed): `@arc76/core`, `.algorand`, `.ethereum`, `.bitcoin`,
  `.bitcoincash`, `.solana`, all `1.5.2026080320`.
- PyPI (**new in scope this round**; wheels downloaded raw via `pip download --no-deps`,
  extracted, diffed against source, and the shipped code executed in a fresh venv):
  `arc76-core`, `arc76-algorand`, `arc76-ethereum`, `arc76-bitcoin`, `arc76-bitcoincash`,
  `arc76-solana`, all `1.1.2026080321`.

## Methodology

All nine phases of `AUDITS-INSTRUCTIONS.md` were performed, now across three languages:

1. **Crypto core**: `Arc76Seed.cs`, `typescript/packages/core/src/index.ts`, and
   `python/packages/core/src/arc76/core/__init__.py` read line by line — 999999 iterations,
   SHA-256, exact salt/input literals, UTF-8 throughout, 32-byte output, real primitives
   (`Rfc2898DeriveBytes.Pbkdf2` / WebCrypto `SubtleCrypto` / `hashlib.pbkdf2_hmac`), no
   fallback paths, no randomness. The .NET culture-sensitive slot formatting (High finding 2)
   was found by testing the "no locale-dependent string formatting" requirement empirically
   rather than by inspection alone.
2. **Published artifacts**: all 18 artifacts (6 nupkg + 6 npm tarballs + 6 wheels) fetched
   raw and extracted; dependency metadata diffed against source (npm diffs are
   formatting-only plus the expected `workspace:^`→`^1.0.0` rewrite; Python wheels
   byte-identical to source; nuspecs match source `.csproj`s); no unexpected files;
   author/repo/license metadata verified on all three registries. Real fresh-install
   dependency resolution performed on all three registries (findings 1, and the npm/PyPI
   passes below).
3. **Cross-language equivalence**: the shipped npm packages, shipped PyPI packages, and
   shipped NuGet packages were all executed against the same known-answer vector set (15
   chain-level literals × password/slot-1/email, plus 4 seed vectors including non-ASCII
   email `café@example.com` → `ed15e3fb…`), each cross-checked against an independent
   PBKDF2 implementation (`crypto.pbkdf2Sync` / `hashlib.pbkdf2_hmac`). All matched
   byte-for-byte. Known-answer literals diffed across all three languages' test files —
   identical. Slot 0 ≠ slot 1 and email ≠ no-email confirmed by actual derivation in every
   language.
4. **Chain-specific correctness**: shipped-artifact runs above validate via each chain's real
   SDK (algosdk/ethers/bitcoinjs-lib+ecpair/cashaddrjs/tweetnacl on npm;
   Algorand4/Nethereum/NBitcoin/Solnet on NuGet; py-algorand-sdk/eth-account/coincurve/
   pynacl on PyPI); address formats confirmed current (SegWit bech32, CashAddr); Python's
   internal CashAddr encoder reviewed line-by-line against the spec algorithm (Informational
   finding 9); no chain package depends on another chain's SDK (verified in published
   metadata, all three registries).
5. **Entropy & guidance**: ≥50-character guidance and the same-curve shared-key warning
   verified present in root/dotnet/typescript/python READMEs, all 12 per-package READMEs
   (npm+PyPI), and all three AI_INTEGRATION.md files; "the account's security is entirely
   the password's security" framing present; 16-character minimum verified enforced by
   executing the shipped artifact in all three languages.
6. **Dependencies**: `dotnet list package --vulnerable --include-transitive` clean (all 7
   projects); `pnpm audit` clean; `pip-audit` over the fresh PyPI install clean. Upstream
   release recency re-checked for every direct dependency (all NuGet deps at latest stable;
   npm deps at latest majors; `tweetnacl`/`cashaddrjs`/`buffer`/`Chaos.NaCl.Standard`
   remain stale-but-CVE-free — finding 5). `pnpm install --frozen-lockfile` used and
   verified in CI workflow; `tiny-secp256k1`/`ecpair` `require()` workaround re-exercised by
   the passing build+tests and by executing the shipped npm tarballs.
7. **Pipelines**: all three workflow files read in full. All third-party actions SHA-pinned
   (including `pypa/gh-action-pypi-publish` in the new PyPI workflow); `audits/` excluded
   from all three path triggers; secrets scoped to the minimum steps; NuGet and PyPI use
   OIDC Trusted Publishing, npm uses a classic automation token (finding 4). Audit #2's
   Medium (Core changes not propagating to dependents) is fixed in all three workflows —
   see regression table.
8. **Tests**: `dotnet test` **22/22 passed** (incl. the new culture regression test);
   `pnpm -r test` **20/20 passed**; `pytest` per Python package **20/20 passed** (5 core +
   3×5 chains).
9. **Regression**: every audit #2 finding independently re-verified — see table below.

## Findings

### [HIGH] (carried) Fresh NuGet installs of `ARC76Account.Bitcoin`/`.BitcoinCash`/`.Solana` still resolve the pre-fix, vulnerable `ARC76Account.Core 1.0.1.2026080317`

- **Location**: nuget.org — the three chain packages' latest version remains
  `1.0.1.2026080317`, whose nuspec pins `<dependency id="ARC76Account.Core"
  version="1.0.1.2026080317" />`; NuGet resolves the lowest version satisfying a range.
- **Description**: Unchanged from audit #2's High finding 2. The publish-workflow fix that
  republishes all dependents when Core changes (commit `dabf5ed`) is merged, but no NuGet
  publish has run since, so the registry state is exactly as audit #2 left it.
- **Evidence**: Real fresh restore performed today (2026-08-04):
  ```
  $ dotnet add package ARC76Account.Bitcoin && dotnet list package --include-transitive
     > ARC76Account.Bitcoin   1.0.1.2026080317   1.0.1.2026080317
     > ARC76Account.Core                         1.0.1.2026080317   <-- pre-fix Core
  ```
- **Impact**: A .NET user installing the Bitcoin, Bitcoin Cash, or Solana package today
  still gets the Core with the audit-#1 Critical non-ASCII-email salt bug and no password
  minimum (and, per finding 2 below, the culture-sensitive negative-slot bug — though that
  one is in *every* shipped Core so far, 1.0.2 included).
- **Recommendation**: Run `publish-nuget.yml` via `workflow_dispatch` (it now publishes all
  six projects at one version with matching Core floors — verified by reading the current
  workflow). Then re-verify with a fresh `dotnet add package` + `--include-transitive` for
  each of the three. This same run also ships finding 2's fix.
- **Status**: **Open** — requires a publish run this audit cannot trigger itself.

### [HIGH] .NET seed derivation was culture-sensitive for negative slots — broke determinism and cross-language consistency under non-ASCII-minus cultures

- **Location**: `dotnet/ARC76Account.Core/Arc76Seed.cs` (both `Derive` overloads) — in
  source at audit start and in **every published `ARC76Account.Core`** (`1.0.1.2026080317`
  and `1.0.2.2026080318`, confirmed by executing the shipped 1.0.2 DLL).
- **Description**: `$"ARC-0076-{slot}-PBKDF2-999999"` formats `slot` with
  `CultureInfo.CurrentCulture`. For `slot >= 0` this is always plain ASCII digits, but a
  negative slot formats with the culture's negative sign: U+2212 (minus sign) under
  sv-SE/fi-FI/lt-LT, and an invisible directional mark prefix (U+200E / U+061C) under
  fa-IR/ar-SA. The salt/input bytes then differ from TypeScript's and Python's ASCII `-1`,
  and from the same .NET code running under en-US — same (password, email, slot), different
  key, depending on the machine's culture. TypeScript (`${slot}`) and Python (f-string) are
  locale-independent and were never affected.
- **Evidence**: Executing the shipped `ARC76Account.Core 1.0.2.2026080318` DLL:
  ```
  culture sv-SE: (-1).ToString()='−1' (U+2212) seed DIFFERS
  culture fa-IR: (-1).ToString()='‎−1' (U+200E) seed DIFFERS
  culture ar-SA: (-1).ToString()='؜-1' (U+061C) seed DIFFERS
  culture fi-FI: (-1).ToString()='−1' (U+2212) seed DIFFERS
  culture lt-LT: (-1).ToString()='−1' (U+2212) seed DIFFERS
  ```
  TypeScript/Node reference for slot −1: `ed92500ed5a3d51e2b01cc2813d87c5d8e44cb3b5f6f58b9…`;
  the pre-fix .NET under sv-SE produced a different seed; the fixed source produces exactly
  the reference value under sv-SE (verified by direct execution).
- **Impact**: A user who derives an account with a negative slot on a machine with an
  affected culture gets an account no other implementation — and no differently-configured
  machine — can re-derive; funds sent there are recoverable only from an identically
  cultured .NET environment. Rated High rather than Critical because no default, documented
  example, or test uses a negative slot (the parameter defaults to 0 and every doc shows
  0/1), so the reachable population is small; the failure mode itself, however, is exactly
  the silent-divergence class the rubric reserves Critical for — treat the republish with
  corresponding urgency.
- **Recommendation**: Fixed in source (culture-invariant `SlotText`). Ships with the
  finding-1 republish. Longer term, consider explicitly validating `slot >= 0` in all three
  languages (a spec-level decision — see also finding 6) so the negative-slot input domain
  stops existing rather than merely being consistent.
- **Status**: **Resolved in source, independently re-verified** (new regression test passes;
  fixed output matches the TypeScript reference byte-for-byte under a hostile culture).
  **Open in every published NuGet Core** until the finding-1 republish runs.

### [LOW] npm publishes with a long-lived classic automation token; NuGet and PyPI use OIDC Trusted Publishing

- **Location**: `.github/workflows/publish-npm.yml` (`NODE_AUTH_TOKEN: secrets.NPM_TOKEN`).
- **Description**: The NuGet and PyPI pipelines both use short-lived OIDC-federated
  credentials (nothing to steal at rest); the npm pipeline still uses a stored automation
  token with publish rights over the `@arc76` scope. A leaked/exfiltrated token allows
  publishing malicious versions of a private-key library until rotated.
- **Recommendation**: Migrate to npm Trusted Publishing (OIDC, supported for GitHub Actions
  publishers), mirroring the other two workflows; the token secret can then be deleted.
- **Status**: Open (maintainer action; hardening, no evidence of compromise).

### [LOW] (carried) `tweetnacl`, `cashaddrjs`, `buffer`, `Chaos.NaCl.Standard` remain stale upstream

- **Description**: Re-checked this round: `tweetnacl` last released 2020-02, `cashaddrjs`
  2020-12, `buffer` 2020-11, `Chaos.NaCl.Standard` single 2020 release. No known CVEs —
  all three vulnerability scanners clean this round. Python's equivalents
  (`pynacl`/`coincurve`/`pycryptodome`) are actively maintained.
- **Status**: Open (monitoring; unchanged from audits #1–#2).

### [LOW] (carried) Example/test password is a low-entropy public digit string

- **Description**: Unchanged: `"1234…890"` (50 digit characters) throughout docs and
  known-answer tests in all three languages, always with the ≥50-character guidance
  adjacent. Anyone using it verbatim shares an account with everyone else who did.
- **Status**: Open (low priority; the string's presence in cross-language KATs is also what
  makes it load-bearing for equivalence testing).

### [LOW] TypeScript accepts non-integer `slot` values the other languages cannot represent

- **Location**: `typescript/packages/core/src/index.ts` (`slot: number`); Python's
  `slot: int` annotation is likewise unenforced at runtime.
- **Description**: `deriveSeed(pw, 1.5)` in TypeScript happily derives from salt
  `ARC-0076-1.5-PBKDF2-999999` (and `NaN` from `ARC-0076-NaN-…`); .NET's `int slot` makes
  those inputs unrepresentable. Deterministic, but an input-domain asymmetry: a TS-derived
  fractional-slot account can never be re-derived from .NET. (Python's f-string happens to
  agree with JS for simple fractions but this is coincidental, not contractual.)
- **Recommendation**: Validate `Number.isInteger(slot)` (and non-negativity, per finding 2's
  longer-term note) in the TypeScript core, and an `isinstance(slot, int)` check in Python.
- **Status**: Open (new this round; found while testing slot edge cases).

### [INFORMATIONAL] Python ships an internal hand-written CashAddr encoder

- **Location**: `python/packages/bitcoincash/src/arc76/bitcoincash/_cashaddr.py`.
- **Description**: Address *encoding* (base32 + BCH checksum per the CashAddr spec), not a
  cryptographic primitive, so the "hand-rolled crypto is automatic Critical" rule does not
  apply. Reviewed line-by-line against the spec's reference algorithm (generator constants,
  polymod, prefix expansion, 8→5 bit conversion all match), and its output is known-answer
  locked to the same literals the .NET (NBitcoin) and TypeScript (cashaddrjs) packages
  assert. The alternative — depending on the unmaintained `cashaddress` PyPI package — is
  plausibly worse. Similarly, Bitcoin WIF is hand-assembled (`0x80 ‖ key ‖ 0x01` +
  base58check) and KAT-locked to NBitcoin/ecpair output.
- **Status**: No action needed; keep KAT coverage if it's ever touched.

### [INFORMATIONAL] (carried) Maintainer's legacy packages (`arc76` npm, `AlgorandARC76Account` NuGet) still live with no deprecation notice

- **Description**: Re-verified: `npm view arc76 deprecated` is empty; NuGet
  `AlgorandARC76Account 1.1.0` unflagged. Now that `@arc76/*` installs cleanly, the blocker
  audit #2 noted is gone.
- **Recommendation**: `npm deprecate arc76 "superseded by @arc76/*"`; NuGet deprecation flag
  on `AlgorandARC76Account` pointing at `ARC76Account.*`.
- **Status**: Open (maintainer registry action).

### [INFORMATIONAL] Typosquat sweep: no third-party lookalikes on any of the three registries

- **Description**: npm search: all `arc76`-related results maintainer-owned. NuGet search:
  all `ARC76Account.*`/`AlgorandARC76Account` owned by `Scholtz` (an unrelated
  `Arc56.Generated.…` package exists but is not a plausible confusion target). PyPI:
  `arc76`, `arc-76`, `arc76account`, `pyarc76` all 404; the published `arc76-*` names are
  maintainer-owned. Note the bare `arc76` name on PyPI is unregistered — the maintainer may
  want to claim it defensively, since it's the most natural typo of the real package names.
- **Status**: Clean this round — repeat each audit.

### [INFORMATIONAL] (carried) GitHub secret/environment configuration unverifiable

- **Description**: Unchanged — workflow YAML scoping verified for all three pipelines, but
  repo-settings-level secrets/environment protection and Actions run logs remain outside
  this audit's access.
- **Status**: Tracked — needs maintainer confirmation.

### [INFORMATIONAL] npm and PyPI dependents declare a `>=1.0.0`-style floor on their core; safe on these registries, but keep it in mind

- **Description**: Published `@arc76/*` chain packages declare `"@arc76/core": "^1.0.0"`
  and `arc76-*` wheels declare `arc76-core>=1.0.0`. Unlike NuGet (lowest-wins — finding 1),
  npm and pip resolve the *newest* matching version, so fresh installs get the latest core
  (verified by real installs on both registries this round: core resolved to
  `1.5.2026080320` / `1.1.2026080321`). No action needed; recorded so a future reader
  doesn't pattern-match this to the NuGet finding — the failure mode differs per registry.
- **Status**: No action needed.

## Regression check against previous audit (2026-08-03, audit #2)

| Audit #2 finding | Status after independent re-verification |
|---|---|
| **High #1** — all five `@arc76/*` npm packages uninstallable (`@arc76/core` never published) | **Resolved and verified live**: all six packages published at `1.5.2026080320`; fresh `npm install` of all five chain packages succeeds and resolves core `1.5.2026080320`; shipped tarball code executed and matched all .NET known-answer literals + independent PBKDF2 byte-for-byte. |
| **High #2** — fresh NuGet installs of Bitcoin/BitcoinCash/Solana resolve pre-fix Core | **Still open** — re-proven with a real fresh restore today (finding 1). Workflow fix is merged; the republish has not run. |
| **Medium** — publish pipelines never propagate a Core change to dependents | **Resolved in source, verified**: all three workflows (nuget/npm/pypi) now include the core folder in every dependent's change detection, with comments citing audit #2. The npm run that produced `1.5.2026080320` exercised the new logic for real (all six published together). NuGet side merged but unexercised until the next publish. |
| **Low** — npm per-package READMEs lacked shared-key warning (fixed in source, tarballs stale) | **Resolved and verified live**: the `1.5.2026080320` tarballs' READMEs are identical to source and carry the warning (diffed after raw extraction). |
| **Low** — `dotnet/AI_INTEGRATION.md` fictional version history | **Resolved, re-verified** in current source (real history, `@arc76/*` pointers correct). |
| **Low** — published npm version string from pre-fix scheme | **Resolved**: `1.5.2026080320` matches the documented `1.{run}.{yyyyMMddHH}` scheme and orders above the old `1.0.32026080318`. |
| **Low** — stale deps (`tweetnacl`, `cashaddrjs`, `Chaos.NaCl.Standard`) | Still open (finding 5; re-checked, still no CVEs). |
| **Low** — example password is a public digit string | Still open (finding 6). |
| **Info** — false-positive record re .NET nupkg READMEs | Nothing to track. |
| **Info** — legacy own-packages live with no deprecation pointer | Still open (finding 8; re-verified undeprecated). |
| **Info** — typosquat sweep clean | Repeated this round, now including PyPI — still clean (finding 9). |
| **Info** — GitHub secrets/config unverifiable | Unchanged (finding 10). |

## Items explicitly out of scope or unable to verify

- Triggering the `workflow_dispatch` republish that findings 1–2 need — an outward-facing
  publish action deliberately left to the maintainer.
- GitHub Actions run logs and repo-settings secret/environment configuration — no access.
- npm/nuget.org/pypi.org account-level settings (2FA, token inventory) — no access.
- The .NET-side fixed Core exists only in source; its published form can only be verified
  after the next NuGet publish (re-run the Phase 2/3 checks then).
- Byte-for-byte reproducible-build comparison of the published artifacts against a local
  rebuild was not performed (version stamping makes naive diffs noisy); shipped-code
  verification was done by direct execution against known answers and, for Python, by
  byte-diffing the shipped `.py` files against source (which are shipped uncompiled).
