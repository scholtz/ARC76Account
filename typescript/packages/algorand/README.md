# arc76-algorand

<p align="center"><img src="../../../assets/arc76-logo-512.png" alt="ARC76 Account" width="140"></p>

Deterministic [ARC76](https://github.com/algorandfoundation/ARCs/blob/main/ARCs/arc-0076.md)
account derivation for **Algorand**.

> Renamed from the `arc76` package (v1.x). See the
> [migration guide](../../README.md#migration-guide-arc76-v1x--arc76-algorand) if you're
> upgrading.

## Usage

```
npm i arc76-algorand --save
```

```ts
import { getAccount, getEmailAccount } from "arc76-algorand";

const account = await getAccount(password); // slot 0
const account1 = await getAccount(password, 1); // slot 1

const emailAccount = await getEmailAccount(email, password); // slot 0
```

Both functions return an `algosdk.Account` (`{ addr, sk }`).

Please use a password of at least 50 characters for real accounts — the account's security is
entirely the password's security.
