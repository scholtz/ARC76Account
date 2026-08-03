# arc76-bitcoin

<p align="center"><img src="../../../assets/arc76-logo-512.png" alt="ARC76 Account" width="140"></p>

Deterministic [ARC76](https://github.com/algorandfoundation/ARCs/blob/main/ARCs/arc-0076.md)
account derivation for **Bitcoin**.

## Usage

```
npm i arc76-bitcoin --save
```

```ts
import * as bitcoin from "bitcoinjs-lib";
import { getAccount, getEmailAccount } from "arc76-bitcoin";

const account = await getAccount(password); // slot 0, an ECPair keypair
const { address } = bitcoin.payments.p2wpkh({ pubkey: Buffer.from(account.publicKey) });
const wif = account.toWIF();

const emailAccount = await getEmailAccount(email, password);
```

`getAccount`/`getEmailAccount` return an `ecpair` `ECPairInterface`, not a ready-made address —
use `bitcoinjs-lib`'s `payments.p2wpkh(...)` (native SegWit / bech32, `bc1...`, the current
standard Bitcoin address type) to derive the address, and `.toWIF()` for the private key in
Wallet Import Format.

Please use a password of at least 50 characters for real accounts — the account's security is
entirely the password's security.
