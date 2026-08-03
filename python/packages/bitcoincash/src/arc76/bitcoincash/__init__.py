"""Deterministic ARC76 account derivation for Bitcoin Cash (CashAddr addresses).

https://github.com/algorandfoundation/ARCs/blob/main/ARCs/arc-0076.md
"""

from __future__ import annotations

import hashlib
from dataclasses import dataclass

import base58
import coincurve
from Crypto.Hash import RIPEMD160

from arc76.core import derive_seed, derive_seed_with_email

from ._cashaddr import encode_p2pkh

__all__ = ["BitcoinCashAccount", "get_account", "get_email_account"]


def _hash160(data: bytes) -> bytes:
    # See arc76.bitcoin._hash160 for why pycryptodome's RIPEMD160 is used instead of hashlib's.
    return RIPEMD160.new(hashlib.sha256(data).digest()).digest()


@dataclass(frozen=True)
class BitcoinCashAccount:
    """Bitcoin Cash uses the same secp256k1 keys as Bitcoin - only the address format differs.

    private_key: the raw 32-byte secp256k1 private key (identical to the ARC76 seed).
    public_key: the 33-byte compressed secp256k1 public key.
    """

    private_key: bytes
    public_key: bytes

    def wif(self) -> str:
        """WIF-encoded private key - the same encoding Bitcoin uses (compressed convention)."""
        payload = b"\x80" + self.private_key + b"\x01"
        return base58.b58encode_check(payload).decode()

    def address(self, prefix: str = "bitcoincash") -> str:
        """CashAddr (P2PKH) address, e.g. 'bitcoincash:q...'."""
        return encode_p2pkh(prefix, _hash160(self.public_key))


def _from_seed(seed: bytes) -> BitcoinCashAccount:
    public_key = coincurve.PrivateKey(seed).public_key.format(compressed=True)
    return BitcoinCashAccount(private_key=seed, public_key=public_key)


def get_account(password: str, slot: int = 0) -> BitcoinCashAccount:
    """Derives a deterministic Bitcoin Cash key from a password."""
    return _from_seed(derive_seed(password, slot))


def get_email_account(email: str, password: str, slot: int = 0) -> BitcoinCashAccount:
    """Derives a deterministic Bitcoin Cash key from an email and password."""
    return _from_seed(derive_seed_with_email(email, password, slot))
