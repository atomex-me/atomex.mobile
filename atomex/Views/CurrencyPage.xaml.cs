using System;
using Xamarin.Forms;
using Xamarin.Essentials;
using atomex.ViewModel;
using atomex.Views;
using atomex.ViewModel.TransactionViewModels;
using Atomex;

namespace atomex
{
    public partial class CurrencyPage : ContentPage
    {
        private CurrencyViewModel _currencyViewModel;
        private INavigationService _navigationService;
        private IAtomexApp _app;

        public CurrencyPage()
        {
            InitializeComponent();
        }
        public CurrencyPage(IAtomexApp app, CurrencyViewModel currencyViewModel, INavigationService navigationService)
        {
            InitializeComponent();

            _navigationService = navigationService;
            _app = app;

            if (currencyViewModel != null)
            {
                _currencyViewModel = currencyViewModel;
                BindingContext = _currencyViewModel;
                if (_currencyViewModel.FullName == "Tezos")
                {
                    DelegateOption.IsVisible = true;
                }
            }
        }

        async void ShowReceivePage(object sender, EventArgs args) {
            await Navigation.PushAsync(new ReceivePage(_currencyViewModel));
        }
        async void ShowSendPage(object sender, EventArgs args)
        {
            await Navigation.PushAsync(new SendPage(_app, _currencyViewModel));
        }
        async void ShowDelegationPage(object sender, EventArgs args)
        {
            if (_currencyViewModel != null)
            {
                await DisplayAlert("Warning", "In progress", "Ok");
            }
        }
        void ShowConversionPage(object sender, EventArgs args)
        {
            if (_currencyViewModel != null)
            {
                _navigationService.ShowConversionPage(_currencyViewModel);
            }
        }
        async void OnListViewItemTapped(object sender, ItemTappedEventArgs e)
        {
            if (e.Item != null)
            {
                await Navigation.PushAsync(new TransactionInfoPage(e.Item as TransactionViewModel));
            }
        }
    }
}