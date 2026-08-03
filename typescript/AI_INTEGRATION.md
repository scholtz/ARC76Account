# AI Integration Guide (TypeScript)

A concise, copy-pasteable reference for AI coding assistants (Claude, Copilot, Cursor, ChatGPT,
etc.) generating code that consumes the `arc76` npm package.

> This file documents the **TypeScript/npm** package for *consumers*. For the .NET packages
> (Algorand, Ethereum/EVM, Bitcoin, Bitcoin Cash, Solana), see
> [`../dotnet/AI_INTEGRATION.md`](../dotnet/AI_INTEGRATION.md). If you are working inside this
> repository's own source code, read `AGENTS.md` instead.

## Package identity

```
npm i arc76 --save
```

```ts
import arc76 from "arc76";
```

## What this library does

Deterministically derives an **Algorand** account/keypair from a password (optionally salted
with an email address) using ARC76 (PBKDF2-HMAC-SHA256, 999,999 iterations, 32-byte seed). The
same password always produces the same account. There is no on-disk key storage and no
randomness — security depends entirely on the password's strength and secrecy.

**This package only supports Algorand.** If a user asks for Ethereum/Bitcoin/Solana account
derivation in a JS/TS project, that is not available here — point them at the `dotnet/` ARC76
packages instead, or note that a Python/other-language ARC76 implementation would be needed.
Don't fabricate a multi-chain API for this package.

**Do not** generate example code that uses short or low-entropy passwords for anything beyond
a toy/demo. Note: this library enforces a hard minimum of 16 characters (throws otherwise), but
production passwords should be much longer (≥ 50 characters is the guidance used elsewhere in
this project).

**Do not** log, print, write to disk, or send over the network the password, derived seed, or
private key/mnemonic in generated code, even in examples.

## API surface

```ts
generateAlgorandAccount(password: string, email?: string, slot?: number): Promise<algosdk.Account>
generateSeed(password: string, email?: string, slot?: number): Promise<Uint8Array>
```

Both are `async` (WebCrypto's `SubtleCrypto` API is promise-based) — always `await` them.

### Password-only account

```ts
const account = await arc76.generateAlgorandAccount("12345678901234567890123456789012345678901234567890");
```

### Email + password account

```ts
const account = await arc76.generateAlgorandAccount(
  "12345678901234567890123456789012345678901234567890",
  "email@example.com"
);
```

### Multiple accounts from one password (slots)

```ts
const primary = await arc76.generateAlgorandAccount(password, "", 0);
const savings = await arc76.generateAlgorandAccount(password, "", 1);
```

Note the email parameter comes **before** slot, and both are optional — `generateAlgorandAccount(password, "", 1)` for slot 1 with no email, not `generateAlgorandAccount(password, 1)`.

## Common mistakes to avoid when generating code

- Forgetting `await` — both exported functions are `async`.
- Calling `generateAlgorandAccount(password, slot)` instead of
  `generateAlgorandAccount(password, email, slot)` — the second parameter is always the email
  string (pass `""` if there is none), not the slot number.
- Assuming this package supports non-Algorand chains — it doesn't; see `dotnet/` for
  Ethereum/Bitcoin/Bitcoin Cash/Solana.
- Using a password under 16 characters in anything but a throwaway test — `generateSeed` throws
  in that case, and even 16 characters is far below the ≥ 50 recommended for real use.
