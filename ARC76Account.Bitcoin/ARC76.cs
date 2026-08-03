using ARC76Account.Core;
using NBitcoin;

namespace ARC76Account.Bitcoin
{
    /// <summary>
    /// ARC76 account derivation for Bitcoin.
    /// </summary>
    public static class ARC76
    {
        /// <summary>
        /// Derives a deterministic Bitcoin key from a password. Use
        /// <see cref="Key.PubKey"/>.GetAddress(ScriptPubKeyType.Segwit, network) for the
        /// native SegWit (bech32, "bc1...") address and <see cref="Key.GetWif"/> for the
        /// WIF-encoded private key.
        /// </summary>
        public static Key GetAccount(string password, int slot = 0)
        {
            var seed = Arc76Seed.Derive(password, slot);
            return new Key(seed);
        }

        /// <summary>
        /// Derives a deterministic Bitcoin key from an email and password.
        /// </summary>
        public static Key GetEmailAccount(string email, string password, int slot = 0)
        {
            var seed = Arc76Seed.Derive(email, password, slot);
            return new Key(seed);
        }
    }
}
