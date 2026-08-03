# ARC76 NPM LIBRARY

> Part of the [ARC76 Account monorepo](../README.md), which also has a
> [.NET implementation](../dotnet/README.md) (multiple chains: Algorand, Ethereum/EVM, Bitcoin,
> Bitcoin Cash, Solana). This package currently supports **Algorand only**.

This is npm library for password generated ED25519 accounts and used mainly by the algorand community.

[ARC76 Specs](https://github.com/algorandfoundation/ARCs/blob/main/ARCs/arc-0076.md)

## Usage

Install npm package

```
npm i arc76 --save
```

Import package

```
import arc76 from "arc76"
```

### Password generated account

```
const account: algosdk.Account = arc76.generateAlgorandAccount("password")
```

### Password generated account for slot 1

```
const account: algosdk.Account = arc76.generateAlgorandAccount("password","",1)
```

### Email password generated account

```
const account: algosdk.Account = arc76.generateAlgorandAccount("12345678901234567890123456789012345678901234567890","email@example.com")
expect(algosdk.encodeAddress(account.addr.publicKey)).toBe("5AHWQJ5D52K4GRW4JWQ5GMR53F7PDSJEGT4PXVFSBQYE7VXDVG3WSPWSBM");
```

### Email password generated account for slot 1

```
const account: algosdk.Account = arc76.generateAlgorandAccount("password","email@example.com",1)
```
