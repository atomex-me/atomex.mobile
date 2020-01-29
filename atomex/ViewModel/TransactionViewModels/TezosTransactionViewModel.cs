using Atomex;
using Atomex.Blockchain.Abstract;
using Atomex.Blockchain.Tezos;

namespace atomex.ViewModel.TransactionViewModels
{
    public class TezosTransactionViewModel : TransactionViewModel
    {
        public string From { get; set; }
        public string To { get; set; }
        public decimal GasLimit { get; set; }
        public bool IsInternal { get; set; }
        public string FromExplorerUri => $"{Currency.AddressExplorerUri}{From}";
        public string ToExplorerUri => $"{Currency.AddressExplorerUri}{To}";

        public TezosTransactionViewModel(TezosTransaction tx)
            : base(tx, GetAmount(tx))
        {
            From = tx.From;
            To = tx.To;
            GasLimit = tx.GasLimit;
            Fee = tx.Fee;
            IsInternal = tx.IsInternal;
        }

        private static decimal GetAmount(TezosTransaction tx)
        {
            var result = 0m;

            if (tx.Type.HasFlag(BlockchainTransactionType.Input))
                result += Tezos.MtzToTz(tx.Amount);

            if (tx.Type.HasFlag(BlockchainTransactionType.Output))
                result += -Tezos.MtzToTz(tx.Amount + tx.Fee);

            tx.InternalTxs?.ForEach(t => result += GetAmount(t));

            return result;
        }
    }
}