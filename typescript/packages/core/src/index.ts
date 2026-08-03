import { Buffer } from "buffer";

// https://github.com/algorandfoundation/ARCs/blob/main/ARCs/arc-0076.md

const ITERATIONS = 999999;
const MIN_PASSWORD_LENGTH = 16;

function assertPassword(password: string): void {
  if (password.length < MIN_PASSWORD_LENGTH) {
    throw new Error(`Password must be at least ${MIN_PASSWORD_LENGTH} characters long`);
  }
}

function getSubtle(): SubtleCrypto {
  const subtle =
    typeof window !== "undefined" && window.crypto?.subtle
      ? window.crypto.subtle
      : require("crypto").webcrypto.subtle;
  if (!subtle) {
    throw new Error("Crypto API is not available");
  }
  return subtle;
}

async function pbkdf2(input: string, salt: string): Promise<Uint8Array> {
  const subtle = getSubtle();
  const cryptoKey = await subtle.importKey(
    "raw",
    Buffer.from(input, "utf-8"),
    "PBKDF2",
    false,
    ["deriveBits"]
  );
  const bits = await subtle.deriveBits(
    {
      name: "PBKDF2",
      hash: "SHA-256",
      salt: Buffer.from(salt, "utf-8"),
      iterations: ITERATIONS,
    },
    cryptoKey,
    256
  );
  return new Uint8Array(bits);
}

/**
 * Derives the 32-byte ARC76 seed from a password. Every chain-specific arc76-* package builds
 * its account/keypair from this seed.
 */
export async function deriveSeed(password: string, slot: number = 0): Promise<Uint8Array> {
  assertPassword(password);
  const input = `ARC-0076-${password}-${slot}-PBKDF2-999999`;
  const salt = `ARC-0076-${slot}-PBKDF2-999999`;
  return pbkdf2(input, salt);
}

/**
 * Derives the 32-byte ARC76 seed from an email and password.
 */
export async function deriveSeedWithEmail(
  email: string,
  password: string,
  slot: number = 0
): Promise<Uint8Array> {
  assertPassword(password);
  const input = `ARC-0076-${email}-${password}-${slot}-PBKDF2-999999`;
  const salt = `ARC-0076-${email}-${slot}-PBKDF2-999999`;
  return pbkdf2(input, salt);
}
