"""Deterministic ARC76 account derivation for Bitcoin (native SegWit / bech32 addresses).

https://github.com/algorandfoundation/ARCs/blob/main/ARCs/arc-0076.md
"""

from __future__ import annotations

import hashlib
from dataclasses import dataclass

import base58
import bech32
import coincurve
from Crypto.Hash import RIPEMD160

from arc76.core import derive_seed, derive_seed_with_email

__all__ = ["BitcoinAccount", "get_account", "get_email_account"]


def _hash160(data: bytes) -> bytes:
    # RIPEMD-160 is intentionally taken from pycryptodome rather than hashlib: hashlib's
    # "ripemd160" is only available when the underlying OpenSSL build still ships its legacy
    # provider (OpenSSL 3.0+ disables it by default on many distros), so relying on it would
    # make address derivation silently platform-dependent. pycryptodome ships its own
    # implementation and needs no OpenSSL legacy provider.
    return RIPEMD160.new(hashlib.sha256(data).digest()).digest()


@dataclass(frozen=True)
class BitcoinAccount:
    """private_key: the raw 32-byte secp256k1 private key (identical to the ARC76 seed).
    public_key: the 33-byte compressed secp256k1 public key.
    """

    private_key: bytes
    public_key: bytes

    def wif(self) -> str:
        """WIF-encoded private key (compressed-public-key convention)."""
        payload = b"\x80" + self.private_key + b"\x01"
        return base58.b58encode_check(payload).decode()

    def address(self, hrp: str = "bc") -> str:
        """Native SegWit (bech32, P2WPKH) address, e.g. 'bc1...'. Pass hrp='tb' for testnet."""
        program = _hash160(self.public_key)
        address = bech32.encode(hrp, 0, program)
        if address is None:
            raise ValueError("failed to derive address")
        return address


def _from_seed(seed: bytes) -> BitcoinAccount:
    public_key = coincurve.PrivateKey(seed).public_key.format(compressed=True)
    return BitcoinAccount(private_key=seed, public_key=public_key)


def get_account(password: str, slot: int = 0) -> BitcoinAccount:
    """Derives a deterministic Bitcoin key from a password."""
    return _from_seed(derive_seed(password, slot))


def get_email_account(email: str, password: str, slot: int = 0) -> BitcoinAccount:
    """Derives a deterministic Bitcoin key from an email and password."""
    return _from_seed(derive_seed_with_email(email, password, slot))
