using System;
using atomex.ViewModel;
using Atomex;
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
                await DisplayAlert("Error", "You must have more than 0 XTZ", "Ok");
            }
        }
    }
}
