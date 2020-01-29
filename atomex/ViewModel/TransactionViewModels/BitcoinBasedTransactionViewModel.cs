
using Atomex.Blockchain.BitcoinBased;

namespace atomex.ViewModel.TransactionViewModels
{
    public class BitcoinBasedTransactionViewModel : TransactionViewModel
    {
        public BitcoinBasedTransactionViewModel(IBitcoinBasedTransaction tx)
            : base(tx, tx.Amount / (decimal)tx.Currency.DigitsMultiplier)
        {
            Fee = tx.Fees != null
                ? tx.Fees.Value / (decimal)tx.Currency.DigitsMultiplier
                : 0; // todo: N/A
        }
    }
}

