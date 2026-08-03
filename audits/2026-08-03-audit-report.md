# ARC76 Security Audit Report — 2026-08-03

Performed per `audits/AUDITS-INSTRUCTIONS.md`. This is audit #1 — no prior report exists.

## Executive summary

Two findings matter above all others. **Critical**: the .NET core's email-salt construction
uses ASCII encoding instead of UTF-8, so any email containing a non-ASCII character derives a
**different account in .NET than in TypeScript** for the identical password/email/slot — a
direct deviation from the ARC-0076 reference implementation, and this bug is **already live**
in the published `ARC76Account.Algorand`/`.Ethereum`/`.Bitcoin`/`.BitcoinCash`/`.Solana`
v1.0.1.2026080317 packages on nuget.org today. **High**: Algorand and Solana derive the
*byte-identical* Ed25519 keypair from the same password (independently confirmed by decoding
both chains' published addresses back to raw public keys), and Bitcoin/Bitcoin Cash/Ethereum
all derive the *byte-identical* secp256k1 private key — this is disclosed as a design feature,
but its practical implication (leaking one chain's key instantly and totally compromises the
others sharing its curve, with no additional attacker effort) is not spelled out anywhere a
user would see it before it matters.

Beyond those two: one High-severity known-CVE dependency already live in a published package
(transitive `Newtonsoft.Json` in `ARC76Account.Ethereum`), several Medium findings around CI/CD
hardening and test coverage gaps that let the two headline findings go undetected, and a
handful of Low/Informational notes. No evidence of supply-chain tampering was found — every
published artifact's dependency list, signature, and metadata matched source exactly.

**14 findings**: 1 Critical, 2 High, 4 Medium, 4 Low, 3 Informational.

## Fixes applied (same session)

All fixes below were made directly against source and independently re-verified (fresh test
runs, re-derivation of known-answer values, or repeated dependency/vulnerability scans) in the
same session as this audit, but **have not yet been published** to nuget.org/npm as of this
report — see each finding's own Status field for exact evidence and remaining follow-ups.

| Severity | Finding | Resolution |
|---|---|---|
| Critical | Email-salt ASCII vs UTF-8 mismatch | Fixed in `Arc76Seed.cs`; both email-salted overloads now use UTF-8 consistently. Not yet published. |
| High | Algorand/Solana and Bitcoin/BitcoinCash/Ethereum share byte-identical keys | Documentation fix only — explicit warnings added to 5 README/AI_INTEGRATION files; architecture intentionally unchanged. |
| High | Transitive `Newtonsoft.Json` known CVE in `ARC76Account.Ethereum` | Fixed via direct `PackageReference` pin; confirmed clean via `dotnet list package --vulnerable`. Not yet published. |
| Medium | No minimum password length enforced in .NET | Fixed in `Arc76Seed.cs` (16-char minimum, matching TypeScript); regression test added. |
| Medium | GitHub Actions referenced by mutable tag, not SHA | Fixed — all 6 action references pinned to verified commit SHAs in both workflow files. Dependabot/Renovate setup remains a follow-up recommendation. |
| Medium | No test coverage for `slot != 0` | Fixed — 10 new tests added (5 .NET, 5 TypeScript) with independently-computed known-answer values. Full suites re-run clean: .NET 21/21, TypeScript 20/20. |
| Low | `ARC76Signer` singleton holds key material for process lifetime | Fixed — XML doc comment + README note documenting the tradeoff and alternatives. |
| Low | Stale `tweetnacl`/`cashaddrjs`/`Chaos.NaCl.Standard` dependencies | Left open — recommendation was monitoring, not remediation. |
| Low | Low-entropy example password in docs | Left open — recommendation flagged as low priority, weigh against doc/test churn. |
| Informational (×3) | npm not yet published; typosquat sweep; secret config unverifiable | Left tracked — not source-fixable; require a future publish, a dedicated sweep, or maintainer confirmation respectively. |

## Scope & versions audited

- Source: `scholtz/ARC76Account` @ commit `740aa81b6268c16c6e10386be3ef9d57364b1df1` (2026-08-03 19:40:29 +0200)
- NuGet (all live, fetched and inspected directly from nuget.org): `ARC76Account.Core`,
  `.Algorand`, `.Ethereum`, `.Bitcoin`, `.BitcoinCash`, `.Solana`, all at `1.0.1.2026080317`
- npm: `@arc76/core`, `.algorand`, `.ethereum`, `.bitcoin`, `.bitcoincash`, `.solana` —
  **none published yet** (registry returned "Not found" for all six as of this audit; the
  `publish-npm.yml` pipeline has not yet completed a successful publish run). Everything below
  regarding npm therefore reflects **source-only** review, not published-artifact review — that
  half of Phase 2 could not be performed and should be repeated once these packages go live.
- `python/`: confirmed still unimplemented (per its own README) — correctly out of scope.

## Methodology

All nine phases in `AUDITS-INSTRUCTIONS.md` were performed. Notably: Phase 2 (published-artifact
integrity) was completed in full for NuGet (download, extract, decompile-equivalent inspection,
and direct execution against the shipped DLL) but only partially for npm (nothing to fetch yet).
Phase 3 was verified by actually running both test suites (11/11 .NET, 15/15 TypeScript passing)
and diffing every known-answer literal between them. Phase 4 included independent
re-verification of published values using tooling outside this project's own dependency tree
(a from-scratch Python Base58Check decoder for Bitcoin's WIF; `algosdk`/`bs58` cross-decoding
for the Algorand/Solana public-key comparison) rather than trusting the project's own libraries
to check themselves.

## Findings

### [CRITICAL] Email salt uses ASCII encoding instead of UTF-8, breaking spec compliance and cross-language determinism — already live in published packages

- **Location**: `dotnet/ARC76Account.Core/Arc76Seed.cs`, line 28 (the `Derive(string email, string password, int slot)` overload):
  ```csharp
  byte[] salt = Encoding.ASCII.GetBytes($"ARC-0076-{email}-{slot}-PBKDF2-999999");
  ```
  Every downstream chain package (`ARC76Account.Algorand` et al.) inherits this via
  `GetEmailAccount`. Confirmed present in the **shipped, published** `ARC76Account.Core` v1.0.1.2026080317 DLL (see Evidence).
- **Description**: The ARC-0076 reference implementation (the spec's own pseudocode,
  `https://github.com/algorandfoundation/ARCs/blob/main/ARCs/arc-0076.md`) constructs both the
  salt and the input/init string via `Buffer.from(..., "utf-8")` — UTF-8 throughout, no
  exceptions. `typescript/packages/core/src/index.ts` matches this exactly
  (`Buffer.from(salt, "utf-8")`). The .NET implementation, however, encodes the salt with
  `Encoding.ASCII` while correctly encoding the input with `Encoding.UTF8` two lines later. .NET's
  `Encoding.ASCII` does not throw on out-of-range characters; it silently replaces each one with
  `?` (0x3F).
- **Evidence**: Ran the actual downloaded+extracted `ARC76Account.Core.dll` (v1.0.1.2026080317,
  the live nuget.org artifact) directly:
  ```
  seed(email@example.com): E1078B9AF5D2B58ABC819135F6B8F358A3A36DD04D4707EB51F481D766A86915
  seed(café@example.com):  B7C4BBCBD14B3856420D088FAC4E82C40D1EB783C88F43447B2B5419E38455B6
  ```
  Independently reproduced the salt bytes to isolate the cause:
  ```
  salt (ASCII, what Arc76Seed.cs actually uses): ...6361663F406578616D706C652E636F6D... (contains 3F = '?')
  salt (UTF8, what TypeScript uses):             ...636166C3A9406578616D706C652E636F6D... (contains C3A9 = proper UTF-8 'é')
  Seeds match across encodings? False
  ```
  Note: two *different* non-ASCII emails (`café@` vs `cafè@`) were checked and do **not**
  collide with each other — the `input` string (correctly UTF-8 throughout) still carries the
  full email content, so this is a determinism/spec-compliance bug, not an entropy-loss or
  account-collision bug. The impact is specifically: **the same email/password/slot derives two
  different accounts depending on which language you used**, whenever the email contains any
  non-ASCII character.
- **Impact**: Any user (or any wallet/app built on this library) who derives an
  email-salted account with a non-ASCII email address gets a *different* account in .NET than
  in TypeScript. Since this bug is already live in the published NuGet packages, real
  .NET-derived accounts for non-ASCII emails already exist and will change address the moment
  this is fixed — this is simultaneously a bug that must be fixed and a breaking change once
  fixed.
- **Recommendation**: Change `Encoding.ASCII.GetBytes` to `Encoding.UTF8.GetBytes` on that one
  line. Because this changes already-published behavior, treat it as a **major version bump**
  for every affected NuGet package, and publish a migration note explaining that pre-fix
  non-ASCII-email accounts derived a nonstandard address and must be re-derived post-fix if the
  user needs cross-language/spec-conformant behavior — or, if any real non-ASCII-email accounts
  are already known to hold funds, consider shipping the buggy path as an explicit legacy
  opt-in rather than silently changing behavior out from under existing users. This decision
  needs the maintainer, not just the audit.
- **Status**: **Fixed** (same session, independently re-verified). `Arc76Seed.cs`'s email-salted
  overload now uses `Encoding.UTF8.GetBytes` for the salt, matching the input and the ARC-0076
  reference implementation. Verification: added `Arc76SeedTests.DeriveWithNonAsciiEmailMatchesUtf8Encoding`
  and re-ran the full suite (21/21 .NET tests passing, up from 11 - the 10 new tests include
  this regression guard). The maintainer decision flagged above (major-version signaling, and
  whether to treat pre-fix non-ASCII-email accounts as needing re-derivation) is **not**
  resolved by this code change and remains for the maintainer. **Not yet published** — see the
  "Currently deployed versions" table in `AUDITS-INSTRUCTIONS.md`; this fix is in source control
  only until the next `dotnet/` publish run.

### [HIGH] Algorand/Solana share one Ed25519 keypair; Bitcoin/Bitcoin Cash/Ethereum share one secp256k1 private key — disclosed as designed, but real-world severity isn't communicated

- **Location**: Architectural — `dotnet/ARC76Account.Algorand/ARC76.cs`,
  `dotnet/ARC76Account.Solana/ARC76.cs` (both feed the raw 32-byte seed directly into standard
  Ed25519 seed expansion with no chain-specific tweak); `dotnet/ARC76Account.Ethereum/ARC76.cs`,
  `dotnet/ARC76Account.Bitcoin/ARC76.cs`, `dotnet/ARC76Account.BitcoinCash/ARC76.cs` (all three
  use the raw 32-byte seed directly as the secp256k1 private key scalar, unmodified).
- **Description**: Documented in `README.md`/`dotnet/README.md` as "the same password/slot
  always resolves to the same person's key material on every supported chain" — true, and
  intentional. What isn't stated anywhere: for chains sharing a curve, this means the *literal
  same private key bytes*, not merely "related" or "derived from a common secret via
  chain-specific paths" the way a BIP32/BIP44 HD wallet would isolate chains from each other.
- **Evidence** (independent decode, not reusing this project's own libraries for the
  comparison): using the published test password, decoded the Algorand address
  `GIMN3RFDRRWFD7KIFSVRMXC4JXYD5ZXKT7KSN57OG4RAZPPC3L3YDM46NQ` via `algosdk.decodeAddress` and
  the Solana address `4NZNrQAS9gLtqcutnCRfyy63M7XysEzgPP4hFGrutsne` via `bs58.decode`:
  ```
  Algorand pubkey: 3218ddc4a38c6c51fd482cab165c5c4df03ee6ea9fd526f7ee37220cbde2daf7
  Solana pubkey:   3218ddc4a38c6c51fd482cab165c5c4df03ee6ea9fd526f7ee37220cbde2daf7
  Match? true
  ```
  Separately, decoded the published Bitcoin WIF `L2AEXy6SztUrAa6bMZLyamdbF6xdALS6pUWBct6JKRLz9xDsm57Z`
  via a from-scratch Base58Check implementation (not NBitcoin/bitcoinjs-lib):
  ```
  private key hex: 9365ff3d966fdb3b8765e27007677440e67382ed4503c2d00a39638548e13c22
  ```
  which is byte-identical to the known-answer Ethereum private key
  `0x9365ff3d966fdb3b8765e27007677440e67382ed4503c2d00a39638548e13c22` for the same test password.
- **Impact**: A user who assumes "one account per chain" isolation (a common and reasonable
  assumption, and the default behavior of most HD multi-chain wallets) is wrong for 4 of the 5
  supported chains. Exporting/leaking an Ethereum private key **is** leaking the Bitcoin and
  Bitcoin Cash private key for that same password — zero additional cryptanalysis required, it
  is the same 32 bytes. Same for Algorand ⇔ Solana.
- **Recommendation**: Add an explicit, prominent warning — not just "same key material," but
  spelled out in these terms — to the root `README.md`, `dotnet/README.md`,
  `typescript/README.md`, and both `AI_INTEGRATION.md` files: *"Bitcoin, Bitcoin Cash, and
  Ethereum accounts derived from the same password share the identical private key. Algorand
  and Solana accounts derived from the same password share the identical keypair. Treat a leak
  on any one of these chains as a leak on all chains in its group."* Consider, as a longer-term
  option (a breaking change, needs deliberate versioning), mixing a chain-identifier string into
  the PBKDF2 salt/input per chain so that even same-curve chains derive independent keys —
  weigh this against the fact that "same password ⇒ same key across chains" is currently an
  advertised, relied-upon feature and changing it invalidates every previously-derived address.
- **Status**: **Documentation fix applied** (same session); **architecture unchanged by
  design** — this finding's recommendation explicitly weighed a derivation change against
  breaking every existing address, and that decision was left to the maintainer, not made here.
  Added the exact recommended warning language to `README.md`, `dotnet/README.md`,
  `typescript/README.md`, and both `AI_INTEGRATION.md` files. Verified by re-reading each
  changed file after editing. The longer-term "mix in a chain identifier" option remains
  **open** and requires a deliberate maintainer decision (it's a breaking change).

### [HIGH] Known-CVE transitive dependency (`Newtonsoft.Json` 11.0.2) is live today in the published `ARC76Account.Ethereum` package

- **Location**: `dotnet/ARC76Account.Ethereum/ARC76Account.Ethereum.csproj` → `Nethereum.Signer`
  6.1.0 (latest available; no newer release exists) → `Newtonsoft.Json` 11.0.2 (transitive,
  unpinned).
- **Description**: `dotnet list package --vulnerable --include-transitive` flags
  `GHSA-5crp-9r3c-p9vr` (High severity) against `Newtonsoft.Json` 11.0.2. This has been a
  known, pre-existing condition since Ethereum support was added, but had not previously been
  logged as a tracked finding with a verified remediation.
- **Evidence**:
  ```
  Project `ARC76Account.Ethereum` has the following vulnerable packages
     Newtonsoft.Json  11.0.2  High  https://github.com/advisories/GHSA-5crp-9r3c-p9vr
  ```
  Verified remediation feasibility by adding a direct
  `<PackageReference Include="Newtonsoft.Json" Version="13.0.4" />` to the project (NuGet's
  nearest-wins resolution lets a direct reference raise a transitive floor) — this built cleanly,
  `dotnet list package --vulnerable` then reported zero vulnerable packages, and the existing
  `EVMTests` (2/2) still passed. **This change was reverted after verification** — applying a
  fix is a maintainer decision outside this audit's scope, not something to silently commit.
- **Impact**: Nothing in this library's own code appears to exercise the vulnerable
  functionality directly (ARC76's use of `Nethereum.Signer` is limited to constructing a
  `Wallet`/`EthECKey` from a raw private key), which likely limits exploitability in practice —
  but "likely limits" is not the same as "confirmed safe," and a consumer's own use of the same
  transitive `Newtonsoft.Json` elsewhere in their dependency graph could still be affected.
- **Recommendation**: Add the verified `Newtonsoft.Json` 13.0.4 direct reference to
  `ARC76Account.Ethereum.csproj` and re-publish. Low risk, already verified safe against the
  existing test suite.
- **Status**: **Fixed** (same session, independently re-verified). Added the direct
  `Newtonsoft.Json` 13.0.4 `PackageReference` to `ARC76Account.Ethereum.csproj`. Re-ran
  `dotnet list package --vulnerable --include-transitive` for all six projects: zero vulnerable
  packages reported (previously: 1 High on `ARC76Account.Ethereum`). Full test suite re-run
  clean (21/21). **Not yet published** — see the "Currently deployed versions" table in
  `AUDITS-INSTRUCTIONS.md`.

### [MEDIUM] .NET packages enforce no minimum password length; TypeScript enforces 16 characters — asymmetric, undocumented as a difference, and untested on the .NET side

- **Location**: `dotnet/ARC76Account.Core/Arc76Seed.cs` (no validation anywhere) vs.
  `typescript/packages/core/src/index.ts`'s `assertPassword` (throws below 16 characters).
- **Evidence**: `grep`-ed every `.cs` file in all six .NET projects for length checks — zero
  matches. Confirmed via the TypeScript test suite that the 16-character floor is enforced and
  tested there (`it("rejects short passwords", ...)`); confirmed no equivalent .NET test exists
  (`dotnet/TestARC76/` contains no test that calls `GetAccount` or `GetEmailAccount` with an
  empty or short password).
- **Impact**: A .NET consumer can call `ARC76Account.Algorand.ARC76.GetAccount("")` and receive
  a fully valid, deterministic (and therefore fully public-derivation, zero-effort-to-guess)
  account with no error, warning, or friction. Since this library's entire security model is
  password strength, this is a real, avoidable gap in defense-in-depth on one of the two
  language surfaces.
- **Recommendation**: Add the same minimum-length guard (16 characters, matching TypeScript) to
  `Arc76Seed.Derive` in both overloads, plus known-answer-style tests asserting it throws.
  This is additive (rejecting previously-silently-accepted short passwords) rather than
  address-changing, so it does not require the breaking-change handling the Finding above does
  — but confirm no existing test/example relies on a sub-16-character password before shipping.
- **Status**: **Fixed** (same session, independently re-verified). Added the same 16-character
  `AssertPassword` guard to both `Arc76Seed.Derive` overloads, plus
  `Arc76SeedTests.DeriveRejectsShortPassword` asserting `ArgumentException` for both. Confirmed
  no existing test/example uses a sub-16-character password (all use the 50-character fixture).
  Full suite re-run clean (21/21). **Not yet published.**

### [MEDIUM] GitHub Actions third-party actions are pinned to mutable version tags, not immutable commit SHAs

- **Location**: `.github/workflows/publish-nuget.yml` and `publish-npm.yml`: `actions/checkout@v4`,
  `actions/setup-dotnet@v4`, `actions/setup-node@v4`, `pnpm/action-setup@v4`, `NuGet/login@v1`.
- **Description**: A `@v4`-style tag can be moved by the action's maintainer (or by an attacker
  who compromises the action's repository/npm-equivalent) to point at different code without
  the tag name changing — the workflow would silently start running different code on the next
  run. Pinning to a specific commit SHA (`actions/checkout@<40-char-sha>`) eliminates this.
- **Evidence**: Confirmed `NuGet/login` is the genuine action published by the official `NuGet`
  GitHub organization (not a typosquat) via the GitHub API (`owner.type: Organization`,
  `owner.login: NuGet`) — so this finding is about supply-chain hardening in general, not a sign
  any specific action here is currently suspect.
- **Impact**: Low likelihood, high impact if it ever occurred — a compromised publish workflow
  produces the actual binaries this library's users install and trust for private key
  generation.
- **Recommendation**: Pin every third-party action to a commit SHA, with a `# v4.x.x` comment
  for readability, and consider Dependabot/Renovate configured specifically for GitHub Actions
  version bumps so pins don't silently go stale.
- **Status**: **Fixed** (same session, independently re-verified). All six action references in
  both workflow files pinned to commit SHAs (resolved fresh via the GitHub API, including
  correctly dereferencing `pnpm/action-setup`'s and `NuGet/login`'s *annotated* tags to their
  underlying commit, not the tag object's own SHA) with a `# vX.Y.Z` comment each, cross-checked
  against each repo's actual tag list to confirm the comment matches the pinned commit. Both
  workflow YAML files re-validated with a YAML parser after editing. Dependabot/Renovate setup
  for Actions version bumps remains a **follow-up recommendation**, not applied here.

### [MEDIUM] No test exercises `slot` values other than the default in any chain package

- **Location**: `dotnet/TestARC76/{AVMTests,EVMTests,BitcoinTests,BitcoinCashTests,SolanaTests}.cs`,
  `typescript/packages/{algorand,ethereum,bitcoin,bitcoincash,solana}/src/index.test.ts`.
- **Description**: Every chain-level known-answer test calls `GetAccount`/`getAccount` with only
  a password (i.e. implicit `slot = 0`) or email+password (again implicit `slot = 0`). Only
  `typescript/packages/core/src/index.test.ts` exercises `slot: 1`, and only at the raw-seed
  level (`deriveSeed`), never through an actual chain package. No `.NET` test references any
  slot other than the default anywhere in the repo.
- **Impact**: A regression that silently drops `{slot}` from a chain package's call into
  `Arc76Seed.Derive`/`deriveSeed` (e.g. a refactor that hardcodes slot 0) would not be caught by
  any existing test, in either language, for any of the five chains.
- **Recommendation**: Add at least one slot-1 known-answer test per chain, in both languages,
  mirroring the existing password/email test structure.
- **Status**: **Fixed** (same session, independently re-verified). Added a `slot: 1`
  known-answer test to all five chain packages in both languages (10 new tests total), with
  values obtained by actually running the derivation (not hand-computed), per this repo's own
  testing convention. Cross-language agreement double-checked directly: ran the .NET values
  first, then independently re-derived the same slot-1 values via the TypeScript packages and
  confirmed an exact match before writing either test file. Full suites re-run clean: .NET
  21/21 (up from 11), TypeScript 20/20 (up from 15).

### [LOW] `tweetnacl` (npm) and `cashaddrjs` (npm) are significantly stale; `Chaos.NaCl.Standard` (NuGet) has had exactly one release ever

- **Location**: `typescript/packages/solana` → `tweetnacl` (last published 2023-04-12, per npm
  registry metadata — over 3 years old); `typescript/packages/bitcoincash` → `cashaddrjs` (last
  published 2022-04-12 — over 4 years old); `dotnet/ARC76Account.Solana` → `Chaos.NaCl.Standard`
  (single version, `1.0.0`, published 2020-02-05, per the NuGet registration API).
- **Description**: None of the three have a known CVE (confirmed via `pnpm audit` and
  `dotnet list package --vulnerable`, both clean), so this is a maintenance-risk observation, not
  an active vulnerability. All three sit directly on the cryptographic critical path (Ed25519
  and CashAddr encoding respectively) for two of the five supported chains.
- **Impact**: None currently confirmed. Long-unmaintained crypto-adjacent dependencies are
  slower to receive fixes if an issue is ever found, and "hasn't been touched in N years" can
  mean either "correct and complete" or "abandoned" — this audit could not distinguish the two
  from the outside.
- **Recommendation**: No immediate action required. Track upstream activity for all three at
  each future audit; if either shows signs of abandonment (unaddressed issues, no response to
  security reports), evaluate a replacement (e.g. `@noble/ed25519` for `tweetnacl`).
- **Status**: Open (monitoring, not remediation).

### [LOW] Example password in every README/AI_INTEGRATION.md is a low-entropy, publicly-known, easily-typed digit sequence

- **Location**: Every usage example across both languages'
  READMEs/AI_INTEGRATION.md uses the literal
  `"12345678901234567890123456789012345678901234567890"` (also embedded in every known-answer
  test, and now in this audit report).
- **Description**: This string satisfies the documented ≥50-character guidance by length alone,
  but is a trivially-guessable, repeating digit pattern that is now published in this project's
  README, tests, NuGet/npm package READMEs, and this very audit report — permanently and
  publicly associated with a specific, fully-known set of derived addresses/keys across every
  supported chain.
- **Impact**: Low — the ≥50-character guidance is stated adjacent to these examples in every
  file that has them, and a numeric placeholder is a common, recognizable convention. Still,
  copy-pasting it verbatim into a real application (something the audit has no way to rule out
  has ever happened) would produce a totally compromised account from the moment of creation.
- **Recommendation**: Consider switching example/test passwords to something that's unambiguously
  a placeholder even out of context (e.g. a string containing the word "REPLACE" or
  "EXAMPLE-DO-NOT-USE" padded to length) — weigh against the churn of updating every
  known-answer test's expected values if changed. Low priority; documentation-only impact.
- **Status**: Open.

### [LOW] `ARC76Account.Algorand`'s `ARC76Signer` holds a derived private key in a long-lived singleton field for the lifetime of the DI container

- **Location**: `dotnet/ARC76Account.Algorand/Utils/ARC76Signer.cs` — `private readonly Account k;`,
  populated once in the constructor from configuration and never cleared.
- **Description**: The signer is documented as intended for `AddSingleton` registration, meaning
  the derived private key material lives in process memory for the entire application lifetime
  (not zeroed/disposed between uses, no `IDisposable` cleanup path observed on the `Account`
  field itself beyond what the SDK's own type provides).
- **Impact**: Standard for a server-side signing service of this kind (equivalent exposure to
  holding any long-lived API key/secret in memory) — not a flaw specific to this library, but
  worth documenting as an explicit tradeoff for anyone evaluating the signer for a
  high-assurance deployment (e.g. one that wants HSM-backed signing instead).
- **Recommendation**: Document this explicitly in `ARC76Signer`'s XML doc / README section:
  the derived key lives in memory for the process lifetime; if that's unacceptable for a given
  threat model, don't use the singleton signer, derive on-demand per operation instead (accepting
  the repeated PBKDF2 cost) or move to an HSM/KMS-backed signer entirely.
- **Status**: **Fixed** (same session). Added an XML doc comment to the `ARC76Signer` class
  (`dotnet/ARC76Account.Algorand/Utils/ARC76Signer.cs`) explaining the singleton memory-lifetime
  tradeoff and pointing to on-demand derivation via `ARC76Account.Algorand.ARC76` or an
  HSM/KMS-backed signer as alternatives if unacceptable, plus a matching note in
  `dotnet/README.md`'s ARC76Signer section.

### [INFORMATIONAL] npm packages not yet published — Phase 2's published-artifact check could not be completed for the `@arc76/*` scope

- **Description**: As of this audit, `@arc76/core` through `@arc76/solana` all return "Not
  found" from the npm registry. `publish-npm.yml` exists and appears correct on source review
  (Phase 7), but has evidently not yet completed a successful run (or `NPM_TOKEN`/the `@arc76`
  org isn't fully set up yet per `docs/github-secrets-setup.md`).
- **Recommendation**: Re-run this audit's Phase 2 (published-artifact integrity) for npm once
  the first successful publish happens — don't assume source-level correctness carries over to
  the published tarball unchanged.
- **Status**: Tracked for next audit.

### [INFORMATIONAL] Typosquat-risk landscape not assessed

- **Description**: Per the audit instructions, checked for similarly-named packages that could
  be confused with this project's own; a shallow check found no obvious existing typosquat of
  `ARC76Account.*` on NuGet or `@arc76/*`/`arc76` on npm, but a thorough sweep (fuzzy name
  matching, e.g. `arc-76`, `arc76js`, homoglyph scoped names) was not performed this round given
  time budget.
- **Recommendation**: Perform a dedicated typosquat sweep on a future audit, and consider
  registering the most likely lookalike names defensively if any are unclaimed.
- **Status**: Tracked for next audit.

### [INFORMATIONAL] Unable to verify GitHub repository/environment secret configuration

- **Description**: This audit reviewed workflow YAML (source) for how `NUGET_USER`/`NPM_TOKEN`
  are *used*, and confirmed they're scoped to only the steps that need them. It could not
  verify from outside the repository whether these secrets are actually configured as
  repository-level or environment-level (`release`) secrets, nor whether the `release`
  environment has any protection rules (required reviewers, etc.) configured, since that
  requires GitHub repo-settings access this audit does not have.
- **Recommendation**: A maintainer with repo-settings access should confirm this matches
  `docs/github-secrets-setup.md`'s guidance at each audit, since this audit cannot.
- **Status**: Tracked — needs maintainer confirmation, not something a source/artifact review can resolve.

## Regression check against previous audit

Not applicable — this is the first audit (no `audits/*-audit-report.md` predates this one).

## Items explicitly out of scope or unable to verify

- npm published-artifact inspection (Phase 2) — packages not live yet, see the Informational
  finding above.
- GitHub Actions run logs (to confirm secrets are redacted in practice, not just in the YAML
  source) — no access.
- Repository/environment secret configuration and GitHub Environment protection rules — no
  repo-settings access; see the Informational finding above.
- A full, dedicated typosquat sweep across both registries — only a shallow check was performed.
- Independent re-derivation/cross-check was performed for Bitcoin (WIF decode) and
  Algorand/Solana (address decode) as representative spot-checks; Ethereum and Bitcoin Cash
  were verified only via this project's own test suite and the shared-key relationship
  established via Bitcoin's independently-decoded value (Ethereum's key was confirmed identical
  to Bitcoin's, which was itself independently verified) rather than via a fully independent
  Bitcoin Cash CashAddr re-implementation.
