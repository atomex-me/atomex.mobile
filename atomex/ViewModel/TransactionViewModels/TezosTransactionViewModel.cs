using Atomex;
using Atomex.Blockchain.Abstract;
using Atomex.Blockchain.Tezos;

namespace atomex.ViewModel.TransactionViewModels
{
    public class TezosTransactionViewModel : TransactionViewModel
    {
        public decimal GasLimit { get; set; }
        public bool IsInternal { get; set; }
        public string FromExplorerUri => $"{Currency.AddressExplorerUri}{From}";
        public string ToExplorerUri => $"{Currency.AddressExplorerUri}{To}";
        public string Alias { get; set; }

        public TezosTransactionViewModel(TezosTransaction tx)
            : base(tx, GetAmount(tx), GetFee(tx))
        {
            From = tx.From;
            To = tx.To;
            GasLimit = tx.GasLimit;
            Fee = Tezos.MtzToTz(tx.Fee);
            IsInternal = tx.IsInternal;
            Alias = tx.Alias;
        }

        private static decimal GetAmount(TezosTransaction tx)
        {
            var result = 0m;

            if (tx.Type.HasFlag(BlockchainTransactionType.Input))
                result += tx.Amount / tx.Currency.DigitsMultiplier;

            var includeFee = tx.Currency.Name == tx.Currency.FeeCurrencyName;
            var fee = includeFee ? tx.Fee : 0;

            if (tx.Type.HasFlag(BlockchainTransactionType.Output))
                result += -(tx.Amount + fee) / tx.Currency.DigitsMultiplier;

            tx.InternalTxs?.ForEach(t => result += GetAmount(t));

            return result;
        }

        private static decimal GetFee(TezosTransaction tx)
        {
            var result = 0m;

            if (tx.Type.HasFlag(BlockchainTransactionType.Output))
                result += Tezos.MtzToTz(tx.Fee);

            tx.InternalTxs?.ForEach(t => result += GetFee(t));

            return result;
        }
    }
}