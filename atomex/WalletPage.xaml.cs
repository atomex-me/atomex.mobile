using System;
using Xamarin.Forms;
using atomex.Models;
using Xamarin.Essentials;
using atomex.ViewModel;

namespace atomex
{
    public partial class WalletPage : ContentPage
    {
        private readonly Wallet wallet;
        private INavigationService _navigationService;

        public WalletPage()
        {
            InitializeComponent();
        }
        public WalletPage(Wallet selectedWallet, TransactionsViewModel TransactionsViewModel, INavigationService navigationService)
        {
            InitializeComponent();

            _navigationService = navigationService;

            if (selectedWallet != null)
            {
                wallet = selectedWallet;
                BindingContext = wallet;
                transactionsList.ItemsSource = TransactionsViewModel.GetSortedList();
            }
        }

        async void ShowReceivePage(object sender, EventArgs args) {
            await Navigation.PushAsync(new ReceivePage(wallet));
        }
        async void ShowSendPage(object sender, EventArgs args)
        {
            await Navigation.PushAsync(new SendPage(wallet));
        }
        async void Copy(object sender, EventArgs args)
        {
            if (wallet != null)
            {
                await Clipboard.SetTextAsync(wallet.Address);
                await DisplayAlert("Адрес скопирован", wallet.Address, "OK");
            }
            else
            {
                await DisplayAlert("Ошибка", "Ошибка при копировании!", "OK");
            }
        }
        void ShowConversionPage(object sender, EventArgs args)
        {
            if (wallet != null)
            {
                _navigationService.ShowConversionPage(wallet);
            }
        }
    }
}
