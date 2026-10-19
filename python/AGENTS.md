# AGENTS.md (Python)

Instructions for AI coding agents (Claude Code, etc.) working on the **Python** libraries in
this repo, which live under `python/` (this file's directory). This repo also contains a .NET
implementation (`dotnet/`, see `dotnet/AGENTS.md`) and a TypeScript one (`typescript/`, see
`typescript/AGENTS.md`) — see the repo-root [`AGENTS.md`](../AGENTS.md) for the cross-language
overview. Paths below are relative to `python/` unless stated otherwise.

## Project overview

A collection of independently-published PyPI packages implementing the
[ARC76 account standard](https://github.com/algorandfoundation/ARCs/blob/main/ARCs/arc-0076.md),
architected as **one package per chain plus a shared core package** — a direct mirror of
`dotnet/`'s `ARC76Account.Core` + one-NuGet-package-per-chain structure and `typescript/`'s
`@arc76/*` pnpm workspace. Installing `arc76-bitcoin` must not drag in `py-algorand-sdk`,
`eth-account`, or `pynacl`. Don't reintroduce a single merged package.

All packages share the `arc76` top-level [namespace package](https://packaging.python.org/en/latest/guides/packaging-namespace-packages/)
(PEP 420, implicit — no `__init__.py` at the `arc76/` level in any package) rather than a
scoped/prefixed import name, so `import arc76.algorand` reads naturally while
`pip install arc76-algorand` mirrors `npm i @arc76/algorand`. **Never add an `arc76/__init__.py`
to any package** — that would turn `arc76` into a regular package in whichever package happens
to get imported first and break the namespace split for every other `arc76-*` package installed
alongside it.

- **`packages/core`** (`arc76-core`) — shared PBKDF2 seed derivation (`derive_seed`,
  `derive_seed_with_email`), stdlib only (`hashlib.pbkdf2_hmac`), no chain SDK dependencies.
  Mirrors `dotnet/ARC76Account.Core/Arc76Seed.cs` and `typescript/packages/core`.
- **`packages/algorand`** (`arc76-algorand`) — `get_account`/`get_email_account` →
  `AlgorandAccount(address, private_key, mnemonic)`, via `py-algorand-sdk` (address/mnemonic
  encoding) + `pynacl` (Ed25519 keypair expansion from the raw seed). Also `get_falcon_account`/`get_email_falcon_account` → `AlgorandFalconAccount` (post-quantum Falcon-1024) behind the optional `falcon` extra (`algorand-falcon`); known-answer addresses are shared with `dotnet/` and `typescript/`.
- **`packages/ethereum`** (`arc76-ethereum`) — → `eth_account.signers.local.LocalAccount`, via
  `eth-account`.
- **`packages/bitcoin`** (`arc76-bitcoin`) — → `BitcoinAccount(private_key, public_key)` with
  `.address()`/`.wif()` methods, via `coincurve` (libsecp256k1 bindings) + `base58` +
  `pycryptodome` (RIPEMD-160) + `bech32`.
- **`packages/bitcoincash`** (`arc76-bitcoincash`) — → `BitcoinCashAccount` (same key type as
  Bitcoin), via the same libs plus an internal `_cashaddr.py` (no maintained CashAddr package
  exists on PyPI — see "Known issues" below).
- **`packages/solana`** (`arc76-solana`) — → `SolanaAccount(public_key, secret_key)` (Ed25519
  keypair), via `pynacl`. Deliberately does **not** depend on `solana-py`/`solders` — only
  `Keypair`-equivalent functionality (no RPC) is needed.

Every chain package exposes the identical **synchronous** shape:
`get_account(password, slot=0)` / `get_email_account(email, password, slot=0)`. This is
deliberately *not* `async`, unlike `typescript/` — Python's `hashlib.pbkdf2_hmac` is a
synchronous, CPU-bound OpenSSL call with no natural async form (the TS packages are `async`
only because browser WebCrypto's `subtle.deriveBits` is async-only). Don't add `async def`
wrappers to "match" the other languages.

## Repo layout

```
python/
  packages/<chain>/
    pyproject.toml        per-package name/version/deps; hatchling build backend
    src/arc76/<chain>/
      __init__.py          get_account / get_email_account
      py.typed             marks the package as PEP 561 typed
      _cashaddr.py          bitcoincash only - internal CashAddr encoder
    tests/test_<chain>.py   known-answer tests, reusing dotnet/TestARC76/*.cs's literal values
    README.md
```

There is no repo-root `pyproject.toml`/workspace tool (unlike `typescript/`'s pnpm workspace) —
each package is fully independent; install/build/test them individually. `arc76-core` has no
path dependency wiring to worry about since PyPI (unlike a local pnpm workspace) always
resolves it from the index, even for another package in this same repo — see "Build & test".

## Build & test

Each package is a standalone project. From a package's own directory:

```
pip install -e .            # editable install (pulls arc76-core and chain deps from PyPI)
pip install -e . pytest     # if pytest isn't already available
python -m pytest
```

There's no cross-package build step to run first — unlike `typescript/`'s workspace (where
`@arc76/core` must be built before dependents can resolve its `dist/` output),
`arc76-core`'s *published* PyPI release is what every other package's `pyproject.toml`
declares as a dependency. When developing `arc76-core` and a dependent package together
locally, either `pip install -e ../core` first, or `pip install -e .[dev]`-style local overrides
— don't assume installing a dependent package alone will pick up uncommitted `core` changes.

## Coding conventions

- Every chain package's `get_account`/`get_email_account` must call `arc76.core`'s
  `derive_seed`/`derive_seed_with_email` — never re-implement the PBKDF2 call. Never import
  another chain's key library into a different chain's package (e.g. `eth_account` inside
  `arc76.bitcoin`).
- The PBKDF2 salt/input string format must stay byte-for-byte identical to
  `dotnet/ARC76Account.Core/Arc76Seed.cs` — **this is the one invariant shared across the whole
  monorepo, in every language.** Verify any change against the known-answer test values in
  `dotnet/TestARC76/*.cs` before touching `packages/core/src/arc76/core/__init__.py`.
- Known-answer tests reuse the exact literal values from `dotnet/TestARC76/*.cs` (also mirrored
  in `typescript/packages/*/src/index.test.ts`) — this proves cross-language compatibility, not
  just internal correctness. When adding a chain here that already exists in `dotnet/`, copy its
  known-answer values rather than deriving new ones.
- Password minimum length (16 chars, enforced in `arc76.core`) is a hard floor in code; the
  README-level guidance (≥ 50 chars) is stronger and aspirational — don't conflate the two or
  lower either without discussion.
- Public API is `snake_case` (`get_account`, `get_email_account`), matching PEP 8 — do not
  mirror the TypeScript packages' camelCase or add camelCase aliases.
- Prefer the standard library and well-audited, chain-canonical libraries over hand-rolled
  cryptography: `hashlib.pbkdf2_hmac` (core), `coincurve`/libsecp256k1 (Bitcoin family),
  `pynacl`/libsodium (Ed25519 chains), `eth-account` (Ethereum), `py-algorand-sdk` (Algorand).
  The one deliberate exception is CashAddr encoding (see "Known issues") — that's plain
  encoding logic, not a cryptographic primitive.

## Known issues / gotchas

- **`hashlib`'s `ripemd160` is not guaranteed available.** It depends on OpenSSL's legacy
  provider being enabled, which OpenSSL 3.0+ disables by default on many distros. `arc76.bitcoin`
  and `arc76.bitcoincash` use `pycryptodome`'s `Crypto.Hash.RIPEMD160` instead, which ships its
  own implementation and needs no OpenSSL legacy provider. Don't switch this back to
  `hashlib.new("ripemd160", ...)` even though it works locally — it silently breaks on hosts
  where the legacy provider isn't loaded.
- **No maintained CashAddr package exists on PyPI** (the one that does, `cashaddress`, is
  effectively unmaintained). `arc76.bitcoincash._cashaddr` implements the P2PKH-only encoder
  directly against the [CashAddr spec](https://github.com/bitcoincashorg/bitcoincash.org/blob/master/spec/cashaddr.md),
  verified against the same known-answer values as `dotnet`/`typescript`. Keep it internal
  (leading underscore, not exported from `arc76.bitcoincash`'s `__all__`) — it's not meant to be
  a general-purpose CashAddr library, only what this package needs.
- **`arc76-algorand`'s `AlgorandAccount.private_key` is a base64-encoded 64-byte expanded
  secret key** (py-algorand-sdk's own convention: `seed || public_key`), not the raw 32-byte
  ARC76 seed. This matches what `algosdk.account`/`algosdk.mnemonic`/`algosdk.transaction`
  expect as input elsewhere in `py-algorand-sdk`, and is analogous to `.sk` on the TypeScript
  package's `algosdk.Account` return value.
- **PyNaCl (`nacl.signing.SigningKey`) is used for Ed25519 key expansion in both
  `arc76-algorand` and `arc76-solana`**, matching `tweetnacl` (TypeScript) and `Chaos.NaCl`
  (.NET) bit-for-bit — all three implement the same RFC 8032 Ed25519 seed expansion, which is
  why the known-answer values are identical across languages.

## Adding support for a new chain

Follow the same shape as the existing packages:
1. `mkdir -p packages/<chain>/src/arc76/<chain>`, copy `pyproject.toml` from an existing package
   (e.g. `packages/ethereum`) and adjust name/description/dependencies. Add a `tests/` dir and a
   `py.typed` marker file (empty) inside `src/arc76/<chain>/`.
2. Add `"arc76-core>=1.0.0"` plus whatever key library the chain needs to `dependencies` in
   `pyproject.toml`.
3. Write `src/arc76/<chain>/__init__.py` with `get_account`/`get_email_account` calling
   `arc76.core`'s `derive_seed`/`derive_seed_with_email`.
4. Write `tests/test_<chain>.py`. **If this chain already has a `dotnet/ARC76Account.<Chain>`
   package, reuse its exact known-answer values from `dotnet/TestARC76/<Chain>Tests.cs`** — a
   mismatch means something broke cross-language compatibility, not that the Python test values
   are "different but also correct."
5. `pip install -e packages/core` then `pip install -e packages/<chain>`, then
   `python -m pytest` from inside `packages/<chain>`.
6. Update this file's package list, `README.md` (table + install list), and
   `AI_INTEGRATION.md`.
7. Update the repo-root `AGENTS.md`/`AI_INTEGRATION.md`/`README.md` if they enumerate chains.

## Security notes

- Never log, print, write to disk, or send over the network the password, derived seed, or
  private key/mnemonic in code or examples.
- Any change to the PBKDF2 parameters (iterations, hash, salt/input format) breaks
  cross-language compatibility with `dotnet/`/`typescript/` and every other chain package here
  — treat it as a major version bump across all affected packages.
- Prefer well-audited libraries backed by OpenSSL/libsecp256k1/libsodium over pure-Python
  cryptography implementations for anything security-sensitive (see "Coding conventions").

## Versioning & release process

Like `dotnet/`/`typescript/`, package versions are **not** hand-maintained in each
`pyproject.toml` — the committed `"version"` field (currently `1.0.0` in every package) is just
a placeholder for local dev; CI overwrites it ephemerally right before building/publishing and
never commits the bump back. See the repo-root `AGENTS.md`'s "CI/CD" section and
`.github/workflows/publish-pypi.yml` for the actual mechanics, and `typescript/AGENTS.md`'s
"Versioning" section for the full rationale behind the `1.{increment}.{yyyyMMddHH}` scheme
(PyPI, like npm, only allows a 3-segment release core, so Python packages use the same scheme
npm does, not .NET's 4-segment one).
