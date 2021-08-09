
using atomex.ViewModel.CurrencyViewModels;
using Atomex;

namespace atomex.ViewModel.SendViewModels
{
    public class TezosSendViewModel : SendViewModel
    {
        public TezosSendViewModel(
            IAtomexApp app,
            CurrencyViewModel currencyViewModel)
            : base(app, currencyViewModel)
        {
        }
    }
}

