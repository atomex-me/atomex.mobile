using System;

using Xamarin.Forms;
using atomex.Models; 

namespace atomex
{
    public partial class AcceptSendPage : ContentPage
    {

        private Transaction currentTransaction;

        public AcceptSendPage()
        {
            InitializeComponent();
        }

        public AcceptSendPage(Wallet selectedWallet, Transaction transaction)
        {
            InitializeComponent();
            if (selectedWallet != null && transaction != null) {
                currentTransaction = transaction;
                FromWallet.Detail = selectedWallet.Address;
                BindingContext = currentTransaction;
            }
        }


        async void OnSendButtonClicked(object sender, EventArgs args) {
            await DisplayAlert("Оповещение", "В разработке", "OK");
        }
    }
}
