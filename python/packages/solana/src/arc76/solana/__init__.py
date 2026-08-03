"""Deterministic ARC76 account derivation for Solana.

https://github.com/algorandfoundation/ARCs/blob/main/ARCs/arc-0076.md
"""

from __future__ import annotations

from dataclasses import dataclass

import nacl.signing

from arc76.core import derive_seed, derive_seed_with_email

__all__ = ["SolanaAccount", "get_account", "get_email_account"]


@dataclass(frozen=True)
class SolanaAccount:
    """public_key: 32-byte Ed25519 public key - base58-encode this for the Solana address.
    secret_key: 64-byte expanded Ed25519 secret key (seed || public_key) - base58-encode this
        for the private key, the same format wallets like Phantom use for private-key export.
    """

    public_key: bytes
    secret_key: bytes


def _from_seed(seed: bytes) -> SolanaAccount:
    # The 32-byte ARC76 seed is expanded directly into an Ed25519 keypair (no BIP32/BIP44
    # hierarchical derivation, no mnemonic) - matching nacl.sign.keyPair.fromSeed (TS) /
    # Ed25519.KeyPairFromSeed (.NET).
    signing_key = nacl.signing.SigningKey(seed)
    verify_key = signing_key.verify_key
    return SolanaAccount(
        public_key=bytes(verify_key),
        secret_key=signing_key.encode() + bytes(verify_key),
    )


def get_account(password: str, slot: int = 0) -> SolanaAccount:
    """Derives a deterministic Solana account from a password."""
    return _from_seed(derive_seed(password, slot))


def get_email_account(email: str, password: str, slot: int = 0) -> SolanaAccount:
    """Derives a deterministic Solana account from an email and password."""
    return _from_seed(derive_seed_with_email(email, password, slot))
