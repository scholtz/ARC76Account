"""Shared ARC76 (PBKDF2-HMAC-SHA256) seed derivation used by the per-chain arc76-* packages
(arc76-algorand, arc76-ethereum, arc76-bitcoin, arc76-bitcoincash, arc76-solana).

https://github.com/algorandfoundation/ARCs/blob/main/ARCs/arc-0076.md
"""

from __future__ import annotations

import hashlib

__all__ = ["derive_seed", "derive_seed_with_email"]

_ITERATIONS = 999_999
_MIN_PASSWORD_LENGTH = 16


def _assert_password(password: str) -> None:
    if len(password) < _MIN_PASSWORD_LENGTH:
        raise ValueError(f"Password must be at least {_MIN_PASSWORD_LENGTH} characters long")


def _assert_slot(slot: int) -> None:
    # MUST reject non-ints: Python has no static type enforcement, and the other two
    # languages' `slot` is a fixed-width int that cannot represent a float. A fractional
    # slot accepted here would derive an account no other ARC76Account implementation could
    # ever re-derive. `bool` is deliberately excluded even though it subclasses `int` in
    # Python, since True/False as a slot is almost certainly a caller bug, not intent. See
    # audits/2026-08-04-audit-report.md (Low finding) - do not relax this.
    if isinstance(slot, bool) or not isinstance(slot, int):
        raise TypeError(f"slot must be an int, got {slot!r}")


def _pbkdf2(input_str: str, salt: str) -> bytes:
    # hashlib.pbkdf2_hmac delegates to OpenSSL's PKCS5_PBKDF2_HMAC when available (the same
    # audited implementation the "cryptography" package itself relies on), falling back to a
    # pure-Python implementation only if OpenSSL is unavailable - no bespoke PBKDF2 code here.
    return hashlib.pbkdf2_hmac(
        "sha256", input_str.encode("utf-8"), salt.encode("utf-8"), _ITERATIONS, dklen=32
    )


def derive_seed(password: str, slot: int = 0) -> bytes:
    """Derives the 32-byte ARC76 seed from a password. Every chain-specific arc76-* package
    builds its account/keypair from this seed."""
    _assert_password(password)
    _assert_slot(slot)
    input_str = f"ARC-0076-{password}-{slot}-PBKDF2-999999"
    salt = f"ARC-0076-{slot}-PBKDF2-999999"
    return _pbkdf2(input_str, salt)


def derive_seed_with_email(email: str, password: str, slot: int = 0) -> bytes:
    """Derives the 32-byte ARC76 seed from an email and password."""
    _assert_password(password)
    _assert_slot(slot)
    input_str = f"ARC-0076-{email}-{password}-{slot}-PBKDF2-999999"
    salt = f"ARC-0076-{email}-{slot}-PBKDF2-999999"
    return _pbkdf2(input_str, salt)
