namespace ARC76Account.Utils
{
    public interface IAlgorandSigner
    {
        /// <summary>
        /// Sign transactions
        /// </summary>
        /// <param name="txs">List of unsigned transactions</param>
        /// <returns></returns>
        public IEnumerable<global::Algorand.Algod.Model.Transactions.SignedTransaction> Sign(global::Algorand.Algod.Model.Transactions.Transaction[] txs);
        /// <summary>
        /// Returns the signing address
        /// </summary>
        /// <returns></returns>
        public global::Algorand.Address getAddress();
    }
}
