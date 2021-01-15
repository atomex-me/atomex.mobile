using System;
using Xamarin.Forms;
using Xamarin.Essentials;
using atomex.ViewModel;
using atomex.Resources;
using atomex.Services;
using System.Threading.Tasks;

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
                _toastService?.Show(AppResources.AddressCopied, ToastPosition.Top, Application.Current.RequestedTheme.ToString());
            }
            else
            {
                await DisplayAlert(AppResources.Error, AppResources.CopyError, AppResources.AcceptButton);
            }
        }

        async void OnShareButtonClicked(object sender, EventArgs args)
        {
            ShareButton.Opacity = 0.5;
            Loader.IsVisible = Loader.IsRunning = true;
            ShareButton.IsEnabled = false;

            await Share.RequestAsync(new ShareTextRequest
                {
                    Text = AppResources.MyPublicAddress + " " + _receiveViewModel.SelectedAddress.WalletAddress.Currency + ":\r\n" + _receiveViewModel.SelectedAddress.Address,
                    Title = AppResources.AddressSharing
                });

            await Task.Delay(1000);

            Loader.IsVisible = Loader.IsRunning = false;
            ShareButton.Opacity = 1;
            ShareButton.IsEnabled = true;
        }
    }
}
