import { Buffer } from "buffer";
import { Wallet } from "ethers";
import { deriveSeed, deriveSeedWithEmail } from "arc76-core";

function toHex(seed: Uint8Array): string {
  return "0x" + Buffer.from(seed).toString("hex");
}

/**
 * Derives a deterministic EVM account from a password.
 */
export async function getAccount(password: string, slot: number = 0): Promise<Wallet> {
  const seed = await deriveSeed(password, slot);
  return new Wallet(toHex(seed));
}

/**
 * Derives a deterministic EVM account from an email and password.
 */
export async function getEmailAccount(
  email: string,
  password: string,
  slot: number = 0
): Promise<Wallet> {
  const seed = await deriveSeedWithEmail(email, password, slot);
  return new Wallet(toHex(seed));
}
