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
| TypeScript | [`typescript/`](typescript/README.md) | npm: `arc76-algorand`, `arc76-ethereum`, `arc76-bitcoin`, `arc76-bitcoincash`, `arc76-solana`, `arc76-core` | Available   |
| Python     | [`python/`](python/README.md)         | PyPI (planned)                                                                | Not yet implemented |

## Which folder do I want?

- Building a .NET/C# app? → [`dotnet/README.md`](dotnet/README.md) — supports Algorand,
  Ethereum/EVM, Bitcoin, Bitcoin Cash, and Solana, one NuGet package per chain.
- Building a JS/TS app (browser or Node)? → [`typescript/README.md`](typescript/README.md) —
  supports Algorand, Ethereum/EVM, Bitcoin, Bitcoin Cash, and Solana, one npm package per chain,
  managed as a pnpm workspace.
- Python isn't available yet — see [`python/README.md`](python/README.md).

## Shared ARC76 semantics

Every implementation in this repo derives account key material the same way, so a given
password (+ optional email, + optional slot) produces the same underlying key material across
languages and chains: PBKDF2-HMAC-SHA256, 999,999 iterations, salt/input strings of the form
`ARC-0076-...-PBKDF2-999999`, 32-byte seed. Please tell your users to use a password of at
least 50 characters — the account's security is entirely the password's security.

## Repo layout

```
dotnet/       .NET libraries (NuGet) - see dotnet/AGENTS.md for contributor instructions
typescript/   TypeScript/JS library (npm) - see typescript/AGENTS.md
python/       Reserved for a future PyPI package
```

Shared, repo-root files: `LICENSE.txt` (applies to the whole repo), this `README.md`, and
`AGENTS.md`/`AI_INTEGRATION.md` (repo-wide overviews — see those files for how instructions are
split between root and per-language).

## Automated publishing (CI/CD)

Two GitHub Actions workflows handle releases, both using **Trusted Publishing** (OIDC — no
long-lived API keys/tokens stored in this repo):

- **[`.github/workflows/publish-nuget.yml`](.github/workflows/publish-nuget.yml)** — triggers
  only on changes under `dotnet/`, builds/tests/packs all six .NET packages, and pushes with
  `--skip-duplicate` (so a `dotnet/` change that didn't bump the shared version in
  `dotnet/Directory.Build.props` publishes nothing new).
- **[`.github/workflows/publish-npm.yml`](.github/workflows/publish-npm.yml)** — triggers only
  on changes under `typescript/`, and publishes **each** `arc76-*` package independently, only
  if that package's own `package.json` version differs from what's already on the npm registry.

Changes to `audits/` (or anything outside `dotnet/`/`typescript/`) never trigger either
workflow, and neither workflow re-publishes a package/version that's already live — so running
an audit, or touching one language's code, never force-publishes an unrelated or unchanged
package.

**One-time setup required** (not done by these workflows themselves):
- On [nuget.org](https://www.nuget.org): add a Trusted Publishing policy —
  Repository Owner `scholtz`, Repository `ARC76Account`, Workflow File `publish-nuget.yml` —
  plus a repo/environment secret `NUGET_USER` holding your nuget.org username.
- On [npmjs.com](https://www.npmjs.com): add a Trusted Publisher to each `arc76-*` package —
  Organization/user `scholtz`, Repository `ARC76Account`, Workflow filename `publish-npm.yml`.
- Both workflows target a GitHub Actions `environment: release`; either create that
  environment in repo settings (optionally with required reviewers) or remove the
  `environment:` line from both workflow files if you don't want that gate.

## AI-assisted development

See [AGENTS.md](AGENTS.md) if you're an AI coding agent contributing to this repo, or
[AI_INTEGRATION.md](AI_INTEGRATION.md) if you're an AI assistant writing code that *consumes*
one of these packages.
