using System;
using atomex.Models;
using atomex.Resources;
using atomex.ViewModel;
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
        private async void OnDelegationTapped(object sender, ItemTappedEventArgs args)
        {
            var delegation = args.Item as Delegation;
            if (delegation.Baker == null)
                await Navigation.PushAsync(new DelegatePage(_delegateViewModel, delegation.Address));
            else
                await Navigation.PushAsync(new DelegationInfoPage(_delegateViewModel, delegation));
        }
    }
}
