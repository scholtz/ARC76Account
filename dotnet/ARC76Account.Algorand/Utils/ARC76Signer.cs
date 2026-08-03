using Algorand.Algod.Model;
using Algorand.Algod.Model.Transactions;
using ARC76Account.Model.Config;
using Microsoft.Extensions.Options;

namespace ARC76Account.Utils
{
    /// <summary>
    /// Signs Algorand transactions with a password-derived ARC76 account. Intended for
    /// <c>AddSingleton</c> registration (see the package README) - which means the derived
    /// private key material lives in process memory for the entire application lifetime, not
    /// zeroed/cleared between uses. This is the same exposure as holding any other long-lived
    /// secret/API key in memory, and is an explicit tradeoff of the singleton pattern this
    /// class is designed for. If that's unacceptable for your threat model (e.g. you need
    /// HSM/KMS-backed signing, or want to minimize the key's in-memory lifetime), derive an
    /// account on-demand per operation via <see cref="ARC76Account.Algorand.ARC76"/> instead of
    /// using this singleton signer, or use an HSM/KMS-backed signer entirely.
    /// </summary>
    public class ARC76Signer : IAlgorandSigner
    {
        private readonly Account k;
        /// <summary>
        /// Constructor for ARC76 signer
        /// </summary>
        /// <param name="arc76Config"></param>
        /// <exception cref="Exception"></exception>
        public ARC76Signer(IOptionsMonitor<ARC76Config> arc76Config)
        {
            if (string.IsNullOrEmpty(arc76Config.CurrentValue.Data))
            {
                throw new Exception("Please configure ARC76:Data");
            }
            k = Algorand.ARC76.GetAccount(arc76Config.CurrentValue.Data);
        }

        public global::Algorand.Address getAddress()
        {
            return k.Address;
        }

        /// <summary>
        /// Sign txs with ARC76 account
        /// </summary>
        /// <param name="txs"></param>
        /// <returns></returns>
        public IEnumerable<SignedTransaction> Sign(Transaction[] txs)
        {
            return txs.Select(tx => tx.Sign(k));
        }
    }
}
