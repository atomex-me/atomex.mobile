using Atomex;
using Atomex.Blockchain.Abstract;
using Atomex.Blockchain.Ethereum;

namespace atomex.ViewModel.TransactionViewModels
{
    public class EthereumTransactionViewModel : TransactionViewModel
    {
        public string From { get; set; }
        public string To { get; set; }
        public decimal GasPrice { get; set; }
        public decimal GasLimit { get; set; }
        public decimal GasUsed { get; set; }
        public bool IsInternal { get; set; }
        public string FromExplorerUri => $"{Currency.AddressExplorerUri}{From}";
        public string ToExplorerUri => $"{Currency.AddressExplorerUri}{To}";


        public EthereumTransactionViewModel(EthereumTransaction tx)
            : base(tx, GetAmount(tx))
        {
            From = tx.From;
            To = tx.To;
            GasPrice = (decimal)tx.GasPrice;
            GasLimit = (decimal)tx.GasLimit;
            GasUsed = (decimal)tx.GasUsed;
            IsInternal = tx.IsInternal;
        }

        private static decimal GetAmount(EthereumTransaction tx)
        {
            var result = 0m;

            if (tx.Type.HasFlag(BlockchainTransactionType.Input))
                result += Ethereum.WeiToEth(tx.Amount);

            if (tx.Type.HasFlag(BlockchainTransactionType.Output))
                result += -Ethereum.WeiToEth(tx.Amount + tx.GasUsed * tx.GasPrice);

            tx.InternalTxs?.ForEach(t => result += GetAmount(t));

            return result;
        }
    }
}
