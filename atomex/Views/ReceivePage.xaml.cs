using System;
using Xamarin.Forms;
using Xamarin.Essentials;
using atomex.ViewModel;

namespace atomex
{
    public partial class ReceivePage : ContentPage
    {

        private ReceiveViewModel _receiveViewModel;

        public ReceivePage()
        {
            InitializeComponent();
        }

        public ReceivePage(CurrencyViewModel currencyViewModel)
        {
            InitializeComponent();
            _receiveViewModel = new ReceiveViewModel(currencyViewModel);
            BindingContext = _receiveViewModel;
        }

        private void OnPickerFocused(object sender, FocusEventArgs args)
        {
            AddressFrame.HasShadow = args.IsFocused;
        }

        async void OnCopyButtonClicked(object sender, EventArgs args) {
            if (_receiveViewModel.SelectedAddress != null)
            {
                await Clipboard.SetTextAsync(_receiveViewModel.SelectedAddress.Address);
                await DisplayAlert("Address copied", _receiveViewModel.SelectedAddress.Address, "Ok");
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
                Text = "My public address to receive " + _receiveViewModel.SelectedAddress.WalletAddress.Currency + ":\r\n" + _receiveViewModel.SelectedAddress.Address,
                Uri = _receiveViewModel.SelectedAddress.Address,
                Title = "Address sharing"
            });
        }
    }
}
