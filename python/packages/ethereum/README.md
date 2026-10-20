# arc76-ethereum

<p align="center"><img src="../../../assets/arc76-logo-512.png" alt="ARC76 Account" width="140"></p>

Deterministic [ARC76](https://github.com/algorandfoundation/ARCs/blob/main/ARCs/arc-0076.md)
account derivation for **Ethereum and other EVM-compatible chains**.

## Usage

```
pip install arc76-ethereum
```

```python
from arc76.ethereum import get_account, get_email_account

account = get_account(password)  # slot 0, eth_account.signers.local.LocalAccount
print(account.address, account.key.hex())

email_account = get_email_account(email, password)
```

Both functions return an
[`eth_account.signers.local.LocalAccount`](https://eth-account.readthedocs.io/) (from
[`eth-account`](https://pypi.org/project/eth-account/), the Ethereum Foundation-maintained
signing library also used by Web3.py) — use `.address` and `.key`.

Please use a password of at least 50 characters for real accounts — the account's security is
entirely the password's security.

> ⚠️ **Same-curve chains share the literal same private key.** `arc76-bitcoin`,
> `arc76-bitcoincash`, and `arc76-ethereum` derive the **identical secp256k1 private key** from
> the same password/email/slot — leaking the key on one of those chains leaks it on all of them.
> Use a different password (a different `slot` does NOT isolate: slots are public) per chain if you need isolation.
