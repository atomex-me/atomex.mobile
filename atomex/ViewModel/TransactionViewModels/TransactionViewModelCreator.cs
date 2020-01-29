using Atomex;
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
            switch (tx.Currency)
            {
                case BitcoinBasedCurrency _:
                    return new BitcoinBasedTransactionViewModel((IBitcoinBasedTransaction)tx);
                case Ethereum _:
                    return new EthereumTransactionViewModel((EthereumTransaction)tx);
                case Tezos _:
                    return new TezosTransactionViewModel((TezosTransaction)tx);
            }

            return null;
        }
    }
}

