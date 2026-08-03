import nacl from "tweetnacl";
import { deriveSeed, deriveSeedWithEmail } from "arc76-core";

export interface SolanaAccount {
  /** 32-byte Ed25519 public key - base58-encode this for the Solana address. */
  publicKey: Uint8Array;
  /** 64-byte expanded Ed25519 secret key - base58-encode this for the private key (the same
   *  format wallets like Phantom use for private-key export). */
  secretKey: Uint8Array;
}

/**
 * Derives a deterministic Solana account from a password. The 32-byte ARC76 seed is expanded
 * directly into an Ed25519 keypair (no BIP32/BIP44 hierarchical derivation, no mnemonic).
 */
export async function getAccount(password: string, slot: number = 0): Promise<SolanaAccount> {
  const seed = await deriveSeed(password, slot);
  return nacl.sign.keyPair.fromSeed(seed);
}

/**
 * Derives a deterministic Solana account from an email and password.
 */
export async function getEmailAccount(
  email: string,
  password: string,
  slot: number = 0
): Promise<SolanaAccount> {
  const seed = await deriveSeedWithEmail(email, password, slot);
  return nacl.sign.keyPair.fromSeed(seed);
}
