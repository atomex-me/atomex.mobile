using System;
using Xamarin.Forms;
using atomex.ViewModel;
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
        public CurrencyPage(CurrencyViewModel currencyViewModel, IAtomexApp app, INavigationService navigationService)
        {
            InitializeComponent();
            _navigationService = navigationService;
            _app = app;

            if (currencyViewModel != null)
            {
                _currencyViewModel = currencyViewModel;
                BindingContext = _currencyViewModel;
                if (_currencyViewModel.CurrencyCode == "XTZ")
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
            await Navigation.PushAsync(new SendPage(_currencyViewModel));
        }
        async void ShowDelegatePage(object sender, EventArgs args)
        {
            await Navigation.PushAsync(new DelegatePage(new DelegateViewModel(_app)));
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
        async void SwipeDown(object sender, EventArgs args)
        {
            Loader.IsRunning = Loader.IsVisible = LoaderLabel.IsVisible = true;
            AvailableAmountLabel.TextColor = Color.LightGray;
            await _currencyViewModel.UpdateCurrencyAsync();
            AvailableAmountLabel.TextColor = Color.Black;
            Loader.IsRunning = Loader.IsVisible = LoaderLabel.IsVisible = false;
        }
    }
}