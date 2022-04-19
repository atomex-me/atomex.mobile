using System.Windows.Input;
using Atomex;
using Atomex.Core;
using Xamarin.Forms;

namespace atomex.ViewModel.CurrencyViewModels
{
    public class TezosCurrencyViewModel : CurrencyViewModel
    {
        public bool IsStakingAvailable => CurrencyCode == "XTZ";

        public TezosCurrencyViewModel(
             IAtomexApp app,
             CurrencyConfig currency,
             INavigationService navigationService)
            : base(app, currency, navigationService)
        {
        }

        private ICommand _stakingPageCommand;
        public ICommand StakingPageCommand => _stakingPageCommand ??= new Command(OnStakingButtonClicked);

        private void OnStakingButtonClicked()
        {
            _navigationService?.ShowPage(new DelegationsListPage(new DelegateViewModel(_app, _navigationService)), TabNavigation.Portfolio);
        }
    }
}
