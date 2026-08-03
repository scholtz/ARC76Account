using ARC76Account.Core;
using NBitcoin;

namespace ARC76Account.BitcoinCash
{
    /// <summary>
    /// ARC76 account derivation for Bitcoin Cash.
    /// </summary>
    public static class ARC76
    {
        /// <summary>
        /// Derives a deterministic Bitcoin Cash key from a password. Use
        /// <see cref="Key.PubKey"/>.GetAddress(ScriptPubKeyType.Legacy, NBitcoin.Altcoins.BCash.Instance.Mainnet)
        /// for the CashAddr ("bitcoincash:q...") address - the standard Bitcoin Cash address
        /// format since the 2018 network-wide CashAddr migration - and
        /// <see cref="Key.GetWif"/> for the WIF-encoded private key. Bitcoin Cash does not
        /// support SegWit, so "Legacy" here means "the only P2PKH type this chain has", not a
        /// legacy/deprecated format.
        /// </summary>
        public static Key GetAccount(string password, int slot = 0)
        {
            var seed = Arc76Seed.Derive(password, slot);
            return new Key(seed);
        }

        /// <summary>
        /// Derives a deterministic Bitcoin Cash key from an email and password.
        /// </summary>
        public static Key GetEmailAccount(string email, string password, int slot = 0)
        {
            var seed = Arc76Seed.Derive(email, password, slot);
            return new Key(seed);
        }
    }
}
