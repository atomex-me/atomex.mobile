using System;
using Xamarin.Forms;
using Xamarin.Essentials;
using atomex.ViewModel;

namespace atomex
{
    public partial class ReceivePage : ContentPage
    {
        private CurrencyViewModel _currency;    

        public ReceivePage()
        {
            InitializeComponent();
        }

        public ReceivePage(CurrencyViewModel currency)
        {
            InitializeComponent();
            _currency = currency;
            BindingContext = currency;
        }

        async void OnCopyButtonClicked(object sender, EventArgs args) {
            if (_currency != null)
            {
                await Clipboard.SetTextAsync(_currency.FreeExternalAddress);
                await DisplayAlert("Address copied", _currency.FreeExternalAddress, "Ok");
            }
            else
            {
                await DisplayAlert("Error", "Copy error", "Ok");
            }
        }

        async void OnShareButtonClicked(object sender, EventArgs args)
        {
            await Share.RequestAsync(new ShareTextRequest
            {
                Text = "My public address to receive " + _currency.CurrencyCode + ":\r\n" + _currency.FreeExternalAddress,
                Uri = _currency.FreeExternalAddress,
                Title = "Address sharing"
            });
        }
    }
}
