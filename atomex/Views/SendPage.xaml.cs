using System;
using System.Collections.Generic;

using Xamarin.Forms;
using atomex.Models;
using Xamarin.Essentials;

namespace atomex
{
    public partial class SendPage : ContentPage
    {
        private Wallet wallet;

        public SendPage()
        {
            InitializeComponent();
        }

        public SendPage(Wallet selectedWallet)
        {
            InitializeComponent();
            if (selectedWallet != null)
            {
                wallet = selectedWallet;
                BindingContext = wallet;
            }
        }

        public void EntryFocused(object sender, FocusEventArgs e)
        {
            //e.VisualElement.Focus();

            //var entry = sender as Entry;
            //if (entry != null)
            //{
            //    //entry.
            //}
        }

        void OnSetMaxAmountButtonClicked(object sender, EventArgs args) {
            if (wallet != null)
            {
                Amount.Text = wallet.Amount.ToString();
            }
        }

        async void OnScanButtonClicked(object sender, EventArgs args) {

            var optionsPage = new ScanningQrPage(selected =>
            {
                Address.Text = selected;
            });

            await Navigation.PushAsync(optionsPage);
        }

        async void OnPasteButtonClicked(object sender, EventArgs args) {
            if (Clipboard.HasText)
            {
                var text = await Clipboard.GetTextAsync();
                Address.Text = text;
            }
            else
            {
                await DisplayAlert("Ошибка", "Буфер обмена пуст", "OK");
            }
        }

        async void OnNextButtonClicked(object sender, EventArgs args)
        {
            if (String.IsNullOrWhiteSpace(Address.Text) || String.IsNullOrWhiteSpace(Amount.Text))
            {
                await DisplayAlert("Warning", "Все поля должны быть заполнены", "OK");
            }
            else
            {
                var transaction = new Transaction();
                transaction.To = Address.Text;
                
               
                transaction.Amount = float.Parse(Amount.Text);
                await Navigation.PushAsync(new AcceptSendPage(wallet, transaction));
            }
        }


    }
}
