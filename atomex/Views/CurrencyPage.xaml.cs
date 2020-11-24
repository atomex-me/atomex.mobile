using System;
using Xamarin.Forms;
using atomex.ViewModel;
using atomex.ViewModel.TransactionViewModels;
using atomex.ViewModel.SendViewModels;
using Atomex;
using atomex.ViewModel.ReceiveViewModels;
using Serilog;
using atomex.Services;
using atomex.Resources;

namespace atomex
{
    public partial class CurrencyPage : ContentPage
    {
        private CurrencyViewModel _currencyViewModel;
        private INavigationService _navigationService { get; }
        private IAtomexApp AtomexApp { get; }
        private IToastService _toastService;

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
            _toastService = DependencyService.Get<IToastService>();
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
                var listView = sender as ListView;
                if (listView != null)
                    listView.SelectedItem = null;
            }
        }

        async void UpdateCurrency(object sender, EventArgs args)
        {
            try
            {
                TxListView.IsRefreshing = RefreshLabel.IsVisible = true;
                AvailableAmountLabel.IsVisible = AvailableAmountInBaseLabel.IsVisible = false;
                if (NoTxsLayout.IsVisible)
                {
                    UpdateButton.IsEnabled = false;
                    UpdateButton.Opacity = 0.5;
                    Loader.IsVisible = Loader.IsRunning = true;
                }
                await _currencyViewModel.UpdateCurrencyAsync();
                if (NoTxsLayout.IsVisible)
                {
                    UpdateButton.IsEnabled = true;
                    UpdateButton.Opacity = 1;
                    Loader.IsVisible = Loader.IsRunning = false;
                }
                TxListView.IsRefreshing = RefreshLabel.IsVisible = false;
                AvailableAmountLabel.IsVisible = AvailableAmountInBaseLabel.IsVisible = true;
                _toastService?.Show(_currencyViewModel.Currency.Description + " " + AppResources.HasBeenUpdated, ToastPosition.Top);
            }
            catch (Exception)
            {
                Log.Error("UpdateCurrencyAsync error");
            }
        }
    }
}