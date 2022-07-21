using Atomex.Blockchain.BitcoinBased;
using Atomex.Blockchain.Abstract;
using Atomex;

namespace atomex.ViewModels.TransactionViewModels
{
    public class BitcoinBasedTransactionViewModel : TransactionViewModel
    {
        public BitcoinBasedTransactionViewModel(
            IBitcoinBasedTransaction tx,
            BitcoinBasedConfig bitcoinBasedConfig,
            INavigationService navigationService)
            : base(tx, bitcoinBasedConfig, tx.Amount / bitcoinBasedConfig.DigitsMultiplier,
                GetFee(tx, bitcoinBasedConfig), navigationService)
        {
            Fee = tx.Fees != null
                ? tx.Fees.Value / bitcoinBasedConfig.DigitsMultiplier
                : 0; // todo: N/A        
        }

        private static decimal GetFee(
            IBitcoinBasedTransaction tx,
            BitcoinBasedConfig bitcoinBasedConfig)
        {
            return tx.Fees != null
                ? tx.Type.HasFlag(BlockchainTransactionType.Output)
                    ? tx.Fees.Value / bitcoinBasedConfig.DigitsMultiplier
                    : 0
                : 0;
        }
    }
}