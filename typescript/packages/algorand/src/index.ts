import { mnemonicFromSeed, mnemonicToSecretKey, Account } from "algosdk";
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
