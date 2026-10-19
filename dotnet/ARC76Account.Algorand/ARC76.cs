using ARC76Account.Core;

namespace ARC76Account.Algorand
{
    /// <summary>
    /// ARC76 account derivation for the Algorand Virtual Machine (AVM).
    /// </summary>
    public static class ARC76
    {
        /// <summary>
        /// Derives a deterministic Algorand account from a password.
        /// </summary>
        public static global::Algorand.Algod.Model.Account GetAccount(string password, int slot = 0)
        {
            var seed = Arc76Seed.Derive(password, slot);
            return new global::Algorand.Algod.Model.Account(seed);
        }

        /// <summary>
        /// Derives a deterministic Algorand account from an email and password.
        /// </summary>
        public static global::Algorand.Algod.Model.Account GetEmailAccount(string email, string password, int slot = 0)
        {
            var seed = Arc76Seed.Derive(email, password, slot);
            return new global::Algorand.Algod.Model.Account(seed);
        }

        /// <summary>
        /// Derives a deterministic Algorand post-quantum Falcon-1024 account from a password.
        /// The same 32-byte ARC76 seed as <see cref="GetAccount(string, int)"/> is fed through
        /// Algorand's standard Falcon derivation (keygen seed = SHA-512/256("PQK" + "f1" + seed)),
        /// so the account has a 24-word mnemonic (<c>ToMnemonic()</c>) and is a different
        /// account from the Ed25519 one derived from the same inputs.
        /// </summary>
        public static global::Algorand.Algod.Model.FalconAccount GetFalconAccount(string password, int slot = 0)
        {
            var seed = Arc76Seed.Derive(password, slot);
            return global::Algorand.Algod.Model.FalconAccount.FromEntropy(seed);
        }

        /// <summary>
        /// Derives a deterministic Algorand post-quantum Falcon-1024 account from an email and password.
        /// See <see cref="GetFalconAccount(string, int)"/>.
        /// </summary>
        public static global::Algorand.Algod.Model.FalconAccount GetEmailFalconAccount(string email, string password, int slot = 0)
        {
            var seed = Arc76Seed.Derive(email, password, slot);
            return global::Algorand.Algod.Model.FalconAccount.FromEntropy(seed);
        }
    }
}
