# ARC76 Account (TypeScript)

<p align="center"><img src="../assets/arc76-logo-512.png" alt="ARC76 Account" width="180"></p>

> Part of the [ARC76 Account monorepo](../README.md), which also has a
> [.NET implementation](../dotnet/README.md).

TypeScript implementation of the
[Algorand ARC76 Account Standard](https://github.com/algorandfoundation/ARCs/blob/main/ARCs/arc-0076.md):
deterministic, password-derived accounts, with one npm package and one namespace per supported
chain — mirroring the `dotnet/` implementation's architecture (`ARC76Account.Core` +
one package per chain).

This is a [pnpm](https://pnpm.io) workspace (`packages/*`); each package below is published to
npm independently.

## Supported chains

| Chain              | Package               | Underlying key library      |
|--------------------|-------------------------|-------------------------------|
| Algorand (AVM)     | [`@arc76/algorand`](packages/algorand/README.md)     | `algosdk`               |
| Ethereum / EVM     | [`@arc76/ethereum`](packages/ethereum/README.md)     | `ethers` v6              |
| Bitcoin            | [`@arc76/bitcoin`](packages/bitcoin/README.md)       | `bitcoinjs-lib` + `ecpair` |
| Bitcoin Cash       | [`@arc76/bitcoincash`](packages/bitcoincash/README.md) | `bitcoinjs-lib` + `ecpair` + `cashaddrjs` |
| Solana             | [`@arc76/solana`](packages/solana/README.md)         | `tweetnacl` + `bs58` |
| —                  | [`@arc76/core`](packages/core/README.md) (shared seed derivation, not usually installed directly) | — |

Every chain package exposes the same shape:

```ts
getAccount(password: string, slot?: number): Promise<...>;
getEmailAccount(email: string, password: string, slot?: number): Promise<...>;
```

`slot` (default `0`) lets a single password derive multiple independent accounts on the same
chain. See each package's own README for its exact return type and how to get an
address/private key out of it (it varies by underlying library, same as in `dotnet/`).

## Install

```
npm i @arc76/algorand --save     # Algorand (AVM)
npm i @arc76/ethereum --save     # Ethereum / EVM-compatible chains
npm i @arc76/bitcoin --save      # Bitcoin
npm i @arc76/bitcoincash --save  # Bitcoin Cash
npm i @arc76/solana --save       # Solana
```

Install only the chain package(s) you need — `@arc76/core` (the shared PBKDF2 seed derivation)
comes in transitively.

Please use a password of at least 50 characters for real accounts — the account's security is
entirely the password's security. (Each package enforces a 16-character minimum in code; treat
that as a hard floor, not a recommendation.)

## Cross-language compatibility

Every package here derives the exact same key material as the equivalent `dotnet/` package for
the same password/email/slot (PBKDF2-HMAC-SHA256, 999,999 iterations, ARC76's salt/input string
format). Each package's tests reuse the literal known-answer values from
`dotnet/TestARC76/*.cs` to prove this.

## Development (this workspace)

```
pnpm install
pnpm build   # builds every package
pnpm test    # tests every package
```

To work on a single package: `pnpm --filter @arc76/algorand test` (or `build`). Note:
`@arc76/core` must be built (`pnpm --filter @arc76/core build`) before other packages can resolve
its types/dist during their own build, since workspace packages link to each other's `dist/`
output rather than their TypeScript source.

## Migration guide (from the old `arc76` package)

This workspace replaces the single `arc76` npm package with per-chain packages, mirroring the
`dotnet/` package split. `arc76` is not maintained going forward — no compatibility shim is
provided.

```diff
- npm i arc76 --save
+ npm i @arc76/algorand --save
```

```diff
- import arc76 from "arc76";
- const account = await arc76.generateAlgorandAccount(password, email, slot);
+ import { getAccount, getEmailAccount } from "@arc76/algorand";
+ const account = await getAccount(password, slot);       // no email
+ const account = await getEmailAccount(email, password, slot); // with email
```

Key differences from the old single-function API:
- `generateAlgorandAccount(password, email, slot)` (one function, optional email) is now two
  functions, `getAccount(password, slot)` and `getEmailAccount(email, password, slot)` —
  mirroring `dotnet/`'s `ARC76Account.Algorand.ARC76.GetAccount`/`GetEmailAccount` exactly.
  Note the parameter order: email comes first in `getEmailAccount`, before password.
- The derivation algorithm itself is unchanged — the same password/email/slot produces the
  same account it always did.

## AI-assisted development

If you're using an AI coding assistant to write code against these packages, see
[AI_INTEGRATION.md](AI_INTEGRATION.md) for a concise, LLM-friendly reference of the current API
surface, common patterns, and pitfalls to avoid.
