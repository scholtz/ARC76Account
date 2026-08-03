# ARC76 Security Audit — Instructions

**Trigger phrase:** when the user types `do new audit` (in this repo, in any conversation),
treat it as a request to execute the full procedure in this document and produce a new dated
report under `audits/`. Everything below is written so that instruction alone is sufficient —
do not ask the user to re-explain scope or methodology; it's all here.

## Why this document is written the way it is

This library's entire purpose is **private key generation** — the single most
security-critical operation in the cryptocurrency/blockchain ecosystem. A private key is not
"a" secret among many; it *is* the asset. Get key derivation wrong, and every account ever
derived from it is compromised, silently, possibly years after the fact, with no way to
"patch" a key that's already been used on-chain.

This library also does something a typical crypto library doesn't: it **deliberately replaces
a random number generator with a deterministic function of user-supplied data** (password,
optional email, slot). That is an unusual and higher-risk design by nature — a normal wallet's
private key is drawn from a CSPRNG with ~256 bits of entropy by construction; an ARC76 account's
effective entropy is whatever the user's password happens to contain, run through PBKDF2. That
shifts a large part of the security burden from "did we implement RNG correctly" (a solved
problem) to "did we implement key stretching correctly, and is our guidance strong enough that
real users end up with adequate entropy" (a much easier place to get subtly wrong).

Treat every audit performed under this document as if a client were paying **US $100,000** for
a third-party cryptographic security review, because that is the level of rigor the stakes
justify: assume nothing, verify everything against source and against what is *actually
published*, write down what you checked and how (not just conclusions), and never mark a
finding resolved without independently re-verifying the fix. A cheap, superficial audit of a
private-key library is worse than no audit — it gives users false confidence.

## Scope

Audit **all of the following**, every time, in full — this is not a "spot check the diff"
exercise. A new audit is a fresh, complete pass, not an incremental review of what changed
since the last one (though §9 below does ask you to diff against the previous report for
regression tracking).

### In scope

**Source code** (this repository, `master` branch, current `HEAD` unless told otherwise):
- `dotnet/ARC76Account.Core/` — shared PBKDF2 seed derivation (the single most important file
  in the whole repo: `Arc76Seed.cs`)
- `dotnet/ARC76Account.Algorand/`, `.Ethereum/`, `.Bitcoin/`, `.BitcoinCash/`, `.Solana/` —
  per-chain key construction from that seed
- `dotnet/ARC76Account.Algorand/Utils/ARC76Signer.cs` — holds a derived private key in memory
  and signs transactions with it; audit this as carefully as key derivation itself
- `typescript/packages/core/` — the TypeScript equivalent of `Arc76Seed.cs`
- `typescript/packages/{algorand,ethereum,bitcoin,bitcoincash,solana}/` — the TypeScript
  per-chain packages
- `.github/workflows/publish-nuget.yml`, `.github/workflows/publish-npm.yml` — the pipelines
  that turn source into what users actually install
- `dotnet/Directory.Build.props`, `typescript/package.json`, every `packages/*/package.json`,
  every `.csproj` — dependency declarations and version/build metadata

**Published artifacts** (see §2 — you must fetch and inspect these, not just the source that
supposedly produced them):
- NuGet: `ARC76Account.Core`, `ARC76Account.Algorand`, `ARC76Account.Ethereum`,
  `ARC76Account.Bitcoin`, `ARC76Account.BitcoinCash`, `ARC76Account.Solana`
- npm: `@arc76/core`, `@arc76/algorand`, `@arc76/ethereum`, `@arc76/bitcoin`,
  `@arc76/bitcoincash`, `@arc76/solana`
- (If `python/` has gained a published package by the time you run this, add it to scope —
  check `python/README.md`'s status first.)

**Documentation**, insofar as it makes security- or entropy-relevant claims a user might rely
on: root `README.md`, `dotnet/README.md`, `typescript/README.md`, every package README, both
`AI_INTEGRATION.md` files (an AI assistant following stale/wrong guidance from these files
*is* a security risk for this library specifically).

### Out of scope

- `python/` if still unimplemented (confirm via its README each time — don't assume).
- `audits/` itself (this folder; don't recurse into your own output).
- General code style/architecture opinions that have no security bearing — that's a separate
  concern (`/code-review`), not this audit.
- Anything requiring access you don't have (e.g. private GitHub Actions run logs, npm/nuget.org
  account settings) — note what you couldn't check in the report rather than guessing.

## The one property everything else is checked against

Every chain's key derivation must trace back to the **same 32-byte PBKDF2 output** for the same
`(password, email, slot)`, computed as:

- iterations: exactly `999999`
- hash: exactly `SHA-256`
- salt: `ARC-0076-{slot}-PBKDF2-999999` (no email) or `ARC-0076-{email}-{slot}-PBKDF2-999999`
- input/password material: `ARC-0076-{password}-{slot}-PBKDF2-999999` (no email) or
  `ARC-0076-{email}-{password}-{slot}-PBKDF2-999999`

This is the ARC76 spec
(https://github.com/algorandfoundation/ARCs/blob/main/ARCs/arc-0076.md). Any deviation from
this — in any language, in any package, published or in source — is a **Critical** finding: it
either breaks cross-language/cross-wallet compatibility silently, or weakens the derivation.
`dotnet/ARC76Account.Core/Arc76Seed.cs` is the canonical reference implementation; every other
package (including `typescript/packages/core`) must match it byte-for-byte, verified by
actually running the derivation and comparing output, not by reading the code and asserting it
"looks equivalent."

## Methodology

Work through every phase below, for every package in scope. Don't skip a phase because a
previous audit covered it — re-verify.

### 1. Cryptographic core review

- Read `Arc76Seed.cs` and `packages/core/src/index.ts` line by line. Confirm: iteration count,
  hash algorithm, salt construction, input construction, output length (32 bytes), and that
  there is no code path that silently falls back to a weaker configuration (fewer iterations,
  a faster/weaker hash, a shorter or omitted salt) under any input, including empty string,
  unicode, or extremely long password/email values.
- Confirm the PBKDF2 implementation source is a real, standard cryptographic primitive
  (`System.Security.Cryptography.Rfc2898DeriveBytes` / WebCrypto `SubtleCrypto`) — not a
  hand-rolled reimplementation. A hand-rolled PBKDF2/HMAC/SHA-256 in a key-derivation library
  is an automatic Critical finding regardless of how correct it looks.
- Confirm salt and input are built from the *exact* literal strings above with no extra/missing
  characters, no locale-dependent string formatting (e.g. a `slot` number formatted with
  thousands separators in some culture), and no encoding mismatch between the salt string and
  the input string (both must be treated as raw UTF-8/ASCII bytes consistently across every
  language implementation).
- Confirm there is no randomness anywhere in the derivation path. A CSPRNG call anywhere
  between "user provides password" and "seed is produced" would make results non-reproducible
  and is a Critical finding for this specific library (the entire point is determinism).

### 2. Published-artifact integrity (supply-chain / tampering check)

This is the check that distinguishes an audit of "the published libraries" from an audit of
just the git repo — a compromised CI runner, a compromised maintainer npm/NuGet account, or a
dependency-confusion attack could all make what's actually installable diverge from what's in
this repository, without changing a single line here.

- For each of the 12 in-scope packages, find its latest published version:
  - NuGet: `https://api.nuget.org/v3-flatcontainer/{id-lowercase}/index.json`, or
    `dotnet package search`
  - npm: `npm view @arc76/<name> version`, or the registry API
- Download the actual package artifact (`.nupkg` / npm tarball) — don't rely on `dotnet add`/
  `npm install` alone, fetch the raw file so you can inspect it directly.
- Unzip/untar it and check:
  - The dependency list embedded in the package metadata (`.nuspec` / `package.json` inside the
    tarball) exactly matches what's declared in this repo's source for that version — no extra
    dependencies, no widened version ranges, nothing that isn't in the source-controlled
    `.csproj`/`package.json`.
  - No unexpected files (anything beyond what `Directory.Build.props`'s `<ItemGroup>` /
    `package.json`'s `"files"` field would produce — e.g. no stray `.env`, no build scripts that
    shouldn't be there, no a second copy of a `dist`/`bin` with different contents).
  - The compiled/transpiled code, decompiled or read directly, actually matches what the
    corresponding tagged commit/version in source control would produce — spot-check the core
    PBKDF2 parameters (§1) directly in the shipped artifact, not just in source. If you can
    rebuild the package locally from the matching source version and diff it byte-for-byte
    against the published artifact, do that — it's the strongest possible check.
  - Package metadata (author, repository URL, license) matches expectations (`scholtz`,
    `https://github.com/scholtz/ARC76Account`, MIT) — a takeover or typosquat would often show
    up here first.
- Check npmjs.com/nuget.org for any **other**, similarly-named package that could plausibly be
  typosquatted against this project (e.g. `arc-76`, `arc76js`, `ARC76Account.Core` vs a
  lookalike) and note it as an informational finding if found, since users could be tricked
  into installing the wrong thing — this isn't something we can fix in our own package, but
  it's a real risk to the people this audit exists to protect, worth documenting.

### 3. Cross-language / cross-chain equivalence verification

- Actually run the derivation (don't just read code) for a fixed, documented test vector in
  every language and confirm identical output at every layer:
  seed bytes → chain-specific key → derived address / exported private key string.
- Reuse `dotnet/TestARC76/*.cs`'s known-answer values as ground truth, and confirm the
  TypeScript tests in `typescript/packages/*/src/index.test.ts` assert the *same* literal
  values (per `typescript/AGENTS.md`'s stated convention) — if any test file has silently
  drifted to asserting a *different* value than its .NET counterpart, that is itself a Critical
  finding (it means the last person to touch it broke cross-language compatibility and the test
  was "fixed" to hide it rather than the bug being fixed).
- For each chain, confirm slot 0 and slot 1 (at minimum) produce different keys, and that a
  password produces a different key than the same password+an email (i.e. confirm the salt/input
  actually changes the output — a bug that accidentally makes `slot`/`email` a no-op would
  silently collapse many users onto the same handful of addresses).

### 4. Chain-specific correctness review (per chain, both languages)

For each of Algorand, Ethereum/EVM, Bitcoin, Bitcoin Cash, Solana:
- Confirm the seed→key construction matches that chain's actual standard (Ed25519 seed
  expansion for Algorand/Solana; raw secp256k1 private key for Ethereum/Bitcoin/Bitcoin Cash) —
  re-derive independently against each chain's own reference tooling/library where practical
  (e.g. does `algosdk`/`bitcoinjs-lib`/`ethers` agree that the derived value is a valid key on
  that chain's curve, and that the address encoding is correct for that chain?).
- Confirm the exported "private key" representation is the standard one for that chain (WIF
  for Bitcoin/Bitcoin Cash, hex `0x`-prefixed for EVM, base58 64-byte secret for Solana, the
  SDK's native `Account`/mnemonic form for Algorand) and isn't truncated, byte-swapped, or
  otherwise mis-encoded.
- Confirm address encoding uses the current standard for that chain (native SegWit for Bitcoin,
  CashAddr for Bitcoin Cash — see `dotnet/AGENTS.md`/`typescript/AGENTS.md` for the documented
  rationale) and hasn't regressed to a legacy/deprecated format.
- Confirm no chain package accidentally imports or depends on another chain's SDK (verify via
  the actual published dependency list from §2, not just intent in the source) — a Bitcoin
  package that somehow pulls in `algosdk` is both a real bloat/attack-surface issue and a sign
  something else may be wrong.

### 5. Entropy & user-guidance review

This is the review that's unique to this library's threat model — get this wrong and users
lose funds regardless of how correct the cryptography is.

- Confirm every README/AI_INTEGRATION.md that shows a usage example either uses an obviously
  fake placeholder password of adequate length, or explicitly calls out that the example
  password is not suitable for production. Flag any example that could be copy-pasted verbatim
  into production code as-is.
- Confirm the ≥50-character password recommendation is stated prominently (not buried) in every
  package's own README, not just the repo root.
- Confirm the enforced minimum (16 characters in the TypeScript packages; check whether the
  .NET packages enforce *any* minimum at all — if they don't, that's a real asymmetry between
  the two ecosystems worth flagging even though it doesn't itself invalidate the cryptography)
  is actually enforced in the shipped code, not just documented.
- Confirm there is no code path, example, or default parameter value anywhere that could lead a
  user to derive a "real" account from a short/weak/well-known string without a deliberate,
  explicit choice to do so.
- Confirm the docs are clear that **password strength is the entire security model** — that
  there is no additional entropy contributed by the library itself, and that anyone who can
  guess or brute-force the password (offline, since derivation is public/deterministic and
  attacker-repeatable at the same cost as the legitimate user) fully compromises the account.
  This is the single most important fact a user of this library needs to internalize; audit the
  docs for whether they actually convey it in those terms, not just as a generic "use a strong
  password" platitude.

### 6. Dependency & supply-chain review

- Run the vulnerability scanners: `dotnet list package --vulnerable --include-transitive` for
  every .NET project; `npm audit` / `pnpm audit` for the TypeScript workspace. Record every
  finding, including transitive ones, even if a previous audit dismissed a similar one — CVE
  databases update continuously.
- For every direct dependency (`Algorand4`, `Nethereum.Signer`, `NBitcoin`,
  `NBitcoin.Altcoins`, `Solnet.Wallet`, `Chaos.NaCl.Standard`, `algosdk`, `ethers`,
  `bitcoinjs-lib`, `ecpair`, `tiny-secp256k1`, `cashaddrjs`, `tweetnacl`, `bs58`): confirm it's
  still actively maintained (check last release date), confirm the installed version isn't
  pinned to something long-abandoned or explicitly yanked/deprecated, and confirm there's no
  unexplained major-version lag behind current upstream that would indicate the dependency was
  never revisited since initial adoption.
- Confirm lockfiles (`typescript/pnpm-lock.yaml`, any `packages.lock.json` if .NET central
  package management/lockfiles are ever adopted) are committed and that CI actually installs
  from them (`--frozen-lockfile` / restore-locked) rather than re-resolving floating ranges on
  every run — a workflow that silently re-resolves dependencies on every publish is a
  dependency-confusion/supply-chain risk even if the lockfile exists.
- Confirm `tiny-secp256k1`/`ecpair` (loaded via the `require()` workaround documented in
  `typescript/AGENTS.md`'s "Known issues") haven't changed their `require` conditional export in
  a way that would make the workaround silently fall through to something unexpected — re-run
  the actual build/test to confirm this still works cleanly, don't just trust the prior audit's
  note that it did.

### 7. Build & release pipeline review

- Read both workflow files (`publish-nuget.yml`, `publish-npm.yml`) fresh, as if reviewing them
  for the first time. Confirm: no secret is ever echoed/logged; the change-detection logic
  (per-project `git diff`) can't be tricked into skipping a real change or including an
  unintended one; the version-computation logic can't collide or go backwards; `NODE_AUTH_TOKEN`
  /`NUGET_USER` are only used in the minimum scope needed (not exported to steps that don't need
  them).
- Confirm third-party GitHub Actions used (`actions/checkout`, `actions/setup-dotnet`,
  `actions/setup-node`, `pnpm/action-setup`, `NuGet/login`) are referenced by a tag from a
  reputable publisher; note as a finding (Medium — supply-chain hardening opportunity, not an
  active compromise) if any are pinned to a mutable tag (e.g. `@v4`) rather than an immutable
  commit SHA, since a compromised action tag could inject arbitrary code into the publish
  pipeline that produces the keys/addresses this whole library exists to get right.
- Confirm `audits/` (this folder) is excluded from both workflows' path triggers, per
  `AGENTS.md`'s stated invariant — if that invariant has been silently broken, note it as a
  Medium finding (not itself a crypto risk, but the process guarantee the user explicitly
  asked for is broken).

### 8. Test coverage adequacy review

- Enumerate every test file per package. Confirm coverage includes, at minimum: password-only
  derivation, email+password derivation, at least two different `slot` values, and (per §3) a
  cross-check that different inputs really do produce different outputs (not just that the
  happy path produces *a* value).
- Identify untested edge cases and record them as findings even if you can't fix them in the
  audit itself: empty-string password (should this be rejected? is it?), very long passwords,
  passwords/emails containing characters requiring multi-byte UTF-8 encoding, `slot` at 0 vs.
  negative vs. very large integers.
- Run the full test suite yourself (`dotnet test` in `dotnet/`; `pnpm test` in `typescript/`)
  and report actual pass/fail, not assumed-passing from reading the code.

### 9. Historical regression check

- Find the most recent prior report in `audits/` (see naming convention below). If one exists,
  re-check every finding it recorded as "Open" or "Needs follow-up" and update its status.
  Never mark a finding "Resolved" without independently re-verifying the fix yourself (re-run
  the relevant check from the phase above) — do not trust a commit message or PR description
  claiming it was fixed.
- If no prior report exists, state that explicitly in the new report (this is audit #1) rather
  than silently omitting the section.

## Severity classification

Use this rubric for every finding; don't invent ad hoc severities.

- **Critical** — directly compromises private key confidentiality, determinism, or
  cross-implementation consistency (e.g. wrong PBKDF2 parameters, a randomness leak, a
  hand-rolled crypto primitive, a language whose derivation silently disagrees with the
  reference implementation). Ship-stopping; recommend pulling the affected published version(s)
  if already live.
- **High** — a real weakening of the security model that doesn't break determinism/consistency
  outright (e.g. a documented-but-unenforced password minimum that's much lower than the
  guidance, an unpinned dependency with a known exploitable CVE in the code path this library
  actually exercises).
- **Medium** — a supply-chain or process hardening gap that increases risk but requires another
  precondition to be exploitable (e.g. mutable Action tags, a lockfile that exists but isn't
  enforced in CI, missing `--frozen-lockfile`).
- **Low** — a real but narrow-impact issue (e.g. an inconsistency between two languages'
  enforced minimums, a stale dependency with no known CVE).
- **Informational** — no direct risk today, but worth the user/maintainer knowing (e.g. a
  typosquat risk on the registries, a documentation clarity gap).

## Report format

Save each audit as `audits/YYYY-MM-DD-audit-report.md` (UTC date you ran it; if you run more
than one in a day, append `-2`, `-3`, ...). Use this structure:

```markdown
# ARC76 Security Audit Report — {date}

## Executive summary
{2-4 sentences: overall posture, count of findings by severity, headline risk if any}

## Scope & versions audited
{exact package versions fetched in §2, exact commit SHA of source reviewed}

## Methodology
{one line per phase in this document confirming it was performed - "see AUDITS-INSTRUCTIONS.md
§N" is fine, don't repeat the instructions verbatim}

## Findings

### [SEVERITY] Short title
- **Location**: file/package/registry artifact
- **Description**: what's wrong, in concrete terms
- **Evidence**: the actual command output / diff / test result that demonstrates it — not just
  an assertion
- **Impact**: what happens to a real user if this isn't fixed
- **Recommendation**: concrete fix
- **Status**: Open / Needs follow-up / Resolved (only after independent re-verification per §9)

{repeat per finding, most severe first}

## Regression check against previous audit
{per §9 - what was open, what's now resolved-and-reverified, what's still open}

## Items explicitly out of scope or unable to verify
{be honest about gaps - e.g. "could not access GitHub Actions run logs to confirm secret
redaction in practice, only reviewed workflow YAML"}
```

## Non-negotiable ground rules

- **Verify, don't assume.** If you didn't actually run it (a test, a derivation, a dependency
  scan, a package download), don't describe it in the report as if you did.
- **No false confidence.** If a check couldn't be completed (no network access to a registry,
  no ability to run a particular tool), say so explicitly in the report rather than omitting it
  or implying it passed.
- **A finding isn't resolved until re-verified.** This is the rule most real audits skip under
  time pressure, and the one most likely to matter for a private-key library — don't skip it
  here.
- **This document itself is in scope for improvement.** If, while performing an audit, you find
  a real gap in this methodology (a check that should exist but doesn't), add it in the same
  pass and note in the report that the instructions were updated — but do not weaken or remove
  an existing check without the user explicitly asking for that.
