using System;
using Xamarin.Forms;
using atomex.ViewModel;
using atomex.ViewModel.TransactionViewModels;
using atomex.ViewModel.SendViewModels;
using Atomex;

namespace atomex
{
    public partial class CurrencyPage : ContentPage
    {
        private CurrencyViewModel _currencyViewModel;
        private INavigationService _navigationService { get; }
        private IAtomexApp AtomexApp { get; }

        public CurrencyPage()
        {
            InitializeComponent();
        }
        public CurrencyPage(CurrencyViewModel currencyViewModel, IAtomexApp atomexApp, INavigationService navigationService)
        {
            InitializeComponent();
            _navigationService = navigationService;
            _currencyViewModel = currencyViewModel;
            AtomexApp = atomexApp;
            BindingContext = _currencyViewModel;
            if (_currencyViewModel.CurrencyCode == "XTZ")
            {
                DelegateButton.IsVisible = true;
            }
        }

        async void ShowReceivePage(object sender, EventArgs args)
        {
            await Navigation.PushAsync(new ReceivePage(_currencyViewModel));
        }
        async void ShowSendPage(object sender, EventArgs args)
        {
            await Navigation.PushAsync(new SendPage(SendViewModelCreator.CreateViewModel(_currencyViewModel)));
        }
        async void ShowDelegationPage(object sender, EventArgs args)
        {
            await Navigation.PushAsync(new DelegationsListPage(new DelegateViewModel(AtomexApp)));
        }
        void ShowConversionPage(object sender, EventArgs args)
        {
            if (_currencyViewModel != null)
            {
                _navigationService.ConvertCurrency(_currencyViewModel.CurrencyCode);
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
            TxListView.IsRefreshing = false;
            Loader.IsRunning = Loader.IsVisible = LoaderLabel.IsVisible = true;
            Color availableAmountColor = AvailableAmountLabel.TextColor;
            Color availableAmountInBaseColor = AvailableAmountInBaseLabel.TextColor;
            AvailableAmountLabel.TextColor = AvailableAmountInBaseLabel.TextColor = Color.LightGray;
            await _currencyViewModel.UpdateCurrencyAsync();
            AvailableAmountLabel.TextColor = availableAmountColor;
            AvailableAmountInBaseLabel.TextColor = availableAmountInBaseColor;
            Loader.IsRunning = Loader.IsVisible = LoaderLabel.IsVisible = false;
        }
    }
}