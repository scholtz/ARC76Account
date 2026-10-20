# @arc76/algorand

<p align="center"><img src="../../../assets/arc76-logo-512.png" alt="ARC76 Account" width="140"></p>

Deterministic [ARC76](https://github.com/algorandfoundation/ARCs/blob/main/ARCs/arc-0076.md)
account derivation for **Algorand**.

> Renamed from the `arc76` package (v1.x). See the
> [migration guide](../../README.md#migration-guide-from-the-old-arc76-package) if you're
> upgrading.

## Usage

```
npm i @arc76/algorand --save
```

```ts
import { getAccount, getEmailAccount } from "@arc76/algorand";

const account = await getAccount(password); // slot 0
const account1 = await getAccount(password, 1); // slot 1

const emailAccount = await getEmailAccount(email, password); // slot 0
```

Both functions return an `algosdk.Account` (`{ addr, sk }`).

### Post-quantum (Falcon-1024) account

```ts
import { getFalconAccount, getEmailFalconAccount } from "@arc76/algorand";

const falcon = await getFalconAccount(password); // { address, publicKey, privateKey, salt }
const emailFalcon = await getEmailFalconAccount(email, password, 1);
```

The same 32-byte ARC76 seed is used as for the Ed25519 account, so leaking the password
compromises both. Slots are public and do not isolate them - only a different
password does. Addresses match the .NET and Python
packages. Keys work with [`falcon-1024`](https://www.npmjs.com/package/falcon-1024).

Please use a password of at least 50 characters for real accounts — the account's security is
entirely the password's security.

> ⚠️ **Same-curve chains share the literal same keypair.** `@arc76/algorand` and
> `@arc76/solana` derive the **identical Ed25519 keypair** from the same password/email/slot —
> leaking the key on one of those chains leaks it on the other. Use a different password (or
> `slot`) per chain if you need isolation.
