using ARC76Account.Core;
using Nethereum.Signer;

namespace ARC76Account.Ethereum
{
    /// <summary>
    /// ARC76 account derivation for Ethereum and other EVM-compatible chains.
    /// </summary>
    public static class ARC76
    {
        /// <summary>
        /// Derives a deterministic EVM account from a password.
        /// </summary>
        public static EthECKey GetAccount(string password, int slot = 0)
        {
            var seed = Arc76Seed.Derive(password, slot);
            return new EthECKey(seed, true, 0);
        }

        /// <summary>
        /// Derives a deterministic EVM account from an email and password.
        /// </summary>
        public static EthECKey GetEmailAccount(string email, string password, int slot = 0)
        {
            var seed = Arc76Seed.Derive(email, password, slot);
            return new EthECKey(seed, true, 0);
        }
    }
}
