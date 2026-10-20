# ARC76 Security Audit Report — 2026-10-19

Performed per `audits/AUDITS-INSTRUCTIONS.md`. Fourth audit overall; the previous one is
`audits/2026-08-04-audit-report.md` (audit #3). A fresh, full pass over all 18 published
packages (6 NuGet + 6 npm + 6 PyPI) and the current source. New in scope since audit #3: the
Algorand post-quantum **Falcon-1024** account derivation (all three languages), the
`Algorand4` → `Algorand5` dependency upgrade, the shared `test-vectors/arc76-vectors.json`, and
`.github/workflows/test-vectors.yml`.

## Auditor

`claude-sonnet-5-5` (Claude Sonnet 5.5, Anthropic), invoked by Ludovit Scholtz via Claude Code.

## Executive summary

Cryptographic posture is strong: all three languages' shipped artifacts derive byte-identical
seeds and accounts (including the new Falcon accounts) against the shared known-answer vectors
and against independent PBKDF2 implementations, and both High findings from audit #3 are now
fixed **and verified live** (NuGet republished at `1.0.4.2026080407` with the culture-invariant
Core; Bitcoin/BitcoinCash/Solana now resolve the fixed Core). One new **High** finding: the
latest `ARC76Account.Algorand` releases (`1.0.5`, `1.0.6` — the ones carrying Falcon and
Algorand5) **cannot be installed**, because they pin an `ARC76Account.Core` version that was
never published; the NuGet pipeline's "publish only changed projects" logic re-creates the
audit-#2 failure class in a new shape.

**11 findings**: 0 Critical, 1 High, 0 Medium, 4 Low, 6 Informational.

## Instructions updated

`AUDITS-INSTRUCTIONS.md` was updated in this pass: the deployed-versions table and notes were
refreshed, Falcon was added to the standing scope notes, and a new Phase 2 check was added
("check every dependent's *latest* version restores", the gap that finding 1 exposed).

## Scope & versions audited

- Source: `scholtz/ARC76Account` @ commit `11b238c8148042e61359554d32566539019f5c83`
  (`master`), working tree clean at audit start; no source files were modified by this audit
  (only `audits/` files and the maintainer's memory notes).
- NuGet (raw `.nupkg` downloaded, extracted, nuspecs inspected): `ARC76Account.Core`
  `1.0.4.2026080407`, `.Ethereum` / `.Bitcoin` / `.BitcoinCash` / `.Solana` `1.0.4.2026080407`,
  `.Algorand` `1.0.6.2026101918` (built from commit `cc7386a`; its `1.0.5` and the fixed
  Core-floor problem are finding 1). The nuspec `repository commit` for the 1.0.4 set is
  `707d15a`, which is after the culture fix `6ed949b`.
- npm (raw tarballs via `npm pack`, extracted, executed): `@arc76/core`, `.algorand`,
  `.ethereum`, `.bitcoin`, `.bitcoincash`, `.solana`, all `1.8.2026101919`.
- PyPI (wheels via `pip download --no-deps`, extracted, diffed against source, installed into a
  fresh venv and executed): `arc76-core`, `-algorand`, `-ethereum`, `-bitcoin`, `-bitcoincash`,
  `-solana`, all `1.4.2026101919`.

## Methodology

All nine phases of `AUDITS-INSTRUCTIONS.md` were performed:

1. **Crypto core**: `Arc76Seed.cs`, `typescript/packages/core/src/index.ts`,
   `python/packages/core/src/arc76/core/__init__.py` re-read line by line — 999999 iterations,
   SHA-256, exact salt/input literals, UTF-8, 32-byte output, real platform primitives
   (`Rfc2898DeriveBytes.Pbkdf2` / WebCrypto / `hashlib.pbkdf2_hmac`), no fallback, no randomness.
   16-char minimum enforced in all three (shipped code executed: short password rejected).
   Culture-invariant slot formatting present in .NET source **and in the shipped Core 1.0.4**
   (the `DeriveIsCultureInvariantForNegativeSlots` regression test, 5 hostile cultures, passes
   when run against the shipped DLLs).
2. **Published artifacts**: all 18 fetched raw and inspected. nuspec/`package.json`/wheel
   `METADATA` dependency lists match source; no unexpected files (nupkgs: DLL + README + LICENSE
   + icon + signature; npm: `dist/`, README, package.json; wheels: `.py` + `py.typed`);
   author/repo/license correct on all three registries. Python wheels byte-identical to source
   modulo CRLF line endings. **Real fresh-install dependency resolution** performed on all three
   registries (npm: all five chain packages in one install → core `1.8.2026101919`; PyPI: all six
   wheels + `[falcon]` extra; NuGet: per-chain `dotnet add package` + `--include-transitive`) —
   finding 1 came from this step.
3. **Cross-language equivalence**: the shipped npm packages, shipped PyPI packages and shipped
   NuGet packages were each executed against `test-vectors/arc76-vectors.json` (9 seed vectors
   incl. non-ASCII-email; Algorand, **Falcon**, Ethereum, Bitcoin, Bitcoin Cash, Solana accounts)
   and every seed was additionally cross-checked against an independent PBKDF2
   (`crypto.pbkdf2Sync` / `hashlib.pbkdf2_hmac`). Results: npm 31/31, PyPI 19/19 + 12/12,
   NuGet 62/62 (the repo's NUnit suite re-pointed at the published packages). Slot 0 ≠ slot 1
   and email ≠ no-email covered by the vector set in every language.
4. **Chain-specific correctness**: addresses/encodings validated by each chain's real SDK
   (algosdk, ethers, bitcoinjs-lib + cashaddrjs, bs58/tweetnacl; Algorand5, Nethereum, NBitcoin,
   Solnet; py-algorand-sdk + algorand-falcon, eth-account, coincurve, pynacl). Bitcoin is native
   SegWit, Bitcoin Cash CashAddr. No chain package depends on another chain's SDK (published
   metadata). The Falcon derivation (keygen seed = SHA-512/256("PQK" ‖ "f1" ‖ seed), canonical
   salt search) is computed by three independent implementations (.NET `FalconAccount.FromEntropy`,
   Python `algorand-falcon`, TS `falcon-1024` + a local salt search) that agree on all 6 Falcon
   vectors.
5. **Entropy & guidance**: ≥50-character guidance, "security is entirely the password's"
   framing, and the same-curve shared-key warning re-verified present in the root, per-language
   and all 12 per-package READMEs; `dotnet`/`typescript`/`python` `AI_INTEGRATION.md` carry the
   shared-key warning. Falcon sections in all three Algorand READMEs and `dotnet/README.md`
   state that the Falcon account shares the Ed25519 account's password material.
6. **Dependencies**: `dotnet list package --vulnerable --include-transitive` (finding 2),
   `pnpm audit` (finding 7; `--prod` clean) and `npm audit --omit=dev` on a fresh install of the
   published packages (clean), `pip-audit` on the fresh PyPI install (only `pip` itself, a
   tooling advisory — not a dependency of the packages). Upstream recency re-checked for every
   direct dependency (finding 4, finding 6). `pnpm install --frozen-lockfile` succeeds; the
   `tiny-secp256k1`/`ecpair` `require()` workaround was re-exercised by the passing build, tests
   and the shipped-tarball run.
7. **Pipelines**: `publish-nuget.yml` read in full; `publish-npm.yml`, `publish-pypi.yml` and
   the new `test-vectors.yml` re-read (triggers, action pins, secret scoping). All third-party
   actions are SHA-pinned; `audits/` is excluded from every trigger; NuGet and PyPI use OIDC,
   npm a stored token (finding 3); npm installs with `--frozen-lockfile`. `test-vectors.yml`
   has `contents: read` only and publishes nothing.
8. **Tests**: `dotnet test` **62/62**; `pnpm -r test` **60/60** (core 15, algorand 21, 4×6);
   `pytest` per Python package (run against the installed shipped wheels) **57/57**
   (core 15, algorand 18, 4×6).
9. **Regression**: audit #3 findings re-verified independently — see table below.

## Findings

### [HIGH] `ARC76Account.Algorand` 1.0.5 and 1.0.6 (latest) are uninstallable — their exact `ARC76Account.Core` dependency was never published

- **Location**: nuget.org `ARC76Account.Algorand` `1.0.5.2026101918`, `1.0.6.2026101918`;
  `.github/workflows/publish-nuget.yml` ("Determine changed packages").
- **Description**: The pipeline packs only the projects whose own folder changed, but every
  chain package's nuspec pins `ARC76Account.Core` to *the same version as the package itself*
  (project reference → exact version). Commits `f8f8af3` (Algorand4→Algorand5) and `cc7386a`
  (Falcon) touched only `dotnet/ARC76Account.Algorand/`, so only Algorand was published, at
  run numbers 5 and 6, depending on `Core 1.0.5` / `Core 1.0.6` that do not exist. This is the
  audit-#2 "dependents/Core version coupling" class again, in the other direction. The latest
  published Algorand version is therefore unresolvable, and the .NET Falcon API and the
  Algorand5 upgrade are not obtainable by any user.
- **Evidence**:
  ```
  $ curl https://api.nuget.org/v3-flatcontainer/arc76account.core/index.json
    ["1.0.1.2026080317","1.0.2.2026080318","1.0.4.2026080407"]
  $ grep dependency ARC76Account.Algorand.nuspec     # 1.0.6.2026101918
    <dependency id="ARC76Account.Core" version="1.0.6.2026101918" .../>
  $ dotnet add package ARC76Account.Algorand          # fresh net10.0 project
    error: NU1102: Unable to find package ARC76Account.Core with version (>= 1.0.6.2026101918)
    error:   - Found 3 version(s) in nuget.org [ Nearest version: 1.0.4.2026080407 ]
  ```
  A direct `GET .../core/1.0.6.2026101918/...nupkg` and `.../1.0.5.../` both return HTTP 404.
  Users pinned to Algorand `1.0.4` (resolves Core `1.0.4`, verified) are unaffected.
- **Impact**: Availability/integrity-of-release, not key material: nobody gets a wrong key —
  installs fail loudly. But the newest Algorand package, with the new Falcon support, is
  unusable, and any user who does `dotnet add package ARC76Account.Algorand` is blocked.
- **Recommendation**: (a) immediate: `workflow_dispatch` `publish-nuget.yml` (it force-publishes
  all six projects at one version). (b) systemic: make the change-detection always include Core
  whenever any chain project is packed (Core is tiny; this also guarantees the exact-pin Core
  exists), or stop pinning Core to the chain package's own version (e.g. a fixed
  `[1.0.4,)` floor via `PackageReference` with `PrivateAssets`/explicit version). Then add a
  post-publish smoke step doing a fresh `dotnet add package` of each pushed chain package.
  Consider unlisting `1.0.5` / `1.0.6` once a good version is out.
- **Status**: **Fixed in source (branch `audit-4-fixes`); registry still broken until the next NuGet publish runs** — re-verify with a fresh restore afterwards.

### [LOW] Transitive `MessagePack 3.1.8` (via `Algorand5`) has a known moderate advisory

- **Location**: `ARC76Account.Algorand` → `Algorand5 5.0.2.2026101113` → `MessagePack 3.1.8`
  (GHSA-qhrr-8q5h-9q3h, "Nested arrays can blow past buffer length limits"; fixed in 3.1.9).
- **Evidence**: `dotnet list package --vulnerable --include-transitive` flags `ARC76Account.Algorand`
  and `TestARC76`; the build prints `warning NU1902`. All other .NET projects are clean.
- **Impact**: Moderate DoS-class advisory in a deserializer; ARC76's own code (seed/key
  derivation) never deserializes untrusted MessagePack, so it is not on the key-derivation
  path, but applications that use Algorand5's algod models on untrusted data inherit it.
- **Recommendation**: add a direct `<PackageReference Include="MessagePack" Version="3.1.9" />`
  (or newer) to the Algorand project, or bump Algorand5 when it ships the fix.
- **Status**: Fixed in source (`Algorand5` 5.0.3.2026101923; vulnerability scan clean).

### [LOW] (carried) npm publishes with a long-lived classic automation token; NuGet and PyPI use OIDC

- **Location**: `.github/workflows/publish-npm.yml` (`NODE_AUTH_TOKEN: secrets.NPM_TOKEN`), step-scoped.
- **Recommendation**: migrate to npm Trusted Publishing now that all six `@arc76/*` packages exist.
- **Status**: Open (maintainer action; unchanged).

### [LOW] (carried) Stale-but-CVE-free upstream dependencies

- **Description**: `tweetnacl` (last release 2023-04), `cashaddrjs` (2022-04), `buffer`
  (2023-07), `Chaos.NaCl.Standard` (single 2020 release), and on PyPI `base58` (2021-10) and
  `bech32` (2020-02). No known CVEs in any of the scanners. Newly noted: PyPI `base58` /
  `bech32` join the list. Current majors otherwise: `algosdk` 3.6.0 (3.9.0 available),
  `bitcoinjs-lib` 7.0.1 (7.0.4), `ecpair` 3.0.1 (3.0.2), `Nethereum.Signer` 6.1.0 (7.0.0
  available — a major behind), `NBitcoin` 10.0.7 (10.0.17), `Microsoft.Extensions.Options`
  10.0.10 (10.0.13) — all within the declared ranges for npm; NuGet pins are exact.
- **Status**: Open (monitoring). Partially fixed in source (NBitcoin, Options, algosdk, bitcoinjs-lib, ecpair
  bumped). `Nethereum.Signer` 7.x deliberately NOT adopted: a major bump of a type consumers use directly.

### [LOW] (carried) Example/test password is a low-entropy public digit string

- **Description**: Unchanged — the 50-digit `"1234…890"` string appears throughout docs and
  the shared vectors file with the ≥50-character guidance adjacent; anyone using it verbatim
  shares an account with everyone else who did. It is also load-bearing for the cross-language
  KATs.
- **Status**: Open (low priority).

### [INFORMATIONAL] Falcon key derivation relies on young, single-source third-party crypto packages

- **Description**: Falcon keygen is now on the key-derivation path in each language:
  .NET via `Algorand5` (`FalconAccount.FromEntropy`), Python via `algorand-falcon 0.1.0` (Algorand
  Foundation, 2026-08-19) + `py-algorand-sdk>=2.12.0`, TypeScript via `falcon-1024 0.2.0`
  (WASM build of the Algorand C reference; single maintainer `joe-p`, who is also an `algosdk`
  maintainer; one release, 2026-08-04) + `js-sha512 0.9.0` (2024-01). TypeScript additionally
  contains a small local `isEd25519Point` check (BigInt, non-constant-time — it operates only on
  a *public* key hash, so no secret-dependent timing) used to pick the canonical address salt.
- **Evidence**: the three implementations independently agree on all 6 Falcon vectors in
  `test-vectors/arc76-vectors.json` (shipped npm, shipped PyPI, shipped NuGet DLL all executed);
  `pnpm audit --prod` / `npm audit --omit=dev` / `pip-audit` clean.
- **Recommendation**: keep the Falcon vectors as the compatibility gate, watch these three
  packages for releases, and consider pinning `falcon-1024` more tightly than `^0.2.0` (pre-1.0
  caret ranges permit 0.2.x only, which is acceptable — noted for awareness).
- **Status**: No action needed.

### [INFORMATIONAL] `pnpm audit` reports 11 advisories, all dev-only (jest/ts-jest transitives)

- **Description**: `js-yaml`, `brace-expansion`, `sprintf-js`, `handlebars` (2 critical,
  5 high, 4 moderate in total) via `jest` / `ts-jest` test tooling only. `pnpm audit --prod`
  and a fresh `npm audit --omit=dev` over the published packages report **0** vulnerabilities;
  none of these ship in any tarball.
- **Recommendation**: refresh `jest`/`ts-jest` to clear CI noise (jest 30.5.2 / ts-jest 29.4.15
  available).
- **Status**: Partially fixed (dev tooling refreshed; 5 dev-only advisories remain, `--prod` clean).

### [INFORMATIONAL] The `AI_INTEGRATION.md` guides do not mention the Falcon API

- **Description**: Falcon is documented in the three Algorand READMEs (with the shared-secret
  caveat) but the AI-assistant guides do not cover `GetFalconAccount` / `getFalconAccount` /
  `get_falcon_account`, so an assistant may fail to find or may misuse it. The risk is low
  (Falcon docs already carry the right warning) but the guides are in scope as consumer guidance.
- **Recommendation**: add a short Falcon section to `dotnet/`, `typescript/` and `python/`
  `AI_INTEGRATION.md`, repeating that it shares the Ed25519 account's password material.
- **Status**: Fixed in source.

### [INFORMATIONAL] (carried) Legacy packages (`arc76` npm, `AlgorandARC76Account` NuGet) still live, undeprecated

- **Description**: `npm view arc76 deprecated` is empty (re-verified; `arc76@1.0.2`, maintainer
  `scholtz`). `AlgorandARC76Account 1.1.0` is still returned by NuGet search; its deprecation
  flag was not separately queried.
- **Recommendation**: `npm deprecate arc76 "superseded by @arc76/*"`; NuGet deprecation pointing
  at `ARC76Account.*`.
- **Status**: Open (maintainer registry action).

### [INFORMATIONAL] Typosquat sweep: clean

- **Description**: npm: `arc-76`, `arc76js`, `arc76account` do not exist; `arc76` and `@arc76/*`
  are maintainer-owned. NuGet: all `ARC76Account.*` / `AlgorandARC76Account` owned by `Scholtz`
  (one unrelated `Arc56.Generated.…` package; not a plausible confusion target). PyPI: `arc76`,
  `arc-76`, `arc76account`, `pyarc76`, `arc76-account` still 404 (unregistered — consider
  claiming `arc76` defensively); `arc76-*` names are maintainer-owned.
- **Status**: Clean — repeat each audit.

### [INFORMATIONAL] .NET packages target `net10.0` only; GitHub secret/environment configuration unverifiable

- **Description**: a `net9.0` project gets `NU1202` for every `ARC76Account.*` package (they
  support only `net10.0`) — documented/intentional per `Directory.Build.props`, recorded so
  users on older SDKs aren't surprised. Separately, repo-settings secrets, environment
  protection and Actions run logs remain outside this audit's access (carried).
- **Status**: No action needed / tracked.

## Regression check against previous audit (2026-08-04, audit #3)

| Audit #3 finding | Status after independent re-verification |
|---|---|
| **High** — fresh NuGet installs of Bitcoin/BitcoinCash/Solana resolve the pre-fix Core | **Resolved and verified live**: all three are `1.0.4.2026080407`; a fresh `dotnet add package` + `--include-transitive` of each resolves `ARC76Account.Core 1.0.4.2026080407`; shipped DLLs executed against the vectors (NuGet 62/62). |
| **High** — .NET culture-sensitive negative-slot derivation | **Resolved in the published package**: `DeriveIsCultureInvariantForNegativeSlots` (sv-SE, fi-FI, lt-LT, fa-IR, ar-SA × both overloads) passes against the shipped Core `1.0.4.2026080407`. Older Core `1.0.1`/`1.0.2` remain on the registry with the bug but are no longer resolved by any current chain package at its newest 1.0.4. |
| **Low** — npm token vs OIDC | Still open (finding 3). |
| **Low** — stale `tweetnacl`/`cashaddrjs`/`buffer`/`Chaos.NaCl.Standard` | Still open (finding 4); PyPI `base58`/`bech32` added. |
| **Low** — example password is a public digit string | Still open (finding 5). |
| **Low** — TypeScript/Python accept non-integer `slot` | **Resolved and verified**: source asserts integer slots (`assertSlot`/`_assert_slot`); the shipped npm `1.8` rejects `1.5` and `NaN`, shipped PyPI `1.4` rejects `1.5`, `True`, `'1'` (executed). |
| **Info** — Python internal CashAddr encoder | No action; wheel file byte-identical to source, KAT-locked (bitcoincash vectors pass). |
| **Info** — legacy packages undeprecated | Still open (npm re-verified). |
| **Info** — typosquat sweep | Repeated, clean. |
| **Info** — GitHub secrets unverifiable | Unchanged. |
| **Info** — `>=1.0.0` core floors on npm/PyPI | Re-verified: fresh npm install resolves core `1.8.2026101919`; no action. |

## Remediation (same-day follow-up, branch `audit-4-fixes`)

| Finding | Resolution in source |
|---|---|
| High - Algorand uninstallable | `publish-nuget.yml` now always packs Core whenever any chain project is packed, and a new pre-push step restores every packed chain package from the fresh artifacts. **Still needs a `workflow_dispatch`/merge-triggered publish to repair the registry**; re-verify with a fresh restore afterwards. |
| Low - MessagePack advisory | `Algorand5` bumped to `5.0.3.2026101923`; `dotnet list package --vulnerable --include-transitive` is clean for all projects. |
| Low - stale deps | NBitcoin 10.0.17, NBitcoin.Altcoins 6.0.11, Microsoft.Extensions.Options 10.0.13, algosdk ^3.9.0, bitcoinjs-lib ^7.0.4, ecpair ^3.0.2 (62/62 .NET and 60/60 TypeScript tests pass). Chaos.NaCl / tweetnacl / cashaddrjs / buffer / PyPI base58+bech32 have no maintained drop-in replacement and remain monitored. |
| Info - AI guides lack Falcon | Falcon sections (with the shared-secret warning) added to all three `AI_INTEGRATION.md`. |
| Info - dev-only pnpm advisories | jest/ts-jest/prettier/@types/node refreshed; 5 dev-only advisories remain (js-yaml, brace-expansion, sprintf-js via jest coverage tooling; `pnpm audit --prod` is clean; pnpm overrides did not take effect and `sprintf-js` has no patched release yet). |
| Low - npm token, example password; Info - legacy deprecation, PyPI `arc76` name | Not code changes: maintainer registry/account actions or intentionally retained. |

## Items explicitly out of scope or unable to verify

- Triggering the `workflow_dispatch` NuGet republish finding 1 needs — an outward-facing publish
  deliberately left to the maintainer.
- GitHub Actions run logs and repo-settings secrets/environment config; registry account-level
  settings (2FA, token inventory) — no access.
- Byte-for-byte reproducible rebuild of the published artifacts: not performed (version stamping
  makes naive diffs noisy). Verification was by direct execution against known answers, nuspec/
  manifest diffs, and (Python, shipped uncompiled) byte-diffing `.py` files against source. npm
  tarball `dist/` was executed but not diffed against a local build.
- The published `ARC76Account.Algorand 1.0.5/1.0.6` could not be installed from the registry, so
  the 1.0.6 DLL was inspected from the extracted nupkg and executed by referencing it directly
  alongside `Core 1.0.4` and `Algorand5` (62/62 including the 6 Falcon vectors) — i.e. the code
  is correct; only its dependency metadata is broken (finding 1).
- `AlgorandARC76Account` (legacy NuGet) deprecation-flag state was not queried.
