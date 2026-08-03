import { deriveSeed, deriveSeedWithEmail } from "@arc76/core";

/**
 * Minimal shape of the `ecpair` keypair this package returns. Declared locally (rather than
 * imported from `ecpair`) to avoid a TypeScript `node16` module-resolution false-positive on
 * that package's dual CJS/ESM types field; the runtime object is a real `ECPairInterface` and
 * exposes its full API if you need more than this.
 */
export interface BitcoinAccount {
  publicKey: Uint8Array;
  toWIF(): string;
}

// tiny-secp256k1/ecpair are dual CJS/ESM packages; TS's node16 module resolution incorrectly
// flags a static `import` of them (both value and type-only) despite their `require`
// conditional export working fine at runtime. Loading them via plain `require()` (untyped,
// unlike a TS `import` declaration) sidesteps that check entirely.
type ECPair = { fromPrivateKey(buf: Buffer): BitcoinAccount };
let ecPair: ECPair | undefined;
function getECPair(): ECPair {
  if (!ecPair) {
    const ecc = require("tiny-secp256k1");
    const { ECPairFactory } = require("ecpair");
    ecPair = ECPairFactory(ecc) as ECPair;
  }
  return ecPair;
}

/**
 * Derives a deterministic Bitcoin key from a password. Use `bitcoinjs-lib`'s
 * `payments.p2wpkh({ pubkey: account.publicKey })` for the native SegWit (bech32, "bc1...")
 * address, and `account.toWIF()` for the WIF-encoded private key.
 */
export async function getAccount(password: string, slot: number = 0): Promise<BitcoinAccount> {
  const seed = await deriveSeed(password, slot);
  return getECPair().fromPrivateKey(Buffer.from(seed));
}

/**
 * Derives a deterministic Bitcoin key from an email and password.
 */
export async function getEmailAccount(
  email: string,
  password: string,
  slot: number = 0
): Promise<BitcoinAccount> {
  const seed = await deriveSeedWithEmail(email, password, slot);
  return getECPair().fromPrivateKey(Buffer.from(seed));
}
