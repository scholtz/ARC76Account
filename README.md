# ARC76 Account

<p align="center"><img src="assets/arc76-logo-512.png" alt="ARC76 Account" width="220"></p>

Multi-language implementations of the
[Algorand ARC76 Account Standard](https://github.com/algorandfoundation/ARCs/blob/main/ARCs/arc-0076.md):
deterministic, password-derived blockchain accounts (optionally salted with an email).

This repo hosts one top-level folder per language/ecosystem. Each is independently versioned
and published, and has its own detailed README:

| Language   | Folder                        | Published as                                                                 | Status      |
|------------|--------------------------------|-------------------------------------------------------------------------------|-------------|
| .NET       | [`dotnet/`](dotnet/README.md)         | NuGet: `ARC76Account.Algorand`, `.Ethereum`, `.Bitcoin`, `.BitcoinCash`, `.Solana`, `.Core` | Available   |
| TypeScript | [`typescript/`](typescript/README.md) | npm: `@arc76/algorand`, `@arc76/ethereum`, `@arc76/bitcoin`, `@arc76/bitcoincash`, `@arc76/solana`, `@arc76/core` | Available   |
| Python     | [`python/`](python/README.md)         | PyPI: `arc76-algorand`, `arc76-ethereum`, `arc76-bitcoin`, `arc76-bitcoincash`, `arc76-solana`, `arc76-core` | Available   |

## Which folder do I want?

- Building a .NET/C# app? → [`dotnet/README.md`](dotnet/README.md) — supports Algorand,
  Ethereum/EVM, Bitcoin, Bitcoin Cash, and Solana, one NuGet package per chain.
- Building a JS/TS app (browser or Node)? → [`typescript/README.md`](typescript/README.md) —
  supports Algorand, Ethereum/EVM, Bitcoin, Bitcoin Cash, and Solana, one npm package per chain,
  managed as a pnpm workspace.
- Building a Python app? → [`python/README.md`](python/README.md) — supports Algorand,
  Ethereum/EVM, Bitcoin, Bitcoin Cash, and Solana, one PyPI package per chain, all under the
  shared `arc76.*` namespace package.

## Packages

Every package below derives account key material identically (same password/email/slot ⇒ same
underlying key) — see [Shared ARC76 semantics](#shared-arc76-semantics).

### .NET (NuGet)

One shared [`dotnet/README.md`](dotnet/README.md) covers all six — there's no per-package
README on the .NET side.

| Package                                                                    | Chain              | What it depends on               |
|------------------------------------------------------------------------------|--------------------|-------------------------------------|
| [`ARC76Account.Core`](https://www.nuget.org/packages/ARC76Account.Core)         | — (shared seed derivation, not usually installed directly) | no chain SDK |
| [`ARC76Account.Algorand`](https://www.nuget.org/packages/ARC76Account.Algorand) | Algorand (AVM)     | `Algorand4`; also ships `ARC76Signer`/`IAlgorandSigner` for ASP.NET Core |
| [`ARC76Account.Ethereum`](https://www.nuget.org/packages/ARC76Account.Ethereum) | Ethereum / EVM     | `Nethereum.Signer`                  |
| [`ARC76Account.Bitcoin`](https://www.nuget.org/packages/ARC76Account.Bitcoin)   | Bitcoin            | `NBitcoin`                          |
| [`ARC76Account.BitcoinCash`](https://www.nuget.org/packages/ARC76Account.BitcoinCash) | Bitcoin Cash | `NBitcoin` + `NBitcoin.Altcoins`     |
| [`ARC76Account.Solana`](https://www.nuget.org/packages/ARC76Account.Solana)     | Solana             | `Solnet.Wallet` + `Chaos.NaCl.Standard` |

### TypeScript (npm, `@arc76` organization)

Each has its own README, linked below.

| Package                                                              | Chain              | What it depends on               |
|--------------------------------------------------------------------------|--------------------|-------------------------------------|
| [`@arc76/core`](https://www.npmjs.com/package/@arc76/core) ([README](typescript/packages/core/README.md)) | — (shared seed derivation, not usually installed directly) | no chain SDK |
| [`@arc76/algorand`](https://www.npmjs.com/package/@arc76/algorand) ([README](typescript/packages/algorand/README.md)) | Algorand (AVM) | `algosdk` |
| [`@arc76/ethereum`](https://www.npmjs.com/package/@arc76/ethereum) ([README](typescript/packages/ethereum/README.md)) | Ethereum / EVM | `ethers` v6 |
| [`@arc76/bitcoin`](https://www.npmjs.com/package/@arc76/bitcoin) ([README](typescript/packages/bitcoin/README.md)) | Bitcoin | `bitcoinjs-lib` + `ecpair` + `tiny-secp256k1` |
| [`@arc76/bitcoincash`](https://www.npmjs.com/package/@arc76/bitcoincash) ([README](typescript/packages/bitcoincash/README.md)) | Bitcoin Cash | `bitcoinjs-lib` + `ecpair` + `cashaddrjs` |
| [`@arc76/solana`](https://www.npmjs.com/package/@arc76/solana) ([README](typescript/packages/solana/README.md)) | Solana | `tweetnacl` + `bs58` |

### Python (PyPI)

Each has its own README, linked below.

| Package                                                              | Chain              | What it depends on               |
|--------------------------------------------------------------------------|--------------------|-------------------------------------|
| [`arc76-core`](https://pypi.org/project/arc76-core/) ([README](python/packages/core/README.md)) | — (shared seed derivation, not usually installed directly) | Python standard library only |
| [`arc76-algorand`](https://pypi.org/project/arc76-algorand/) ([README](python/packages/algorand/README.md)) | Algorand (AVM) | `py-algorand-sdk` + `pynacl` |
| [`arc76-ethereum`](https://pypi.org/project/arc76-ethereum/) ([README](python/packages/ethereum/README.md)) | Ethereum / EVM | `eth-account` |
| [`arc76-bitcoin`](https://pypi.org/project/arc76-bitcoin/) ([README](python/packages/bitcoin/README.md)) | Bitcoin | `coincurve` (libsecp256k1) + `bech32` + `pycryptodome` |
| [`arc76-bitcoincash`](https://pypi.org/project/arc76-bitcoincash/) ([README](python/packages/bitcoincash/README.md)) | Bitcoin Cash | `coincurve` (libsecp256k1) + internal CashAddr encoder + `pycryptodome` |
| [`arc76-solana`](https://pypi.org/project/arc76-solana/) ([README](python/packages/solana/README.md)) | Solana | `pynacl` (libsodium) |

## Shared ARC76 semantics

Every implementation in this repo derives account key material the same way, so a given
password (+ optional email, + optional slot) produces the same underlying key material across
languages and chains: PBKDF2-HMAC-SHA256, 999,999 iterations, salt/input strings of the form
`ARC-0076-...-PBKDF2-999999`, 32-byte seed. Please tell your users to use a password of at
least 50 characters — the account's security is entirely the password's security.

> ⚠️ **Same-curve chains share the literal same private key, not just "related" keys.**
> Bitcoin, Bitcoin Cash, and Ethereum accounts derived from the same password/email/slot are
> the **identical secp256k1 private key** — not merely derived from a common secret via
> separate chain-specific paths (the way a BIP32/BIP44 HD wallet isolates chains from each
> other), but the exact same 32 bytes, just encoded differently per chain. Algorand and Solana
> accounts derived from the same password/email/slot are likewise the **identical Ed25519
> keypair**. Practically: leaking your Ethereum private key **is** leaking your Bitcoin and
> Bitcoin Cash private key for that password — no additional attacker effort required. If you
> need isolation between chains, use a different password (or `slot`) per chain rather than
> assuming this library gives you that isolation by default.

## Repo layout

```
dotnet/       .NET libraries (NuGet) - see dotnet/AGENTS.md for contributor instructions
typescript/   TypeScript/JS library (npm) - see typescript/AGENTS.md
python/       Python library (PyPI) - see python/AGENTS.md
```

Shared, repo-root files: `LICENSE.txt` (applies to the whole repo), this `README.md`, and
`AGENTS.md`/`AI_INTEGRATION.md` (repo-wide overviews — see those files for how instructions are
split between root and per-language).

## Automated publishing (CI/CD)

Three GitHub Actions workflows handle releases:

- **[`.github/workflows/publish-nuget.yml`](.github/workflows/publish-nuget.yml)** — triggers
  only on changes under `dotnet/`, then builds+tests everything but only packs+pushes the
  specific project(s) whose own folder actually changed (e.g. a Bitcoin-only change doesn't
  republish the other five). Publishes via NuGet **Trusted Publishing** (OIDC — no API key
  stored anywhere).
- **[`.github/workflows/publish-npm.yml`](.github/workflows/publish-npm.yml)** — triggers only
  on changes under `typescript/`, and independently builds+tests+publishes **each** `@arc76/*`
  package only if that specific package's own folder changed. Publishes via a classic npm
  **Automation token** (`NPM_TOKEN` secret) — npm's OIDC Trusted Publishing requires each
  package to already exist on the registry, which isn't the case yet for six brand-new `@arc76/*`
  packages; see [`docs/github-secrets-setup.md`](docs/github-secrets-setup.md) for how to
  switch to it later.
- **[`.github/workflows/publish-pypi.yml`](.github/workflows/publish-pypi.yml)** — triggers
  only on changes under `python/`, and independently builds+tests+publishes **each** `arc76-*`
  package only if that specific package's own folder changed. Publishes via PyPI **Trusted
  Publishing** (OIDC — no stored API token; PyPI supports registering a "pending" trusted
  publisher for a project before it's ever been published, unlike npm at the time
  `publish-npm.yml` was written).

Changes to `audits/` (or anything outside `dotnet/`/`typescript/`/`python/`) never trigger any
of these workflows — so running an audit, or touching one language's code, never force-publishes
an unrelated or unchanged package.

**Versioning**: every published version encodes exactly when it was published, so you never
need to look anywhere else to find out. .NET packages use `1.0.{increment}.{yyyyMMddHH}` (e.g.
`1.0.42.2026080314` — run #42, 2026-08-03 14:00 UTC); npm and PyPI packages use the closest
semver/PEP-440-legal equivalent, `1.{increment}.{yyyyMMddHH}` (`{increment}` in the minor slot,
the plain publish timestamp in the patch slot, since neither format allows a 4th dotted release
segment — e.g. `1.42.2026080314`). `{increment}` is the GitHub Actions run number, which only
ever goes up. See `dotnet/AGENTS.md`/`typescript/AGENTS.md`/`python/AGENTS.md` for the full
rationale.

**One-time setup required** (not done by these workflows themselves) — see
[`docs/github-secrets-setup.md`](docs/github-secrets-setup.md) for the full walkthrough:
- A nuget.org Trusted Publishing policy + a `NUGET_USER` GitHub secret (your nuget.org
  username).
- An `@arc76` npm organization + an npm Automation token stored as the `NPM_TOKEN` GitHub
  secret.
- A pypi.org Trusted Publishing policy for each of the six `arc76-*` projects (can be created
  as "pending" publishers before the projects exist).
- All three workflows target a GitHub Actions `environment: release`; either create that
  environment in repo settings (optionally with required reviewers) or remove the
  `environment:` line from the workflow files if you don't want that gate.

## AI-assisted development

See [AGENTS.md](AGENTS.md) if you're an AI coding agent contributing to this repo, or
[AI_INTEGRATION.md](AI_INTEGRATION.md) if you're an AI assistant writing code that *consumes*
one of these packages.
