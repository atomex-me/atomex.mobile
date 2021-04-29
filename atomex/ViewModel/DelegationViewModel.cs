using System;
using System.Threading.Tasks;
using System.Windows.Input;
using Atomex.Blockchain.Tezos;
using Xamarin.Essentials;
using Xamarin.Forms;

namespace atomex.ViewModel
{
    public class DelegationViewModel
    {
        public INavigation Navigation { get; set; }

        public BakerData Baker { get; set; }
        public string Address { get; set; }
        public decimal Balance { get; set; }
        public string BbUri { get; set; }
        public DateTime DelegationTime { get; set; }
        public string Status { get; set; }

        public DelegateViewModel DelegateViewModel;

        public DelegationViewModel(DelegateViewModel delegateViewModel, INavigation navigation)
        {
            DelegateViewModel = delegateViewModel;
            Navigation = navigation;
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
        public ICommand ChangeBakerCommand => _changeBakerCommand ??= new Command(async (item) => await OnChangeBakerButtonClicked());

        private async Task OnChangeBakerButtonClicked()
        {
            if (!string.IsNullOrEmpty(Address))
            {
                DelegateViewModel.SetWalletAddress(Address);
                await Navigation.PushAsync(new DelegatePage(DelegateViewModel));
            }
        }
    }
}
