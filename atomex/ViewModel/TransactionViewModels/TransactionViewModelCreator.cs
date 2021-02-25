using System;
using Atomex.Blockchain.Abstract;
using Atomex.Blockchain.BitcoinBased;
using Atomex.Blockchain.Ethereum;
using Atomex.Blockchain.Tezos;

namespace atomex.ViewModel.TransactionViewModels
{
    public static class TransactionViewModelCreator
    {
        public static TransactionViewModel CreateViewModel(IBlockchainTransaction tx)
        {
            return tx.Currency.Name switch
            { 
                 "BTC"   => (TransactionViewModel)new BitcoinBasedTransactionViewModel((IBitcoinBasedTransaction)tx),
                 "LTC"   => new BitcoinBasedTransactionViewModel((IBitcoinBasedTransaction)tx),
                 "USDT"  => new EthereumERC20TransactionViewModel((EthereumTransaction)tx),
                 "TBTC"  => new EthereumERC20TransactionViewModel((EthereumTransaction)tx),
                 "WBTC"  => new EthereumERC20TransactionViewModel((EthereumTransaction)tx),
                 "ETH"   => new EthereumTransactionViewModel((EthereumTransaction)tx),
                 "NYX"   => new TezosNYXTransactionViewModel((TezosTransaction)tx),
                 "FA2"   => new TezosFA2TransactionViewModel((TezosTransaction)tx),
                 "FA12"  => new TezosFA12TransactionViewModel((TezosTransaction)tx),
                 "TZBTC" => new TezosFA12TransactionViewModel((TezosTransaction)tx),
                 "KUSD"  => new TezosFA12TransactionViewModel((TezosTransaction)tx),
                 "XTZ"   => new TezosTransactionViewModel((TezosTransaction)tx),
                 _ => throw new NotSupportedException("Not supported transaction type."),
             };
        }
    }
}

