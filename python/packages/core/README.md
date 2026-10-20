# arc76-core

<p align="center"><img src="../../../assets/arc76-logo-512.png" alt="ARC76 Account" width="140"></p>

Shared [ARC76](https://github.com/algorandfoundation/ARCs/blob/main/ARCs/arc-0076.md) seed
derivation (PBKDF2-HMAC-SHA256, 999,999 iterations, 32-byte output) used internally by the
per-chain `arc76-*` packages (`arc76-algorand`, `arc76-ethereum`, `arc76-bitcoin`,
`arc76-bitcoincash`, `arc76-solana`).

Most consumers should install a chain-specific package instead of this one directly — this
package is only useful if you're implementing ARC76 support for a new chain yourself.

Zero third-party dependencies: PBKDF2 is computed with the standard library's
[`hashlib.pbkdf2_hmac`](https://docs.python.org/3/library/hashlib.html#hashlib.pbkdf2_hmac),
which delegates to OpenSSL's `PKCS5_PBKDF2_HMAC` — the same audited implementation the
`cryptography` package itself relies on — falling back to a pure-Python implementation only if
OpenSSL is unavailable. There is no bespoke PBKDF2 code in this package.

## Usage

```
pip install arc76-core
```

```python
from arc76.core import derive_seed, derive_seed_with_email

seed = derive_seed(password)          # bytes, 32 bytes, slot 0
seed1 = derive_seed(password, 1)      # slot 1

email_seed = derive_seed_with_email(email, password)  # slot 0
```

Password must be at least 16 characters (raises `ValueError` otherwise) — production passwords
should be much longer (≥ 50 characters recommended).

`derive_seed`/`derive_seed_with_email` are synchronous, unlike the TypeScript package's `async`
functions — Python's `hashlib.pbkdf2_hmac` is a synchronous, CPU-bound OpenSSL call with no
natural async form (the TypeScript package is `async` only because browsers' WebCrypto
`subtle.deriveBits` is async-only).

> ⚠️ **Same-curve chains share the literal same key material.** From the same
> password/email/slot, `arc76-bitcoin`/`arc76-bitcoincash`/`arc76-ethereum` derive the
> identical secp256k1 private key, and `arc76-algorand`/`arc76-solana` derive the identical
> Ed25519 keypair — leaking one chain's key leaks every other chain sharing its curve. Use a
> different password (a different `slot` does NOT isolate: slots are public) per chain if you need isolation.
