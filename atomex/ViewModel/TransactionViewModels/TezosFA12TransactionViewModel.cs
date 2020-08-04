using Atomex;
using Atomex.Blockchain.Abstract;
using Atomex.Blockchain.Tezos;

namespace atomex.ViewModel.TransactionViewModels
{
    public class TezosFA12TransactionViewModel : TransactionViewModel
    {
        public string From { get; set; }
        public string To { get; set; }
        public decimal GasLimit { get; set; }
        public bool IsInternal { get; set; }
        public string FromExplorerUri => $"{Currency.AddressExplorerUri}{From}";
        public string ToExplorerUri => $"{Currency.AddressExplorerUri}{To}";

        public TezosFA12TransactionViewModel()
        {
        }

        public TezosFA12TransactionViewModel(TezosTransaction tx)
            : base(tx, GetAmount(tx), 0)
        {
            From = tx.From;
            To = tx.To;
            GasLimit = tx.GasLimit;
            Fee = Tezos.MtzToTz(tx.Fee);
            IsInternal = tx.IsInternal;
        }

        private static decimal GetAmount(TezosTransaction tx)
        {
            var result = 0m;

            if (tx.Type.HasFlag(BlockchainTransactionType.SwapRedeem) ||
                tx.Type.HasFlag(BlockchainTransactionType.SwapRefund))
                result += tx.Amount.FromTokenDigits(tx.Currency.DigitsMultiplier);
            else
            {
                if (tx.Type.HasFlag(BlockchainTransactionType.Input))
                    result += tx.Amount.FromTokenDigits(tx.Currency.DigitsMultiplier);
                if (tx.Type.HasFlag(BlockchainTransactionType.Output))
                    result += -tx.Amount.FromTokenDigits(tx.Currency.DigitsMultiplier);
            }

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
