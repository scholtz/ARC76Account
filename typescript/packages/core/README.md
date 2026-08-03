# @arc76/core

<p align="center"><img src="../../../assets/arc76-logo-512.png" alt="ARC76 Account" width="140"></p>

Shared [ARC76](https://github.com/algorandfoundation/ARCs/blob/main/ARCs/arc-0076.md) seed
derivation (PBKDF2-HMAC-SHA256, 999,999 iterations, 32-byte output) used internally by the
per-chain `@arc76/*` packages (`@arc76/algorand`, `@arc76/ethereum`, `@arc76/bitcoin`,
`@arc76/bitcoincash`, `@arc76/solana`).

Most consumers should install a chain-specific package instead of this one directly — this
package is only useful if you're implementing ARC76 support for a new chain yourself.

## Usage

```
npm i @arc76/core --save
```

```ts
import { deriveSeed, deriveSeedWithEmail } from "@arc76/core";

const seed = await deriveSeed(password); // Uint8Array(32), slot 0
const seed1 = await deriveSeed(password, 1); // slot 1

const emailSeed = await deriveSeedWithEmail(email, password); // slot 0
```

Password must be at least 16 characters (throws otherwise) — production passwords should be
much longer (≥ 50 characters recommended).
