# AI Integration Guide

This repo publishes ARC76 (deterministic, password-derived blockchain account) libraries in
multiple languages. Pick the guide for the package you're actually writing code against —
don't mix API shapes between them, they are different libraries with different surfaces:

- **.NET** (NuGet: `ARC76Account.Algorand`, `.Ethereum`, `.Bitcoin`, `.BitcoinCash`, `.Solana`) —
  see [`dotnet/AI_INTEGRATION.md`](dotnet/AI_INTEGRATION.md). Supports five chains.
- **TypeScript/npm** (`@arc76/algorand`, `@arc76/ethereum`, `@arc76/bitcoin`,
  `@arc76/bitcoincash`, `@arc76/solana`) — see
  [`typescript/AI_INTEGRATION.md`](typescript/AI_INTEGRATION.md).
- **Python** — not published yet; there is nothing to integrate against.

## The one thing true across all of them

All implementations derive account key material the same way for the same inputs
(PBKDF2-HMAC-SHA256, 999,999 iterations, ARC76's salt/input string format) — so the same
password/email/slot produces the same underlying key material whether you use the .NET or
TypeScript package. This does **not** mean the two packages have the same API, the same chain
coverage, or the same parameter order/validation — check the language-specific guide above
before writing code.
