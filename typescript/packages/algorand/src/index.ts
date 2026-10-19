import { mnemonicFromSeed, mnemonicToSecretKey, Account, encodeAddress } from "algosdk";
import { sha512_256 } from "js-sha512";
import { deriveSeed, deriveSeedWithEmail } from "@arc76/core";

/**
 * Derives a deterministic Algorand account from a password.
 */
export async function getAccount(password: string, slot: number = 0): Promise<Account> {
  const seed = await deriveSeed(password, slot);
  const mnemonic = mnemonicFromSeed(seed);
  return mnemonicToSecretKey(mnemonic);
}

/**
 * Derives a deterministic Algorand account from an email and password.
 */
export async function getEmailAccount(
  email: string,
  password: string,
  slot: number = 0
): Promise<Account> {
  const seed = await deriveSeedWithEmail(email, password, slot);
  const mnemonic = mnemonicFromSeed(seed);
  return mnemonicToSecretKey(mnemonic);
}

/** A post-quantum Falcon-1024 Algorand account. */
export interface FalconAccount {
  /** Base32 Algorand address (checksummed). */
  address: string;
  /** Raw 1793-byte Falcon-1024 public key. */
  publicKey: Uint8Array;
  /** Raw 2305-byte Falcon-1024 private key (as used by `falcon-1024`'s `signCompressed`). */
  privateKey: Uint8Array;
  /** Canonical address salt (0-255), needed with `publicKey` to build the PQ signature envelope. */
  salt: number;
}

const FALCON_1024_SCHEME = new Uint8Array([0x66, 0x31]); // "f1"
const ascii = (s: string) => new TextEncoder().encode(s);

function sha512_256Bytes(...parts: Uint8Array[]): Uint8Array {
  const data = new Uint8Array(parts.reduce((n, p) => n + p.length, 0));
  let off = 0;
  for (const p of parts) {
    data.set(p, off);
    off += p.length;
  }
  return new Uint8Array(sha512_256.arrayBuffer(data));
}

const P = (1n << 255n) - 19n;
const D = (((-121665n % P) + P) * modPow(121666n, P - 2n)) % P;

function modPow(base: bigint, exp: bigint): bigint {
  let result = 1n;
  base %= P;
  while (exp > 0n) {
    if (exp & 1n) result = (result * base) % P;
    base = (base * base) % P;
    exp >>= 1n;
  }
  return result;
}

/**
 * True if the 32 bytes decode to any point on the Ed25519 curve - the same broad predicate as
 * go-algorand's basics.IsEdwards25519Point (ports py-algorand-sdk's ed25519_check).
 */
function isEd25519Point(bytes: Uint8Array): boolean {
  let y = 0n;
  for (let i = 31; i >= 0; i--) y = (y << 8n) | BigInt(bytes[i]);
  y = (y & ((1n << 255n) - 1n)) % P;
  const u = (y * y - 1n + P) % P;
  const v = (D * y * y + 1n) % P;
  const x = (u * modPow(v, 3n) * modPow((u * modPow(v, 7n)) % P, (P - 5n) / 8n)) % P;
  const vxx = (v * x * x) % P;
  return vxx === u || vxx === (P - u) % P;
}

function falconFromSeed(seed: Uint8Array): FalconAccount {
  // Loaded lazily so plain Ed25519 users never load the Falcon WASM module.
  // eslint-disable-next-line @typescript-eslint/no-require-imports
  const { generateKey } = require("falcon-1024") as typeof import("falcon-1024");
  // Same derivation as Algorand's standard Falcon accounts (algosdk-py's to_pq_seed, .NET's
  // FalconAccount.FromEntropy): keygen seed = SHA-512/256("PQK" + scheme + seed).
  const keygenSeed = sha512_256Bytes(ascii("PQK"), FALCON_1024_SCHEME, seed);
  const { publicKey, privateKey } = generateKey(keygenSeed);
  for (let salt = 0; salt < 256; salt++) {
    const candidate = sha512_256Bytes(ascii("PQA"), FALCON_1024_SCHEME, new Uint8Array([salt]), publicKey);
    if (!isEd25519Point(candidate)) {
      return { address: encodeAddress(candidate), publicKey, privateKey, salt };
    }
  }
  throw new Error("No canonical salt found for Falcon public key");
}

/**
 * Derives a deterministic Algorand post-quantum (Falcon-1024) account from a password. Uses the
 * same 32-byte ARC76 seed as {@link getAccount}, so it is a different account from the Ed25519
 * one for the same inputs.
 */
export async function getFalconAccount(password: string, slot: number = 0): Promise<FalconAccount> {
  return falconFromSeed(await deriveSeed(password, slot));
}

/**
 * Derives a deterministic Algorand post-quantum (Falcon-1024) account from an email and password.
 */
export async function getEmailFalconAccount(
  email: string,
  password: string,
  slot: number = 0
): Promise<FalconAccount> {
  return falconFromSeed(await deriveSeedWithEmail(email, password, slot));
}
