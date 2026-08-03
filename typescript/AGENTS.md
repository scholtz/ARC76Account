# AGENTS.md (TypeScript)

Instructions for AI coding agents (Claude Code, etc.) working on the **TypeScript/npm**
library in this repo, which lives under `typescript/` (this file's directory). This repo also
contains a .NET implementation (`dotnet/`, see `dotnet/AGENTS.md`) and a planned Python one
(`python/`) — see the repo-root [`AGENTS.md`](../AGENTS.md) for the cross-language overview.
Paths below are relative to `typescript/` unless stated otherwise.

## Project overview

Published to npm as [`arc76`](https://www.npmjs.com/package/arc76). Implements the
[ARC76 account standard](https://github.com/algorandfoundation/ARCs/blob/main/ARCs/arc-0076.md):
deterministic account derivation from a password (optionally salted with an email), using
PBKDF2-HMAC-SHA256 (999,999 iterations, 32-byte output) via the WebCrypto `SubtleCrypto` API
(works in both browser and Node via `require("crypto").webcrypto`).

**Scope: Algorand only.** Unlike the `dotnet/` implementation (which has one package per
chain — Algorand, Ethereum, Bitcoin, Bitcoin Cash, Solana), this package currently only exposes
`generateAlgorandAccount`. If asked to add another chain here, treat it as a real feature
addition (new exported function, new tests, new README section) — don't assume the .NET
per-chain architecture is already mirrored here.

## Repo layout

- `src/index.ts` — `generateSeed` (PBKDF2 derivation) and `generateAlgorandAccount` (seed →
  Algorand mnemonic → `algosdk.Account`, via `algosdk`'s `mnemonicFromSeed`/`mnemonicToSecretKey`)
- `src/index.test.ts` — Jest tests
- `package.json` — `build` runs `tsc`, `test` runs `jest`
- `README.md` — public usage docs (published to npm via the `files` field)

## Build & test

```
npm install
npm run build
npm test
```

## Coding conventions

- The PBKDF2 salt/input string format (`ARC-0076-{email}-{password}-{slot}-PBKDF2-999999` /
  `ARC-0076-{email}-{slot}-PBKDF2-999999`, with the `-{email}` segment omitted when no email is
  given) must stay byte-for-byte identical to what `dotnet/ARC76Account.Core/Arc76Seed.cs`
  produces — **the whole point of ARC76 is that the same password/email/slot derives the same
  key across languages and chains.** Verify any change against the .NET known-answer test
  values before touching this.
- This package currently requires passwords ≥ 16 characters (`generateSeed` throws otherwise).
  The `dotnet/` docs recommend ≥ 50 characters as guidance rather than enforcing a hard minimum
  — this is a pre-existing difference between the two implementations, not a bug to
  "reconcile" without being asked; if you do change either one, note the discrepancy in both
  READMEs.
- Keep using the WebCrypto `SubtleCrypto` API for PBKDF2 rather than adding a Node-only crypto
  dependency — that's what keeps this package usable in both browser and Node.

## Security notes

- Never log, print, write to disk, or send over the network the password, derived seed, or
  private key/mnemonic in code or examples.
- Any change to the PBKDF2 parameters (iterations, hash, salt/input format) breaks
  cross-language compatibility with `dotnet/` and is a breaking change for every existing
  derived address — treat it as a major version bump.

## Release process

Version is set in `package.json`. `npm run build` compiles `src/` to `dist/` (the published
`main`/`types` entry points). No CI workflow exists yet.
