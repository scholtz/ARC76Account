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
    }
}
