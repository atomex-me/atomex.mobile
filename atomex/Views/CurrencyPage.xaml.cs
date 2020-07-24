using System;
using Xamarin.Forms;
using atomex.ViewModel;
using atomex.ViewModel.TransactionViewModels;
using atomex.ViewModel.SendViewModels;
using Atomex;
using atomex.ViewModel.ReceiveViewModels;

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
            await Navigation.PushAsync(new ReceivePage(ReceiveViewModelCreator.CreateViewModel(_currencyViewModel)));
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
                _navigationService.ConvertCurrency(_currencyViewModel.CurrencyCode);
        }
        async void OnListViewItemTapped(object sender, ItemTappedEventArgs e)
        {
            if (e.Item != null)
            {
                await Navigation.PushAsync(new TransactionInfoPage(e.Item as TransactionViewModel));
            }
        }

        async void Refresh(object sender, EventArgs args)
        {
            RefreshLabel.IsVisible = true;
            AvailableAmountLabel.IsVisible = AvailableAmountInBaseLabel.IsVisible = false;
            if (Device.RuntimePlatform == Device.iOS)
            {
                RefreshView.IsRefreshing = true;
                await _currencyViewModel.UpdateCurrencyAsync();
                RefreshView.IsRefreshing = false;
            }
            else if (Device.RuntimePlatform == Device.Android)
            {
                await _currencyViewModel.UpdateCurrencyAsync();
                TxListView.IsRefreshing = false;
            }
            RefreshLabel.IsVisible = false;
            AvailableAmountLabel.IsVisible = AvailableAmountInBaseLabel.IsVisible = true;
        }
    }
}