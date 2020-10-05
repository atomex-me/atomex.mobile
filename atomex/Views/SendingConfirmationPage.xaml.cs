using System;
using Xamarin.Forms;
using atomex.ViewModel.SendViewModels;
using atomex.Resources;

namespace atomex
{
    public partial class SendingConfirmationPage : ContentPage
    {

        private SendViewModel _sendViewModel;

        private const int BACK_COUNT = 2;

        public SendingConfirmationPage(SendViewModel sendViewModel)
        {
            InitializeComponent();
            _sendViewModel = sendViewModel;
            if (sendViewModel.Currency.FeeCode == "ETH")
            {
                EthereumFeeValue.IsVisible = true;
                FeeValue.IsVisible = false;
            }
            BindingContext = sendViewModel;
        }


        async void OnSendButtonClicked(object sender, EventArgs args) {
            try
            {
                BlockActions(true);

                var error = await _sendViewModel.Send();
                if (error != null)
                {
                    BlockActions(false);
                    await DisplayAlert(AppResources.Error, error, AppResources.AcceptButton);
                    return;
                }
                var res = await DisplayAlert(AppResources.Success, _sendViewModel.Amount + " " + _sendViewModel.CurrencyCode + " " + AppResources.sentTo + " " + _sendViewModel.To, null, AppResources.AcceptButton);
                if (!res)
                {
                    for (var i = 1; i < BACK_COUNT; i++)
                    {
                        Navigation.RemovePage(Navigation.NavigationStack[Navigation.NavigationStack.Count - 2]);
                    }
                    await Navigation.PopAsync();
                }
            }
            catch (Exception e)
            {
                BlockActions(false);
                await DisplayAlert(AppResources.Error, AppResources.SendingTransactionError, AppResources.AcceptButton);
            }
        }

        private void BlockActions(bool flag)
        {
            SendingLoader.IsVisible = SendingLoader.IsRunning = flag;
            SendButton.IsEnabled = !flag;
            if (flag)
            {
                Content.Opacity = 0.5;
            }
            else
            {
                Content.Opacity = 1;
            }
        }
    }
}
