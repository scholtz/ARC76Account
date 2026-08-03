"""Deterministic ARC76 account derivation for Ethereum and other EVM-compatible chains.

https://github.com/algorandfoundation/ARCs/blob/main/ARCs/arc-0076.md
"""

from __future__ import annotations

from eth_account import Account
from eth_account.signers.local import LocalAccount

from arc76.core import derive_seed, derive_seed_with_email

__all__ = ["get_account", "get_email_account"]


def get_account(password: str, slot: int = 0) -> LocalAccount:
    """Derives a deterministic EVM account from a password."""
    return Account.from_key(derive_seed(password, slot))


def get_email_account(email: str, password: str, slot: int = 0) -> LocalAccount:
    """Derives a deterministic EVM account from an email and password."""
    return Account.from_key(derive_seed_with_email(email, password, slot))
