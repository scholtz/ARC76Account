# arc76-algorand

<p align="center"><img src="../../../assets/arc76-logo-512.png" alt="ARC76 Account" width="140"></p>

Deterministic [ARC76](https://github.com/algorandfoundation/ARCs/blob/main/ARCs/arc-0076.md)
account derivation for **Algorand**.

## Usage

```
pip install arc76-algorand
```

```python
from arc76.algorand import get_account, get_email_account

account = get_account(password)          # slot 0
account1 = get_account(password, 1)      # slot 1

email_account = get_email_account(email, password)  # slot 0
```

Both functions return an `AlgorandAccount(address, private_key, mnemonic)`:

- `address` — the base32 Algorand address.
- `private_key` — base64-encoded 64-byte expanded Ed25519 secret key, in
  [`py-algorand-sdk`](https://pypi.org/project/py-algorand-sdk/)'s standard convention (accepted
  directly by `algosdk.account`/`algosdk.mnemonic`, and by `algosdk.transaction` signing calls).
- `mnemonic` — the standard 25-word Algorand account mnemonic, importable into any Algorand
  wallet (Pera, Defly, ...).

The Ed25519 keypair itself is expanded via [PyNaCl](https://pypi.org/project/PyNaCl/)
(the Python binding for libsodium), the same audited primitive Algorand's own `py-algorand-sdk`
uses internally.

### Post-quantum (Falcon-1024) account

```
pip install "arc76-algorand[falcon]"
```

```python
from arc76.algorand import get_falcon_account, get_email_falcon_account

falcon = get_falcon_account(password)  # slot 0 - an AlgorandFalconAccount
falcon = get_email_falcon_account(email, password, slot=1)
```

Returns `AlgorandFalconAccount(address, public_key, private_key, salt)`. The same 32-byte ARC76
seed is used as for the Ed25519 account, so leaking the password compromises both - use a
different `slot` to isolate them. Addresses match the .NET and TypeScript packages.

Please use a password of at least 50 characters for real accounts — the account's security is
entirely the password's security.

> ⚠️ **Same-curve chains share the literal same keypair.** `arc76-algorand` and `arc76-solana`
> derive the **identical Ed25519 keypair** from the same password/email/slot — leaking the key
> on one of those chains leaks it on the other. Use a different password (or `slot`) per chain
> if you need isolation.
