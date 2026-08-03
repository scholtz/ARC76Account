import { Buffer } from "buffer";
import { mnemonicFromSeed, mnemonicToSecretKey } from "algosdk";
// https://github.com/algorandfoundation/ARCs/blob/main/ARCs/arc-0076.md

const generateSeed = async (
  password: string,
  email: string = "",
  slot: number = 0
): Promise<Uint8Array> => {
  if (password.length < 16) {
    throw Error("Password must be at least 16 characters long");
  }

  const emailAddition = email ? `-${email}` : "";
  const init = `ARC-0076${emailAddition}-${password}-${slot}-PBKDF2-999999`;
  const salt = `ARC-0076${emailAddition}-${slot}-PBKDF2-999999`;
  const iterations = 999999;
  const subtle =
    typeof window !== "undefined" && window.crypto?.subtle
      ? window.crypto.subtle
      : require("crypto").webcrypto.subtle;
  if (!subtle) {
    throw new Error("Crypto API is not available");
  }
  const cryptoKey = await subtle.importKey(
    "raw",
    Buffer.from(init, "utf-8"),
    "PBKDF2",
    false,
    ["deriveBits", "deriveKey"]
  );
  const masterBits = await subtle.deriveBits(
    {
      name: "PBKDF2",
      hash: "SHA-256",
      salt: Buffer.from(salt, "utf-8"),
      iterations: iterations,
    },
    cryptoKey,
    256
  );
  return new Uint8Array(masterBits);
};
const generateAlgorandAccount = async (
  password: string,
  email: string = "",
  slot: number = 0
) => {
  const uint8 = await generateSeed(password, email, slot);
  const mnemonic = mnemonicFromSeed(uint8);
  const genAccount = mnemonicToSecretKey(mnemonic);
  return genAccount;
};
export { generateSeed, generateAlgorandAccount };
