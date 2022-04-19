using System;
using Atomex;
using Atomex.Blockchain.Abstract;
using Atomex.Blockchain.BitcoinBased;
using Atomex.Blockchain.Ethereum;
using Atomex.Blockchain.Tezos;
using Atomex.Core;
using Atomex.EthereumTokens;

namespace atomex.ViewModel.TransactionViewModels
{
    public static class TransactionViewModelCreator
    {
        public static TransactionViewModel CreateViewModel(
            IBlockchainTransaction tx,
            CurrencyConfig currencyConfig,
            INavigationService navigationService)
        {
            return tx.Currency switch
            {
                "BTC" => (TransactionViewModel)new BitcoinBasedTransactionViewModel(tx as IBitcoinBasedTransaction, currencyConfig as BitcoinBasedConfig, navigationService),
                "LTC" => new BitcoinBasedTransactionViewModel(tx as IBitcoinBasedTransaction, currencyConfig as BitcoinBasedConfig, navigationService),
                "USDT" => new EthereumERC20TransactionViewModel(tx as EthereumTransaction, currencyConfig as Erc20Config, navigationService),
                "TBTC" => new EthereumERC20TransactionViewModel(tx as EthereumTransaction, currencyConfig as Erc20Config, navigationService),
                "WBTC" => new EthereumERC20TransactionViewModel(tx as EthereumTransaction, currencyConfig as Erc20Config, navigationService),
                "ETH" => new EthereumTransactionViewModel(tx as EthereumTransaction, currencyConfig as EthereumConfig, navigationService),
                "XTZ" => new TezosTransactionViewModel(tx as TezosTransaction, currencyConfig as TezosConfig, navigationService),
                _ => throw new NotSupportedException("Not supported transaction type."),
            };
        }
    }
}

