using atomex.Common;
using Atomex;
using Atomex.Blockchain.Abstract;
using Atomex.Blockchain.Ethereum;
using Atomex.EthereumTokens;

namespace atomex.ViewModels.TransactionViewModels
{
    public class EthereumERC20TransactionViewModel : TransactionViewModel
    {
        public decimal GasPrice { get; set; }
        public decimal GasLimit { get; set; }
        public decimal GasUsed { get; set; }
        public bool IsInternal { get; set; }
        public string Alias { get; set; }

        public EthereumERC20TransactionViewModel(
            EthereumTransaction tx,
            Erc20Config erc20Config,
            INavigationService navigationService)
            : base(tx, erc20Config, GetAmount(tx, erc20Config), 0, navigationService)
        {
            From = tx.From;
            To = tx.To;
            GasPrice = EthereumConfig.WeiToGwei((decimal) tx.GasPrice);
            GasLimit = (decimal) tx.GasLimit;
            GasUsed = (decimal) tx.GasUsed;
            IsInternal = tx.IsInternal;
            Alias = Amount switch
            {
                <= 0 => tx.To.TruncateAddress(),
                > 0 => tx.From.TruncateAddress()
            };
        }

        public static decimal GetAmount(
            EthereumTransaction tx,
            Erc20Config erc20Config)
        {
            var result = 0m;

            if (tx.Type.HasFlag(BlockchainTransactionType.SwapRedeem) ||
                tx.Type.HasFlag(BlockchainTransactionType.SwapRefund))
                result += erc20Config.TokenDigitsToTokens(tx.Amount);
            else
            {
                if (tx.Type.HasFlag(BlockchainTransactionType.Input))
                    result += erc20Config.TokenDigitsToTokens(tx.Amount);
                if (tx.Type.HasFlag(BlockchainTransactionType.Output))
                    result += -erc20Config.TokenDigitsToTokens(tx.Amount);
            }

            tx.InternalTxs?.ForEach(t => result += GetAmount(t, erc20Config));

            return result;
        }
    }
}