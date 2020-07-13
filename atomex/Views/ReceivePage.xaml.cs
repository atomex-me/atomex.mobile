using System;
using Xamarin.Forms;
using Xamarin.Essentials;
using atomex.ViewModel;
using atomex.Resources;

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

        private void OnPickerClicked(object sender, EventArgs args)
        {
            AddressPicker.Focus();
        }

        async void OnCopyButtonClicked(object sender, EventArgs args) {
            if (_receiveViewModel.SelectedAddress != null)
            {
                await Clipboard.SetTextAsync(_receiveViewModel.SelectedAddress.Address);
                await DisplayAlert(AppResources.AddressCopied, _receiveViewModel.SelectedAddress.Address, AppResources.AcceptButton);
            }
            else
            {
                await DisplayAlert(AppResources.Error, AppResources.CopyError, AppResources.AcceptButton);
            }
        }

        async void OnShareButtonClicked(object sender, EventArgs args)
        {
            await Share.RequestAsync(new ShareTextRequest
            {
                Text = AppResources.MyPublicAddress + " " + _receiveViewModel.SelectedAddress.WalletAddress.Currency + ":\r\n" + _receiveViewModel.SelectedAddress.Address,
                Uri = _receiveViewModel.SelectedAddress.Address,
                Title = AppResources.AddressSharing
            });
        }
    }
}
