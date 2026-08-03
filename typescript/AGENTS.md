# AGENTS.md (TypeScript)

Instructions for AI coding agents (Claude Code, etc.) working on the **TypeScript** libraries
in this repo, which live under `typescript/` (this file's directory). This repo also contains
a .NET implementation (`dotnet/`, see `dotnet/AGENTS.md`) and a planned Python one (`python/`)
— see the repo-root [`AGENTS.md`](../AGENTS.md) for the cross-language overview. Paths below
are relative to `typescript/` unless stated otherwise.

## Project overview

A [pnpm](https://pnpm.io) workspace implementing the
[ARC76 account standard](https://github.com/algorandfoundation/ARCs/blob/main/ARCs/arc-0076.md),
architected as **one npm package per chain plus a shared core package** — a direct mirror of
`dotnet/`'s `ARC76Account.Core` + one-NuGet-package-per-chain structure. Installing
`arc76-bitcoin` must not drag in `algosdk`, `ethers`, or `cashaddrjs`. Don't reintroduce a
single merged package.

- **`packages/core`** (`arc76-core`) — shared PBKDF2 seed derivation (`deriveSeed`,
  `deriveSeedWithEmail`), no chain SDK dependencies. Mirrors
  `dotnet/ARC76Account.Core/Arc76Seed.cs`.
- **`packages/algorand`** (`arc76-algorand`) — `getAccount`/`getEmailAccount` →
  `algosdk.Account`, via `algosdk`.
- **`packages/ethereum`** (`arc76-ethereum`) — → `ethers.Wallet` (`ethers` v6).
- **`packages/bitcoin`** (`arc76-bitcoin`) — → an `ecpair` keypair, via `bitcoinjs-lib` +
  `ecpair` + `tiny-secp256k1`.
- **`packages/bitcoincash`** (`arc76-bitcoincash`) — → an `ecpair` keypair (same key type as
  Bitcoin), via the same libs plus `cashaddrjs` for CashAddr encoding.
- **`packages/solana`** (`arc76-solana`) — → `{ publicKey, secretKey }` (Ed25519 keypair), via
  `tweetnacl` + `bs58`. Deliberately does **not** depend on `@solana/web3.js` — see "Known
  issues" below.

Every chain package exposes the identical async shape:
`getAccount(password, slot = 0)` / `getEmailAccount(email, password, slot = 0)`.

## Repo layout

```
typescript/
  pnpm-workspace.yaml       packages: packages/*
  package.json              private root; shared devDependencies (typescript, jest, ts-jest, prettier); pnpm -r scripts
  tsconfig.base.json        shared compiler options - extended by each package's own tsconfig.json
  jest.preset.js            shared Jest config - spread into each package's own jest.config.js
  packages/<chain>/
    package.json            per-package name/version/deps; "arc76-core": "workspace:*" for chain packages
    tsconfig.json           extends ../../tsconfig.base.json; sets rootDir/outDir locally (path-like
                             options must live in the leaf config - see "Known issues")
    jest.config.js           module.exports = { ...require("../../jest.preset") }
    src/index.ts            getAccount / getEmailAccount
    src/index.test.ts        known-answer tests, reusing dotnet/TestARC76/*.cs's literal values
    README.md
```

## Build & test

```
pnpm install
pnpm build   # pnpm -r run build
pnpm test    # pnpm -r run test
```

Per-package: `pnpm --filter arc76-<chain> build` / `test`. **Build `arc76-core` first** after a
fresh clone or a change to it — the other packages resolve it via its `dist/` output (declared
`main`/`types`), not its TypeScript source, since that's how npm packages normally resolve each
other; if `arc76-core`'s `dist/` is missing or stale, dependent packages' typecheck/build will
fail or use outdated types.

## Coding conventions

- Every chain package's `getAccount`/`getEmailAccount` must call `arc76-core`'s
  `deriveSeed`/`deriveSeedWithEmail` — never re-implement the PBKDF2 call. Never add a
  `require`/`import` on another chain's key library (e.g. `algosdk` inside `arc76-bitcoin`).
- The PBKDF2 salt/input string format must stay byte-for-byte identical to
  `dotnet/ARC76Account.Core/Arc76Seed.cs` — **this is the one invariant shared across the whole
  monorepo, in every language.** Verify any change against the known-answer test values in
  `dotnet/TestARC76/*.cs` before touching `packages/core/src/index.ts`.
- Known-answer tests reuse the exact literal values from `dotnet/TestARC76/*.cs` — this proves
  cross-language compatibility, not just internal correctness. When adding a chain here that
  already exists in `dotnet/`, copy its known-answer values rather than deriving new ones.
- Password minimum length (16 chars, enforced in `arc76-core`) is a hard floor in code; the
  README-level guidance (≥ 50 chars) is stronger and aspirational — don't conflate the two or
  lower either without discussion.

## Known issues / gotchas

- **`tiny-secp256k1` and `ecpair` (used by `arc76-bitcoin`/`arc76-bitcoincash`) are dual
  CJS/ESM packages that TypeScript's `module`/`moduleResolution: "node16"` incorrectly flags**
  when statically `import`-ed (both as values and as types — TS1479/TS1541/TS1542), even
  though both packages have a working `require` conditional export. The fix used here is to
  load them via plain `require(...)` calls (untyped, not a TS `import` declaration) rather than
  dynamic `import()` (which compiles fine but breaks under Jest's default CJS test environment
  without `--experimental-vm-modules`) or a static import (which `tsc` rejects outright). See
  `packages/bitcoin/src/index.ts` for the pattern. If you hit the same TS1479-class error for a
  new chain's dependency, try this pattern before reaching for `ignoreDeprecations` or
  downgrading `moduleResolution`.
- **`@solana/web3.js` (v1.98.x) cannot be required from a CommonJS context** as of this
  writing — its `rpc-websockets` dependency requires `uuid` v14, which is ESM-only, and
  `rpc-websockets`' compiled `.cjs` does a bare `require("uuid")` at load time. This is a real
  upstream incompatibility (reproduces in plain Node, not just Jest), which is why
  `arc76-solana` uses `tweetnacl` + `bs58` directly instead — we only need
  `Keypair`-equivalent functionality (no RPC), so the full SDK isn't worth the broken
  dependency. If `@solana/web3.js` fixes this upstream and a consumer specifically needs a
  `web3.Keypair` instance, document the wrap-it-yourself pattern in the README rather than
  taking the dependency here.
- **Path-like `tsconfig` options (`rootDir`, `outDir`) must be set in each package's own
  `tsconfig.json`, not in `tsconfig.base.json`.** TypeScript resolves relative paths in an
  extended base config relative to the base config's own directory, not the extending file's
  directory — so `"rootDir": "src"` in `tsconfig.base.json` would resolve to
  `typescript/src` (doesn't exist), not `packages/<chain>/src`.

## Adding support for a new chain

Follow the same shape as the existing packages:
1. `mkdir -p packages/<chain>/src`, copy `package.json`/`tsconfig.json`/`jest.config.js` from an
   existing package (e.g. `packages/ethereum`) and adjust name/description/dependencies.
2. Add `"arc76-core": "workspace:*"` plus whatever key library the chain needs.
3. Write `src/index.ts` with `getAccount`/`getEmailAccount` calling `arc76-core`'s
   `deriveSeed`/`deriveSeedWithEmail`.
4. Write `src/index.test.ts`. **If this chain already has a `dotnet/ARC76Account.<Chain>`
   package, reuse its exact known-answer values from `dotnet/TestARC76/<Chain>Tests.cs`** — a
   mismatch means something broke cross-language compatibility, not that the TS test values are
   "different but also correct."
5. `pnpm install` (links the new workspace package), then `pnpm --filter arc76-<chain> build`
   and `test`.
6. Update this file's package list, `README.md` (table + install list + migration guide if
   relevant), and `AI_INTEGRATION.md`.
7. Update the repo-root `AGENTS.md`/`AI_INTEGRATION.md`/`README.md` if they enumerate chains.

## Security notes

- Never log, print, write to disk, or send over the network the password, derived seed, or
  private key/mnemonic in code or examples.
- Any change to the PBKDF2 parameters (iterations, hash, salt/input format) breaks
  cross-language compatibility with `dotnet/` and every other chain package here — treat it as
  a major version bump across all affected packages.

## Versioning

Like `dotnet/`, package versions are **not** hand-maintained in each `package.json` — the
committed `"version"` field (currently `1.0.0` in every package) is just a placeholder for
local dev; CI overwrites it ephemerally right before packing/publishing and never commits the
bump back.

The published version is `1.0.{increment}{yyyyMMddHH}` — e.g. run #7, published
2026-08-03 14:00 UTC, becomes `1.0.72026080314`. **The trailing 10 digits are always the exact
publish timestamp**, so you can read off when a package was last published just from its
version string. This deviates slightly from `dotnet/`'s dotted `1.0.{increment}.{yyyyMMddHH}`
because semver's release core only allows three dot-separated numeric segments — a 4th dotted
segment is invalid semver and `npm publish` rejects it. Semver *build metadata*
(`1.0.7+2026080314`) was considered and rejected: some tooling in the npm/semver ecosystem
silently strips `+build` suffixes (verified empirically — `npm version`/`semver.clean()` do
this), so it wouldn't reliably survive. The concatenated-patch form
(`1.0.{increment}{date}`) is a perfectly ordinary, always-increasing semver patch with no such
risk — verified against the `semver` package directly, including that larger `{increment}`
values always sort higher regardless of the fixed-width date suffix.

`{increment}` is `github.run_number` (monotonically increasing, never resets) in CI. Never
hand-edit a package's committed version to look like the computed scheme — it'll just be
overwritten at publish time anyway, and a stray real-looking version in git is confusing.

Because every chain package's `package.json` declares `"arc76-core": "workspace:^"` (not
`"workspace:*"`) — `pnpm pack`/`pnpm publish` rewrite that to a **caret range** like `^1.0.0`
in the published tarball, not a pin to one exact version. This is deliberate: each of the 6
packages is published from its own isolated CI matrix job (see `.github/workflows/publish-npm.yml`),
so a dependent package's job has no way to know the exact ephemeral version `arc76-core`'s
*own* job happened to publish in the same run. A caret range against core's committed
placeholder major.minor (`^1.0.0`) matches every real published core version we ever produce
(they're all `1.0.{huge number}`, and `^1.0.0` means `>=1.0.0 <2.0.0`) — so this works
correctly without cross-job coordination. If you ever bump the major/minor prefix away from
`1.0`, re-verify this still holds.

## Release process

Publishing is fully automated by `.github/workflows/publish-npm.yml` (repo root, not under
`typescript/`): it triggers on `typescript/**` changes, and for each of the 6 packages
independently, diffs that package's own `typescript/packages/<name>/` folder against the
previous commit — only packages whose own files actually changed get built, tested, and
published. A change to `arc76-core` does **not** cause `arc76-algorand` etc. to be
republished (see "Versioning" above for why that's safe) — bump/touch a dependent's own files
if it needs to be republished. See the root `AGENTS.md`'s "CI/CD" section.
