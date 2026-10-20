# arc76-bitcoincash

<p align="center"><img src="../../../assets/arc76-logo-512.png" alt="ARC76 Account" width="140"></p>

Deterministic [ARC76](https://github.com/algorandfoundation/ARCs/blob/main/ARCs/arc-0076.md)
account derivation for **Bitcoin Cash** (CashAddr addresses).

## Usage

```
pip install arc76-bitcoincash
```

```python
from arc76.bitcoincash import get_account, get_email_account

account = get_account(password)  # slot 0
print(account.address())         # CashAddr address, e.g. "bitcoincash:q..."
print(account.wif())             # WIF-encoded private key (same encoding Bitcoin uses)

email_account = get_email_account(email, password)
```

Both functions return a `BitcoinCashAccount(private_key, public_key)` — Bitcoin Cash uses the
same secp256k1 keys as Bitcoin, only the address format differs:

- `private_key` — the raw 32-byte secp256k1 private key (identical to the ARC76 seed).
- `public_key` — the 33-byte compressed secp256k1 public key.
- `.address(prefix="bitcoincash")` — the CashAddr (P2PKH) address.
- `.wif()` — the WIF-encoded private key (the same encoding `arc76-bitcoin` uses).

Elliptic-curve operations use [`coincurve`](https://pypi.org/project/coincurve/) (Python
bindings for [`libsecp256k1`](https://github.com/bitcoin-core/secp256k1), the same library
Bitcoin Core uses). CashAddr encoding is implemented internally against the
[CashAddr spec](https://github.com/bitcoincashorg/bitcoincash.org/blob/master/spec/cashaddr.md)
rather than via a third-party dependency, since no actively-maintained CashAddr package exists
on PyPI; it's covered by known-answer tests shared with the `dotnet`/`typescript` packages.

Please use a password of at least 50 characters for real accounts — the account's security is
entirely the password's security.

> ⚠️ **Same-curve chains share the literal same private key.** `arc76-bitcoin`,
> `arc76-bitcoincash`, and `arc76-ethereum` derive the **identical secp256k1 private key** from
> the same password/email/slot — leaking the key on one of those chains leaks it on all of them.
> Use a different password (a different `slot` does NOT isolate: slots are public) per chain if you need isolation.
