using System;
using atomex.Resources;
using atomex.ViewModel;
using Xamarin.Essentials;
using Xamarin.Forms;

namespace atomex
{
    public partial class DelegationsListPage : ContentPage
    {

        private DelegateViewModel _delegateViewModel;

        public DelegationsListPage()
        {
            InitializeComponent();
        }

        public DelegationsListPage(DelegateViewModel delegateViewModel)
        {
            InitializeComponent();
            _delegateViewModel = delegateViewModel;
            BindingContext = delegateViewModel;
        }

        private async void OnNewDelegationButtonClicked(object sender, EventArgs args)
        {
            if (_delegateViewModel.CanDelegate)
            {
                await Navigation.PushAsync(new DelegatePage(_delegateViewModel));
            }
            else
            {
                await DisplayAlert(AppResources.Error, AppResources.NoTezosError, AppResources.AcceptButton);
            }
        }
        private void OnDelegationTapped(object sender, ItemTappedEventArgs args)
        {
            var delegation = args.Item as Delegation;
            Launcher.OpenAsync(new Uri(delegation.TxExplorerUri));
        }
    }
}
