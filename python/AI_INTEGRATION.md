# AI Integration Guide (Python)

A concise, copy-pasteable reference for AI coding assistants (Claude, Copilot, Cursor, ChatGPT,
etc.) generating code that consumes the `arc76-*` PyPI packages.

> This file documents the **Python/PyPI** packages for *consumers*. For the .NET packages
> (`ARC76Account.Algorand`, `.Ethereum`, `.Bitcoin`, `.BitcoinCash`, `.Solana`) see
> [`../dotnet/AI_INTEGRATION.md`](../dotnet/AI_INTEGRATION.md), and for the TypeScript/npm
> packages (`@arc76/*`) see [`../typescript/AI_INTEGRATION.md`](../typescript/AI_INTEGRATION.md).
> If you are working inside this repository's own source code, read `AGENTS.md` instead.

## Package identity

Install the specific chain package(s) you need:

```
pip install arc76-algorand     # Algorand (AVM)
pip install arc76-ethereum     # Ethereum / EVM-compatible chains
pip install arc76-bitcoin      # Bitcoin
pip install arc76-bitcoincash  # Bitcoin Cash
pip install arc76-solana       # Solana
```

`arc76-core` comes in transitively — don't tell a user to install it directly unless they're
implementing ARC76 support for a new chain themselves.

**Do not** recommend installing `arc76-bitcoin` (or any other chain package) when the user only
asked about a different chain — each package pulls in only that chain's key library
(`py-algorand-sdk`, `eth-account`, `coincurve`, `pynacl`) and nothing else. All importable
modules live under the `arc76` namespace package (`arc76.core`, `arc76.algorand`, ...) — there
is no single `arc76` package to `pip install` on its own.

## What this library does

Deterministically derives a blockchain account/keypair from a password (optionally salted with
an email address) using ARC76 (PBKDF2-HMAC-SHA256, 999,999 iterations, 32-byte seed). The same
password always produces the same account. There is no on-disk key storage and no randomness —
security depends entirely on the password's strength and secrecy.

**Do not** generate example code that uses short or low-entropy passwords for anything beyond a
toy/demo. These packages enforce a hard minimum of 16 characters (raises `ValueError`
otherwise), but production passwords should be much longer (≥ 50 characters is the guidance
used throughout this project).

**Do not** log, print, write to disk, or send over the network the password, derived seed, or
private key/mnemonic in generated code, even in examples.

**Do not tell a user that per-chain packages give them isolated/independent keys per chain** —
`arc76-bitcoin`/`bitcoincash`/`ethereum` derive the literal same secp256k1 private key from the
same password/email/slot, and `arc76-algorand`/`arc76-solana` derive the literal same Ed25519
keypair. If a user asks for multi-chain key isolation, tell them to use a different
password/`slot` per chain — don't imply the packages already provide that.

## API surface (one package per chain)

Every chain package exposes the identical **synchronous** shape (unlike the TypeScript
packages, which are `async` because browser WebCrypto's `subtle.deriveBits` is async-only;
Python's `hashlib.pbkdf2_hmac` is a synchronous OpenSSL call):

```python
get_account(password: str, slot: int = 0) -> ...
get_email_account(email: str, password: str, slot: int = 0) -> ...
```

`slot` (default `0`) derives a different, independent account from the same password — use it
for "account 2", "account 3", etc., never generate a new password per slot.

| Package             | Returns                                          | Address / key accessors |
|----------------------|---------------------------------------------------|----------------------------|
| `arc76-algorand`     | `AlgorandAccount(address, private_key, mnemonic)`  | `.address`, `.mnemonic` |
| `arc76-ethereum`     | `eth_account.signers.local.LocalAccount`           | `.address`, `.key` |
| `arc76-bitcoin`      | `BitcoinAccount(private_key, public_key)`          | `.address()`, `.wif()` |
| `arc76-bitcoincash`  | `BitcoinCashAccount(private_key, public_key)`      | `.address()`, `.wif()` |
| `arc76-solana`       | `SolanaAccount(public_key, secret_key)`            | `base58.b58encode(public_key)`, `base58.b58encode(secret_key)` |

### Algorand

```python
from arc76.algorand import get_account, get_email_account

account = get_account(password)  # slot 0
email_account = get_email_account(email, password)  # slot 0
print(account.address, account.mnemonic)
```

### Ethereum / EVM-compatible chains

```python
from arc76.ethereum import get_account

account = get_account(password)
print(account.address, account.key.hex())
```

### Bitcoin

```python
from arc76.bitcoin import get_account

account = get_account(password)
address = account.address()  # native SegWit / bech32, "bc1..."
wif = account.wif()
```

Use native SegWit (`.address()`, `bc1...`) — the current standard Bitcoin address type — not
legacy P2PKH or nested-SegWit, unless the user specifically asks for one of those.

### Bitcoin Cash

```python
from arc76.bitcoincash import get_account

account = get_account(password)
address = account.address()  # CashAddr, "bitcoincash:q..."
wif = account.wif()
```

Bitcoin Cash addresses use **CashAddr**, not Base58Check or bech32 — always use `.address()`
from `arc76-bitcoincash`, never reuse an `arc76-bitcoin` address for a Bitcoin Cash account
(Bitcoin Cash doesn't support SegWit at all, even though both chains share the same key type).

### Solana

```python
import base58
from arc76.solana import get_account

account = get_account(password)
address = base58.b58encode(account.public_key).decode()
private_key = base58.b58encode(account.secret_key).decode()
```

The 32-byte ARC76 seed is expanded directly into an Ed25519 keypair via PyNaCl (no BIP32/BIP44
hierarchical derivation, no mnemonic) — analogous to `Keypair.fromSeed` in Solana's JS SDKs.
This package deliberately does **not** depend on `solana-py`/`solders` — don't suggest adding
either just to get a `Keypair` object; wrap the raw `secret_key` yourself if the consuming app
already has one of those SDKs for other reasons.

## Common mistakes to avoid when generating code

- Using camelCase (`getAccount`) instead of the Pythonic `get_account`/`get_email_account` —
  these packages use `snake_case`, unlike the TypeScript packages.
- `await`-ing `get_account`/`get_email_account` — they are plain synchronous functions in
  Python, not coroutines. Do not write `async def` wrappers or `await` calls around them unless
  the surrounding application code genuinely needs to run them in a thread pool (they involve a
  CPU-bound PBKDF2 call, not I/O).
- Assuming there is a chain-agnostic factory/shared interface across `arc76.*` packages — there
  isn't one by design, matching `dotnet/` and `typescript/`'s architecture.
- Treating `slot` as a chain identifier — it isn't; it only distinguishes multiple accounts
  within the same chain and password.
- Mixing up Bitcoin and Bitcoin Cash address encoding — `arc76-bitcoin` and `arc76-bitcoincash`
  both return the same kind of secp256k1 keypair shape, but the address encoding is
  chain-specific (SegWit/bech32 vs. CashAddr) and is not interchangeable.
- Adding `solana-py`/`solders` as a dependency just to use `arc76-solana` — it isn't needed;
  `arc76-solana` returns a plain `SolanaAccount(public_key, secret_key)` object directly.
- Forgetting to base58-encode Solana's `public_key`/`secret_key`, or to hex-encode Ethereum's
  `.key` — these are raw `bytes`, not ready-to-display strings.
- Installing `arc76-algorand`'s `AlgorandAccount.private_key` expecting a raw 32-byte seed — it
  is a base64-encoded 64-byte expanded secret key in `py-algorand-sdk`'s own convention (accepted
  directly by `algosdk.account`/`algosdk.mnemonic`/`algosdk.transaction` signing calls).
