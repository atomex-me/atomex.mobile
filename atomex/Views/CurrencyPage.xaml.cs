using System;
using Xamarin.Forms;
using Xamarin.Essentials;
using atomex.ViewModel;

namespace atomex
{
    public partial class CurrencyPage : ContentPage
    {
        private CurrencyViewModel _currencyViewModel;
        private INavigationService _navigationService;

        public CurrencyPage()
        {
            InitializeComponent();
        }
        public CurrencyPage(CurrencyViewModel currencyViewModel, INavigationService navigationService)
        {
            InitializeComponent();

            _navigationService = navigationService;

            if (currencyViewModel != null)
            {
                _currencyViewModel = currencyViewModel;
                BindingContext = _currencyViewModel;
                if (currencyViewModel.Transactions.Count != 0)
                {
                    transactionsList.IsVisible = true;
                    transactionsList.ItemsSource = currencyViewModel.Transactions;
                }
                else
                {
                    transactionsList.IsVisible = false;
                }
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
            await Navigation.PushAsync(new SendPage(_currencyViewModel));
        }
        async void Copy(object sender, EventArgs args)
        {
            if (_currencyViewModel != null)
            {
                await Clipboard.SetTextAsync(_currencyViewModel.Address);
                await DisplayAlert("Адрес скопирован", _currencyViewModel.Address, "OK");
            }
            else
            {
                await DisplayAlert("Ошибка", "Ошибка при копировании!", "OK");
            }
        }
        async void ShowDelegationPage(object sender, EventArgs args)
        {
            if (_currencyViewModel != null)
            {
                await DisplayAlert("Оповещение", "В разработке", "OK");
            }
        }
        void ShowConversionPage(object sender, EventArgs args)
        {
            if (_currencyViewModel != null)
            {
                _navigationService.ShowConversionPage(_currencyViewModel);
            }
        }
    }
}
