using System;

using Xamarin.Forms;
using atomex.ViewModel; 

namespace atomex
{
    public partial class AcceptSendPage : ContentPage
    {
        public AcceptSendPage()
        {
            InitializeComponent();
        }

        public AcceptSendPage(CurrencyViewModel currencyViewModel, string address, string amount)
        {
            InitializeComponent();
            if (address != null && amount != null) {
                AddressFrom.Detail = currencyViewModel.Address;
                AddressTo.Detail = address;
                Amount.Detail = amount.ToString();
            }
        }


        async void OnSendButtonClicked(object sender, EventArgs args) {
            await DisplayAlert("Оповещение", "В разработке", "OK");
        }
    }
}
