"""Deterministic ARC76 account derivation for Algorand (AVM).

https://github.com/algorandfoundation/ARCs/blob/main/ARCs/arc-0076.md
"""

from __future__ import annotations

import base64
from dataclasses import dataclass

import nacl.signing
from algosdk import account as algosdk_account
from algosdk import mnemonic as algosdk_mnemonic

from arc76.core import derive_seed, derive_seed_with_email

__all__ = ["AlgorandAccount", "get_account", "get_email_account"]


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
