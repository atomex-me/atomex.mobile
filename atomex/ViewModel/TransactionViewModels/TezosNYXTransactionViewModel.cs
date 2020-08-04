using Atomex.Blockchain.Tezos;

namespace atomex.ViewModel.TransactionViewModels
{
    public class TezosNYXTransactionViewModel : TezosFA12TransactionViewModel
    {
        public TezosNYXTransactionViewModel()
            : base()
        {
        }

        public TezosNYXTransactionViewModel(TezosTransaction tx)
            : base(tx)
        {
        }
    }
}
