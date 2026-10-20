# ARC76 Account (Python)

<p align="center"><img src="../assets/arc76-logo-512.png" alt="ARC76 Account" width="180"></p>

> Part of the [ARC76 Account monorepo](../README.md), which also has a
> [.NET implementation](../dotnet/README.md) and a
> [TypeScript implementation](../typescript/README.md).

Python implementation of the
[Algorand ARC76 Account Standard](https://github.com/algorandfoundation/ARCs/blob/main/ARCs/arc-0076.md):
deterministic, password-derived accounts, with one PyPI package and one `arc76.*` module per
supported chain — mirroring the `dotnet/` and `typescript/` implementations' architecture
(`ARC76Account.Core`/`@arc76/core` + one package per chain).

Each package below is published to PyPI independently, and installed independently.

## Supported chains

| Chain              | Package               | Underlying key library      |
|--------------------|-------------------------|-------------------------------|
| Algorand (AVM)     | [`arc76-algorand`](packages/algorand/README.md)     | `py-algorand-sdk` + `pynacl`  |
| Ethereum / EVM     | [`arc76-ethereum`](packages/ethereum/README.md)     | `eth-account`                 |
| Bitcoin            | [`arc76-bitcoin`](packages/bitcoin/README.md)       | `coincurve` (libsecp256k1) + `bech32` |
| Bitcoin Cash       | [`arc76-bitcoincash`](packages/bitcoincash/README.md) | `coincurve` (libsecp256k1) + internal CashAddr encoder |
| Solana             | [`arc76-solana`](packages/solana/README.md)         | `pynacl` (libsodium)          |
| —                  | [`arc76-core`](packages/core/README.md) (shared seed derivation, not usually installed directly) | — (Python standard library only) |

Every chain package exposes the same shape:

```python
get_account(password: str, slot: int = 0) -> ...
get_email_account(email: str, password: str, slot: int = 0) -> ...
```

`slot` (default `0`) lets a single password derive multiple independent accounts on the same
chain. See each package's own README for its exact return type and how to get an
address/private key out of it (it varies by underlying library, same as in `dotnet/` and
`typescript/`).

Unlike the TypeScript packages (which are `async`, since browser WebCrypto's PBKDF2 API is
async-only), these functions are plain **synchronous** calls — Python's `hashlib.pbkdf2_hmac`
is a synchronous, CPU-bound OpenSSL call with no natural async form.

## Install

```
pip install arc76-algorand     # Algorand (AVM)
pip install arc76-ethereum     # Ethereum / EVM-compatible chains
pip install arc76-bitcoin      # Bitcoin
pip install arc76-bitcoincash  # Bitcoin Cash
pip install arc76-solana       # Solana
```

Install only the chain package(s) you need — `arc76-core` (the shared PBKDF2 seed derivation)
comes in transitively. All packages import from the shared `arc76` namespace package, e.g.
`from arc76.algorand import get_account`.

Please use a password of at least 50 characters for real accounts — the account's security is
entirely the password's security. (Each package enforces a 16-character minimum in code; treat
that as a hard floor, not a recommendation.)

> ⚠️ **`arc76-bitcoin`, `arc76-bitcoincash`, and `arc76-ethereum` derive the literal same
> secp256k1 private key from the same password/email/slot** (not merely related keys — the
> exact same 32 bytes). `arc76-algorand` and `arc76-solana` likewise derive the literal same
> Ed25519 keypair. Leaking one chain's key leaks every other chain sharing its curve, with zero
> extra attacker effort. See the [monorepo README](../README.md#shared-arc76-semantics) for the
> full explanation. Use a different password (a different `slot` does NOT isolate: slots are public) per chain if you need isolation.

## Cryptography choices

Every package favors well-audited, chain-canonical libraries over hand-rolled cryptography:

- **`arc76-core`** uses only the Python standard library — `hashlib.pbkdf2_hmac`, which
  delegates to OpenSSL's `PKCS5_PBKDF2_HMAC`. Zero third-party dependencies.
- **`arc76-bitcoin`/`arc76-bitcoincash`** use [`coincurve`](https://pypi.org/project/coincurve/),
  Python bindings for [`libsecp256k1`](https://github.com/bitcoin-core/secp256k1) — the same
  library Bitcoin Core itself uses — plus [`pycryptodome`](https://pypi.org/project/pycryptodome/)
  for RIPEMD-160 (deliberately not `hashlib`'s, since that depends on an OpenSSL legacy provider
  that isn't guaranteed to be enabled).
- **`arc76-algorand`/`arc76-solana`** use [`PyNaCl`](https://pypi.org/project/PyNaCl/), the
  Python binding for libsodium, for Ed25519 key expansion — the same primitive `tweetnacl`
  (TypeScript) and `Chaos.NaCl` (.NET) use, which is why known-answer values match bit-for-bit
  across all three languages.
- **`arc76-ethereum`** uses [`eth-account`](https://pypi.org/project/eth-account/), the
  Ethereum Foundation-maintained signing library also used by Web3.py.
- **`arc76-algorand`** uses [`py-algorand-sdk`](https://pypi.org/project/py-algorand-sdk/), the
  Algorand Foundation's official Python SDK, for address/mnemonic encoding.

## Cross-language compatibility

Every package here derives the exact same key material as the equivalent `dotnet/` and
`typescript/` package for the same password/email/slot (PBKDF2-HMAC-SHA256, 999,999 iterations,
ARC76's salt/input string format). Each package's tests reuse the literal known-answer values
from `dotnet/TestARC76/*.cs`, verified to also match `typescript/packages/*/src/index.test.ts`.

## Development (this repo)

Each package is independent — there's no repo-root workspace tool. From a package's own
directory:

```
pip install -e .
pip install pytest
python -m pytest
```

`arc76-core` is pulled from PyPI like any other dependency (not resolved from a local sibling
folder) — see [`AGENTS.md`](AGENTS.md) for how to develop `arc76-core` and a dependent package
together locally.

## AI-assisted development

If you're using an AI coding assistant to write code against these packages, see
[AI_INTEGRATION.md](AI_INTEGRATION.md) for a concise, LLM-friendly reference of the current API
surface, common patterns, and pitfalls to avoid.
