# arc76-solana

<p align="center"><img src="../../../assets/arc76-logo-512.png" alt="ARC76 Account" width="140"></p>

Deterministic [ARC76](https://github.com/algorandfoundation/ARCs/blob/main/ARCs/arc-0076.md)
account derivation for **Solana**.

## Usage

```
pip install arc76-solana
```

```python
import base58
from arc76.solana import get_account, get_email_account

account = get_account(password)  # slot 0
print(base58.b58encode(account.public_key).decode())  # address
print(base58.b58encode(account.secret_key).decode())  # private key (Phantom export format)

email_account = get_email_account(email, password)
```

Both functions return a `SolanaAccount(public_key, secret_key)`. The 32-byte ARC76 seed is
expanded directly into an Ed25519 keypair (no BIP32/BIP44 hierarchical derivation, no mnemonic):

- `public_key` — 32-byte Ed25519 public key; base58-encode it for the Solana address.
- `secret_key` — 64-byte expanded Ed25519 secret key (`seed || public_key`); base58-encode it
  for the private key, the same format wallets like Phantom use for private-key export.

The Ed25519 keypair is expanded via [PyNaCl](https://pypi.org/project/PyNaCl/) (the Python
binding for libsodium), matching what `tweetnacl` (TypeScript) and `Chaos.NaCl` (.NET) do.
Deliberately does **not** depend on `solana-py`/`solders` — only `Keypair`-equivalent
functionality (no RPC) is needed here.

Please use a password of at least 50 characters for real accounts — the account's security is
entirely the password's security.

> ⚠️ **Same-curve chains share the literal same keypair.** `arc76-algorand` and `arc76-solana`
> derive the **identical Ed25519 keypair** from the same password/email/slot — leaking the key
> on one of those chains leaks it on the other. Use a different password (or `slot`) per chain
> if you need isolation.
