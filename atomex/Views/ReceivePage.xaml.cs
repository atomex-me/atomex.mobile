using System;
using Xamarin.Forms;
using Xamarin.Essentials;
using atomex.ViewModel;
using atomex.Resources;
using atomex.Services;

namespace atomex
{
    public partial class ReceivePage : ContentPage
    {

        private ReceiveViewModel _receiveViewModel;

        private IToastService _toastService;

        public ReceivePage()
        {
            InitializeComponent();
        }

        public ReceivePage(ReceiveViewModel receiveViewModel)
        {
            InitializeComponent();
            _receiveViewModel = receiveViewModel;
            BindingContext = receiveViewModel;
            _toastService = DependencyService.Get<IToastService>();
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
                _toastService?.Show(AppResources.AddressCopied, ToastPosition.Bottom);
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
