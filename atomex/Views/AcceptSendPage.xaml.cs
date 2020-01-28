using System;

using Xamarin.Forms;
using atomex.ViewModel; 

namespace atomex
{
    public partial class AcceptSendPage : ContentPage
    {

        private Transaction _currentTransaction;

        public AcceptSendPage()
        {
            InitializeComponent();
        }

        public AcceptSendPage(CurrencyViewModel selectedCurrency, Transaction transaction)
        {
            InitializeComponent();
            if (selectedCurrency != null && transaction != null) {
                _currentTransaction = transaction;
                FromWallet.Detail = selectedCurrency.Address;
                BindingContext = _currentTransaction;
            }
        }


        async void OnSendButtonClicked(object sender, EventArgs args) {
            await DisplayAlert("Оповещение", "В разработке", "OK");
        }
    }
}
