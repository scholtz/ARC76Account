# AI Integration Guide (TypeScript)

A concise, copy-pasteable reference for AI coding assistants (Claude, Copilot, Cursor, ChatGPT,
etc.) generating code that consumes the `@arc76/*` npm packages.

> This file documents the **TypeScript/npm** packages for *consumers*. For the .NET packages
> (`ARC76Account.Algorand`, `.Ethereum`, `.Bitcoin`, `.BitcoinCash`, `.Solana`), see
> [`../dotnet/AI_INTEGRATION.md`](../dotnet/AI_INTEGRATION.md). If you are working inside this
> repository's own source code, read `AGENTS.md` instead.

## Package identity

There is **no single `arc76` package anymore** — that was the pre-split v1.x package (Algorand
only). Install the specific chain package(s) you need:

```
npm i @arc76/algorand --save     # Algorand (AVM)
npm i @arc76/ethereum --save     # Ethereum / EVM-compatible chains
npm i @arc76/bitcoin --save      # Bitcoin
npm i @arc76/bitcoincash --save  # Bitcoin Cash
npm i @arc76/solana --save       # Solana
```

`@arc76/core` comes in transitively — don't tell a user to install it directly unless they're
implementing ARC76 support for a new chain themselves.

**Do not** recommend installing `@arc76/bitcoin` (or any other chain package) when the user only
asked about a different chain — each package pulls in only that chain's key library
(`algosdk`, `ethers`, `bitcoinjs-lib`/`ecpair`, `cashaddrjs`, `tweetnacl`) and nothing else.

## What this library does

Deterministically derives a blockchain account/keypair from a password (optionally salted with
an email address) using ARC76 (PBKDF2-HMAC-SHA256, 999,999 iterations, 32-byte seed). The same
password always produces the same account. There is no on-disk key storage and no randomness —
security depends entirely on the password's strength and secrecy.

**Do not** generate example code that uses short or low-entropy passwords for anything beyond
a toy/demo. These packages enforce a hard minimum of 16 characters (throws otherwise), but
production passwords should be much longer (≥ 50 characters is the guidance used throughout
this project).

**Do not** log, print, write to disk, or send over the network the password, derived seed, or
private key/mnemonic in generated code, even in examples.

**Do not tell a user that per-chain packages give them isolated/independent keys per chain** —
`@arc76/bitcoin`/`bitcoincash`/`ethereum` derive the literal same secp256k1 private key from the
same password/email/slot, and `algorand`/`solana` derive the literal same Ed25519 keypair. If a
user asks for multi-chain key isolation, tell them to use a different password/`slot` per
chain — don't imply the packages already provide that.

## API surface (one package per chain)

Every chain package exposes the identical **async** shape:

```ts
getAccount(password: string, slot?: number): Promise<...>;
getEmailAccount(email: string, password: string, slot?: number): Promise<...>;
```

Always `await` both — they use the WebCrypto/Node `SubtleCrypto` API for PBKDF2 internally.
`slot` (default `0`) derives a different, independent account from the same password — use it
for "account 2", "account 3", etc., never generate a new password per slot.

| Package             | Returns                                   | Address / key accessors |
|----------------------|---------------------------------------------|----------------------------|
| `@arc76/algorand`     | `algosdk.Account`                          | `.addr` |
| `@arc76/ethereum`     | `ethers.Wallet`                            | `.address`, `.privateKey` |
| `@arc76/bitcoin`      | `{ publicKey, toWIF() }` (ecpair-like)      | `bitcoinjs-lib`'s `payments.p2wpkh({ pubkey })` for address, `.toWIF()` for private key |
| `@arc76/bitcoincash`  | `{ publicKey, toWIF() }` (ecpair-like)      | `cashaddrjs`'s `encode(...)` over `bitcoinjs-lib`'s `crypto.hash160(publicKey)` for address, `.toWIF()` for private key |
| `@arc76/solana`       | `{ publicKey, secretKey }` (Ed25519 keypair) | `bs58.encode(publicKey)` for address, `bs58.encode(secretKey)` for private key |

### Algorand

```ts
import { getAccount, getEmailAccount } from "@arc76/algorand";

const account = await getAccount(password); // slot 0
const emailAccount = await getEmailAccount(email, password); // slot 0
```

#### Algorand post-quantum (Falcon-1024)

```ts
import { getFalconAccount, getEmailFalconAccount } from "@arc76/algorand";

const falcon = await getFalconAccount(password); // { address, publicKey, privateKey, salt }
const emailFalcon = await getEmailFalconAccount(email, password, 1);
```

Warning for assistants: the Falcon account is derived from the *same* 32-byte ARC76 seed as the Ed25519 account for the same password/email/slot, so leaking the password compromises both. Slots do NOT isolate them (slots are public and derived from the same password; anyone with the password can derive every slot's Ed25519 and Falcon keys) - only a different password does. Never invent the Falcon API from the Ed25519 one.

### Ethereum / EVM-compatible chains

```ts
import { getAccount } from "@arc76/ethereum";

const account = await getAccount(password);
console.log(account.address, account.privateKey);
```

### Bitcoin

```ts
import * as bitcoin from "bitcoinjs-lib";
import { getAccount } from "@arc76/bitcoin";

const account = await getAccount(password);
const { address } = bitcoin.payments.p2wpkh({ pubkey: Buffer.from(account.publicKey) });
const wif = account.toWIF();
```

Use native SegWit (`payments.p2wpkh`, `bc1...`) — the current standard Bitcoin address type —
not legacy P2PKH or nested-SegWit, unless the user specifically asks for one of those.

### Bitcoin Cash

```ts
import * as bitcoin from "bitcoinjs-lib";
import { encode } from "cashaddrjs";
import { getAccount } from "@arc76/bitcoincash";

const account = await getAccount(password);
const hash160 = bitcoin.crypto.hash160(Buffer.from(account.publicKey));
const address = encode("bitcoincash", "P2PKH", hash160); // "bitcoincash:q..."
const wif = account.toWIF();
```

Bitcoin Cash addresses use **CashAddr**, not Base58Check or bech32 — always encode via
`cashaddrjs`, never reuse Bitcoin's `payments.p2wpkh` output for a Bitcoin Cash address (Bitcoin
Cash doesn't support SegWit at all).

### Solana

```ts
import bs58 from "bs58";
import { getAccount } from "@arc76/solana";

const account = await getAccount(password);
const address = bs58.encode(account.publicKey);
const privateKey = bs58.encode(account.secretKey);
```

The 32-byte ARC76 seed is expanded directly into an Ed25519 keypair via `tweetnacl` (no
BIP32/BIP44 hierarchical derivation, no mnemonic) — analogous to `Keypair.fromSeed` in
`@solana/web3.js`. This package deliberately does **not** depend on `@solana/web3.js` itself
(that SDK's RPC websocket dependency chain has a real ESM/CommonJS incompatibility unrelated to
key derivation) — don't suggest adding it as a dependency just to get a `Keypair` object; wrap
the raw `secretKey` yourself if the consuming app already has `@solana/web3.js` for other
reasons.

## Common mistakes to avoid when generating code

- Using the old `arc76` package or its `generateAlgorandAccount(password, email, slot)` single
  function — as of the per-chain split, use `@arc76/algorand`'s `getAccount(password, slot)` /
  `getEmailAccount(email, password, slot)` instead. Note the parameter order:
  `getEmailAccount(email, password, slot)`, email first.
- Forgetting `await` — every exported function here is `async`.
- Assuming there is a chain-agnostic factory/shared interface across `@arc76/*` packages —
  there isn't one by design, matching `dotnet/`'s architecture.
- Treating `slot` as a chain identifier — it isn't; it only distinguishes multiple accounts
  within the same chain and password.
- Mixing up Bitcoin and Bitcoin Cash address encoding — `@arc76/bitcoin` and
  `@arc76/bitcoincash` both return the same kind of secp256k1 keypair shape, but the address
  encoding is chain-specific (SegWit/bech32 vs. CashAddr) and is not interchangeable.
- Adding `@solana/web3.js` as a dependency just to use `@arc76/solana` — it isn't needed;
  `@arc76/solana` returns a plain `{ publicKey, secretKey }` object directly.
- Using `bs58.encode` on the wrong Solana field, or forgetting to encode at all — `publicKey`
  and `secretKey` are raw `Uint8Array`s, not ready-to-display strings.
