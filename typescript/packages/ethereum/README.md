# arc76-ethereum

Deterministic [ARC76](https://github.com/algorandfoundation/ARCs/blob/main/ARCs/arc-0076.md)
account derivation for **Ethereum and other EVM-compatible chains**.

## Usage

```
npm i arc76-ethereum --save
```

```ts
import { getAccount, getEmailAccount } from "arc76-ethereum";

const account = await getAccount(password); // slot 0, ethers.Wallet
console.log(account.address, account.privateKey);

const emailAccount = await getEmailAccount(email, password);
```

Both functions return an `ethers.Wallet` (`ethers` v6) — use `.address` and `.privateKey`.

Please use a password of at least 50 characters for real accounts — the account's security is
entirely the password's security.
