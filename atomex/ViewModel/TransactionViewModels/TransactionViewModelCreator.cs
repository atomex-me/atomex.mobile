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
            CurrencyConfig currencyConfig)
        {
            return tx.Currency switch
            {
                "BTC" => (TransactionViewModel)new BitcoinBasedTransactionViewModel(tx as IBitcoinBasedTransaction, currencyConfig as BitcoinBasedConfig),
                "LTC" => new BitcoinBasedTransactionViewModel(tx as IBitcoinBasedTransaction, currencyConfig as BitcoinBasedConfig),
                "USDT" => new EthereumERC20TransactionViewModel(tx as EthereumTransaction, currencyConfig as Erc20Config),
                "TBTC" => new EthereumERC20TransactionViewModel(tx as EthereumTransaction, currencyConfig as Erc20Config),
                "WBTC" => new EthereumERC20TransactionViewModel(tx as EthereumTransaction, currencyConfig as Erc20Config),
                "ETH" => new EthereumTransactionViewModel(tx as EthereumTransaction, currencyConfig as EthereumConfig),
                "XTZ" => new TezosTransactionViewModel(tx as TezosTransaction, currencyConfig as TezosConfig),
                _ => throw new NotSupportedException("Not supported transaction type."),
            };
        }
    }
}

