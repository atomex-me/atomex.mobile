using System;
using System.Windows.Input;
using atomex.Views.Delegate;
using Atomex.Blockchain.Tezos;
using Xamarin.Essentials;
using Xamarin.Forms;

namespace atomex.ViewModel
{
    public class DelegationViewModel
    {
        private INavigationService _navigationService { get; set; }

        public BakerData Baker { get; set; }
        public string Address { get; set; }
        public decimal Balance { get; set; }
        public string BbUri { get; set; }
        public DateTime DelegationTime { get; set; }
        public string Status { get; set; }

        public DelegateViewModel DelegateViewModel;

        public DelegationViewModel(DelegateViewModel delegateViewModel, INavigationService navigationService)
        {
            DelegateViewModel = delegateViewModel ?? throw new ArgumentNullException(nameof(DelegateViewModel));
            _navigationService = navigationService ?? throw new ArgumentNullException(nameof(_navigationService)); 
        }

        private ICommand _checkRewardsCommand;
        public ICommand CheckRewardsCommand => _checkRewardsCommand ??= new Command((item) => OnCheckRewardsButtonClicked());

        private void OnCheckRewardsButtonClicked()
        {
            if (!string.IsNullOrEmpty(BbUri))
            {
                Launcher.OpenAsync(new Uri(BbUri + Address));
            }
        }

        private ICommand _changeBakerCommand;
        public ICommand ChangeBakerCommand => _changeBakerCommand ??= new Command((item) => OnChangeBakerButtonClicked());

        private void OnChangeBakerButtonClicked()
        {
            if (!string.IsNullOrEmpty(Address))
            {
                //DelegateViewModel.SetWalletAddress(Address);
                _navigationService?.ShowPage(new DelegatePage(DelegateViewModel), TabNavigation.Portfolio);
            }
        }
    }
}
