# @arc76/solana

<p align="center"><img src="../../../assets/arc76-logo-512.png" alt="ARC76 Account" width="140"></p>

Deterministic [ARC76](https://github.com/algorandfoundation/ARCs/blob/main/ARCs/arc-0076.md)
account derivation for **Solana**.

## Usage

```
npm i @arc76/solana --save
```

```ts
import bs58 from "bs58";
import { getAccount, getEmailAccount } from "@arc76/solana";

const account = await getAccount(password); // slot 0
const address = bs58.encode(account.publicKey);
const privateKey = bs58.encode(account.secretKey);

const emailAccount = await getEmailAccount(email, password);
```

`getAccount`/`getEmailAccount` return `{ publicKey: Uint8Array(32), secretKey: Uint8Array(64) }`
(an Ed25519 keypair, via `tweetnacl`'s `sign.keyPair.fromSeed` — no BIP32/BIP44 hierarchical
derivation, no mnemonic). Base58-encode `publicKey` for the address and `secretKey` for the
private key — the same format wallets like Phantom use for private-key export.

Note: this package intentionally does **not** depend on `@solana/web3.js` — that SDK's RPC
websocket dependency chain currently has an ESM/CommonJS incompatibility
(`rpc-websockets` → `uuid` v14, ESM-only) unrelated to key derivation. If your app already uses
`@solana/web3.js` for RPC calls, you can wrap this package's output in `new web3.Keypair({...})`
or `web3.Keypair.fromSecretKey(account.secretKey)` yourself.

Please use a password of at least 50 characters for real accounts — the account's security is
entirely the password's security.
