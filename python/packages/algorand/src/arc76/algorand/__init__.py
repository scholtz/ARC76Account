"""Deterministic ARC76 account derivation for Algorand (AVM).

https://github.com/algorandfoundation/ARCs/blob/main/ARCs/arc-0076.md
"""

from __future__ import annotations

import base64
from dataclasses import dataclass, field

import nacl.signing
from algosdk import account as algosdk_account
from algosdk import mnemonic as algosdk_mnemonic

from arc76.core import derive_seed, derive_seed_with_email

__all__ = [
    "AlgorandAccount",
    "AlgorandFalconAccount",
    "get_account",
    "get_email_account",
    "get_falcon_account",
    "get_email_falcon_account",
]


@dataclass(frozen=True)
class AlgorandAccount:
    """address: base32 Algorand address (checksummed).
    private_key: base64-encoded 64-byte expanded Ed25519 secret key, in py-algorand-sdk's
        standard "private_key" convention (the same string algosdk.account/mnemonic accept).
    mnemonic: the standard 25-word Algorand account mnemonic, importable into any Algorand
        wallet (Pera, Defly, ...).
    """

    address: str
    private_key: str
    mnemonic: str


def _from_seed(seed: bytes) -> AlgorandAccount:
    # The ARC76 seed *is* the Ed25519 signing seed - expanding it into a full keypair here
    # mirrors what algosdk's own mnemonicToSecretKey (TS) / Account(seed) (.NET) do internally.
    signing_key = nacl.signing.SigningKey(seed)
    verify_key = signing_key.verify_key
    private_key_b64 = base64.b64encode(signing_key.encode() + bytes(verify_key)).decode()
    return AlgorandAccount(
        address=algosdk_account.address_from_private_key(private_key_b64),
        private_key=private_key_b64,
        mnemonic=algosdk_mnemonic.from_private_key(private_key_b64),
    )


def get_account(password: str, slot: int = 0) -> AlgorandAccount:
    """Derives a deterministic Algorand account from a password."""
    return _from_seed(derive_seed(password, slot))


def get_email_account(email: str, password: str, slot: int = 0) -> AlgorandAccount:
    """Derives a deterministic Algorand account from an email and password."""
    return _from_seed(derive_seed_with_email(email, password, slot))


@dataclass(frozen=True)
class AlgorandFalconAccount:
    """A post-quantum Falcon-1024 Algorand account.

    address: base32 Algorand address (checksummed).
    public_key: raw 1793-byte Falcon-1024 public key.
    private_key: raw 2305-byte Falcon-1024 private key, as accepted by
        ``algorand_falcon.falcon1024.Signer``.
    salt: the canonical address salt (0-255) - needed together with ``public_key`` to build the
        post-quantum signature envelope for transactions.
    """

    address: str
    public_key: bytes
    private_key: bytes = field(repr=False)  # keep the secret out of repr()/tracebacks
    salt: int


def _falcon_from_seed(seed: bytes) -> AlgorandFalconAccount:
    # Imported lazily: the Falcon dependencies are the optional `arc76-algorand[falcon]` extra.
    try:
        from algorand_falcon.falcon1024 import Signer
    except ImportError as e:  # pragma: no cover - exercised only without the extra installed
        raise ImportError(
            "Falcon support requires the optional dependencies: pip install 'arc76-algorand[falcon]'"
        ) from e
    from algosdk import constants, encoding

    # Same derivation as Algorand's standard Falcon accounts (algosdk.mnemonic.to_pq_seed, and
    # .NET's FalconAccount.FromEntropy): keygen seed = SHA-512/256("PQK" + scheme + seed).
    scheme = constants.falcon_1024_scheme
    keygen_seed = encoding.checksum(constants.pq_seed_prefix + scheme + seed)
    signer = Signer.generate(keygen_seed)
    address, salt = encoding.address_from_pq_key(scheme, signer.public_key)
    return AlgorandFalconAccount(
        address=address,
        public_key=signer.public_key,
        private_key=signer.private_key,
        salt=salt,
    )


def get_falcon_account(password: str, slot: int = 0) -> AlgorandFalconAccount:
    """Derives a deterministic Algorand post-quantum (Falcon-1024) account from a password.

    Uses the same 32-byte ARC76 seed as `get_account`, so it is a different account from the
    Ed25519 one for the same inputs. Requires the ``falcon`` extra.
    """
    return _falcon_from_seed(derive_seed(password, slot))


def get_email_falcon_account(email: str, password: str, slot: int = 0) -> AlgorandFalconAccount:
    """Derives a deterministic Algorand post-quantum (Falcon-1024) account from an email and password."""
    return _falcon_from_seed(derive_seed_with_email(email, password, slot))
