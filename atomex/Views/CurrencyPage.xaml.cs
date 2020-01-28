using System;
using Xamarin.Forms;
using Xamarin.Essentials;
using atomex.ViewModel;

namespace atomex
{
    public partial class CurrencyPage : ContentPage
    {
        private CurrencyViewModel _currency;
        private INavigationService _navigationService;

        public CurrencyPage()
        {
            InitializeComponent();
        }
        public CurrencyPage(CurrencyViewModel selectedCurrency, TransactionsViewModel transactionsViewModel, INavigationService navigationService)
        {
            InitializeComponent();

            _navigationService = navigationService;

            if (selectedCurrency != null)
            {
                _currency = selectedCurrency;
                BindingContext = _currency;
                if (transactionsViewModel.GetTransactionsByCurrency(_currency.FullName).Count != 0)
                {
                    transactionsList.IsVisible = true;
                    transactionsList.ItemsSource = transactionsViewModel.GetTransactionsByCurrency(_currency.FullName);
                }
                else
                {
                    transactionsList.IsVisible = false;
                }
                if (_currency.FullName == "Tezos")
                {
                    DelegateOption.IsVisible = true;
                }
            }
        }

        async void ShowReceivePage(object sender, EventArgs args) {
            await Navigation.PushAsync(new ReceivePage(_currency));
        }
        async void ShowSendPage(object sender, EventArgs args)
        {
            await Navigation.PushAsync(new SendPage(_currency));
        }
        async void Copy(object sender, EventArgs args)
        {
            if (_currency != null)
            {
                await Clipboard.SetTextAsync(_currency.Address);
                await DisplayAlert("Адрес скопирован", _currency.Address, "OK");
            }
            else
            {
                await DisplayAlert("Ошибка", "Ошибка при копировании!", "OK");
            }
        }
        async void ShowDelegationPage(object sender, EventArgs args)
        {
            if (_currency != null)
            {
                await DisplayAlert("Оповещение", "В разработке", "OK");
            }
        }
        void ShowConversionPage(object sender, EventArgs args)
        {
            if (_currency != null)
            {
                _navigationService.ShowConversionPage(_currency);
            }
        }
    }
}
