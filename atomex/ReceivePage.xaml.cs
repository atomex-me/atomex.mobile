using System;
using System.Collections.Generic;

using Xamarin.Forms;
using Xamarin.Essentials;
using atomex.Models;

namespace atomex
{
    public partial class ReceivePage : ContentPage
    {
        private Wallet wallet;

        public ReceivePage()
        {
            InitializeComponent();
        }

        public ReceivePage(Wallet selectedWallet)
        {
            InitializeComponent();
            wallet = selectedWallet;
            BindingContext = wallet;
        }

        async void OnCopyButtonClicked(object sender, EventArgs args) {
            if (wallet != null)
            {
                await Clipboard.SetTextAsync(wallet.Address);
                await DisplayAlert("Адрес скопирован", wallet.Address, "OK");
            }
            else
            {
                await DisplayAlert("Ошибка", "Ошибка при копировании", "OK");
            }
        }

        async void OnShareButtonClicked(object sender, EventArgs args)
        {
            await Share.RequestAsync(new ShareTextRequest
            {
                Text = "Мой публичный адрес для получения " + wallet.Name + ":\r\n" + wallet.Address,
                Uri = wallet.Address,
                Title = "Address sharing"
            });
        }
    }
}
