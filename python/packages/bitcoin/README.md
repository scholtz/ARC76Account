# arc76-bitcoin

<p align="center"><img src="../../../assets/arc76-logo-512.png" alt="ARC76 Account" width="140"></p>

Deterministic [ARC76](https://github.com/algorandfoundation/ARCs/blob/main/ARCs/arc-0076.md)
account derivation for **Bitcoin** (native SegWit / bech32 addresses).

## Usage

```
pip install arc76-bitcoin
```

```python
from arc76.bitcoin import get_account, get_email_account

account = get_account(password)  # slot 0
print(account.address())         # bech32 P2WPKH address, e.g. "bc1..."
print(account.wif())             # WIF-encoded private key

email_account = get_email_account(email, password)
```

Both functions return a `BitcoinAccount(private_key, public_key)`:

- `private_key` — the raw 32-byte secp256k1 private key (identical to the ARC76 seed).
- `public_key` — the 33-byte compressed secp256k1 public key.
- `.address(hrp="bc")` — the native SegWit (bech32, P2WPKH) address; pass `hrp="tb"` for testnet.
- `.wif()` — the WIF-encoded private key (compressed-public-key convention).

Elliptic-curve operations use [`coincurve`](https://pypi.org/project/coincurve/), Python
bindings for [`libsecp256k1`](https://github.com/bitcoin-core/secp256k1) — the same
constant-time, side-channel-hardened library Bitcoin Core itself uses for secp256k1. RIPEMD-160
(for the address hash) is taken from [`pycryptodome`](https://pypi.org/project/pycryptodome/)
rather than the standard library, since `hashlib`'s `ripemd160` depends on OpenSSL's legacy
provider being enabled (not guaranteed on OpenSSL 3.0+); `pycryptodome` ships its own
implementation so address derivation doesn't silently vary by platform.

Please use a password of at least 50 characters for real accounts — the account's security is
entirely the password's security.

> ⚠️ **Same-curve chains share the literal same private key.** `arc76-bitcoin`,
> `arc76-bitcoincash`, and `arc76-ethereum` derive the **identical secp256k1 private key** from
> the same password/email/slot — leaking the key on one of those chains leaks it on all of them.
> Use a different password (a different `slot` does NOT isolate: slots are public) per chain if you need isolation.
