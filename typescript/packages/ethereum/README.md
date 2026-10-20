# @arc76/ethereum

<p align="center"><img src="../../../assets/arc76-logo-512.png" alt="ARC76 Account" width="140"></p>

Deterministic [ARC76](https://github.com/algorandfoundation/ARCs/blob/main/ARCs/arc-0076.md)
account derivation for **Ethereum and other EVM-compatible chains**.

## Usage

```
npm i @arc76/ethereum --save
```

```ts
import { getAccount, getEmailAccount } from "@arc76/ethereum";

const account = await getAccount(password); // slot 0, ethers.Wallet
console.log(account.address, account.privateKey);

const emailAccount = await getEmailAccount(email, password);
```

Both functions return an `ethers.Wallet` (`ethers` v6) — use `.address` and `.privateKey`.

Please use a password of at least 50 characters for real accounts — the account's security is
entirely the password's security.

> ⚠️ **Same-curve chains share the literal same private key.** `@arc76/bitcoin`,
> `@arc76/bitcoincash`, and `@arc76/ethereum` derive the **identical secp256k1 private key**
> from the same password/email/slot — leaking the key on one of those chains leaks it on all of
> them. Use a different password (a different `slot` does NOT isolate: slots are public) per chain if you need isolation.
