using Atomex;
using Atomex.Blockchain.Abstract;
using Atomex.Blockchain.Tezos;

namespace atomex.ViewModel.TransactionViewModels
{
    public class TezosTransactionViewModel : TransactionViewModel
    {
        public decimal GasLimit { get; set; }
        public bool IsInternal { get; set; }
        public string Alias { get; set; }


        public TezosTransactionViewModel(TezosTransaction tx, TezosConfig tezosConfig)
            : base(tx, tezosConfig, GetAmount(tx, tezosConfig), GetFee(tx))
        {
            From = tx.From;
            To = tx.To;
            GasLimit = tx.GasLimit;
            Fee = TezosConfig.MtzToTz(tx.Fee);
            IsInternal = tx.IsInternal;
            Alias = tx.Alias;
        }

        private static decimal GetAmount(TezosTransaction tx, TezosConfig tezosConfig)
        {
            var result = 0m;

            if (tx.Type.HasFlag(BlockchainTransactionType.Input))
                result += tx.Amount / tezosConfig.DigitsMultiplier;

            var includeFee = tezosConfig.Name == tezosConfig.FeeCurrencyName;
            var fee = includeFee ? tx.Fee : 0;

            if (tx.Type.HasFlag(BlockchainTransactionType.Output))
                result += -(tx.Amount + fee) / tezosConfig.DigitsMultiplier;

            tx.InternalTxs?.ForEach(t => result += GetAmount(t, tezosConfig));

            return result;
        }

        private static decimal GetFee(TezosTransaction tx)
        {
            var result = 0m;

            if (tx.Type.HasFlag(BlockchainTransactionType.Output))
                result += TezosConfig.MtzToTz(tx.Fee);

            tx.InternalTxs?.ForEach(t => result += GetFee(t));

            return result;
        }
    }
}