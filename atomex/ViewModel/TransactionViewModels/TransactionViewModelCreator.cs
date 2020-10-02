using Atomex;
using Atomex.Blockchain.Abstract;
using Atomex.Blockchain.BitcoinBased;
using Atomex.Blockchain.Ethereum;
using Atomex.Blockchain.Tezos;
using Atomex.EthereumTokens;
using Atomex.TezosTokens;

namespace atomex.ViewModel.TransactionViewModels
{
    public static class TransactionViewModelCreator
    {
        public static TransactionViewModel CreateViewModel(IBlockchainTransaction tx)
        {
            switch (tx.Currency)
            {
                case BitcoinBasedCurrency _:
                    return new BitcoinBasedTransactionViewModel((IBitcoinBasedTransaction)tx);
                case Tether _:
                    return new EthereumERC20TransactionViewModel((EthereumTransaction)tx);
                case TBTC _:
                    return new EthereumERC20TransactionViewModel((EthereumTransaction)tx);
                case WBTC _:
                    return new EthereumERC20TransactionViewModel((EthereumTransaction)tx);
                case Ethereum _:
                    return new EthereumTransactionViewModel((EthereumTransaction)tx);
                case NYX _:
                    return new TezosFA12TransactionViewModel((TezosTransaction)tx);
                case FA2 _:
                    return new TezosFA12TransactionViewModel((TezosTransaction)tx);
                case FA12 _:
                    return new TezosFA12TransactionViewModel((TezosTransaction)tx);
                case Tezos _:
                    return new TezosTransactionViewModel((TezosTransaction)tx);
            }

            return null;
        }
    }
}

