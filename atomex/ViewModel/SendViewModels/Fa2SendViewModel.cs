using atomex.ViewModel.CurrencyViewModels;
using Atomex;

namespace atomex.ViewModel.SendViewModels
{
    public class Fa2SendViewModel : Fa12SendViewModel
    {
        public Fa2SendViewModel(
            IAtomexApp app,
            CurrencyViewModel currencyViewModel,
            INavigationService navigationService)
            : base(app, currencyViewModel, navigationService)
        {
        }
    }
}
