using ARC76Account.Core;
using Chaos.NaCl;
using Solnet.Wallet;

namespace ARC76Account.Solana
{
    /// <summary>
    /// ARC76 account derivation for Solana.
    /// </summary>
    public static class ARC76
    {
        /// <summary>
        /// Derives a deterministic Solana account from a password. The ARC76 seed is
        /// expanded into an Ed25519 keypair (the same "raw seed" approach other SDKs expose
        /// as e.g. <c>Keypair.fromSeed</c> - no BIP32/BIP44 hierarchical derivation is
        /// involved). Use <see cref="Account.PublicKey"/> (base58) as the address and
        /// <see cref="Account.PrivateKey"/> (base58 of the 64-byte expanded secret key) as
        /// the private key - the same format wallets like Phantom export.
        /// </summary>
        public static Account GetAccount(string password, int slot = 0)
        {
            var seed = Arc76Seed.Derive(password, slot);
            return FromSeed(seed);
        }

        /// <summary>
        /// Derives a deterministic Solana account from an email and password.
        /// </summary>
        public static Account GetEmailAccount(string email, string password, int slot = 0)
        {
            var seed = Arc76Seed.Derive(email, password, slot);
            return FromSeed(seed);
        }

        private static Account FromSeed(byte[] seed)
        {
            Ed25519.KeyPairFromSeed(out var publicKey, out var expandedPrivateKey, seed);
            return new Account(expandedPrivateKey, publicKey);
        }
    }
}
