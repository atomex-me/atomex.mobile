using System;
using Xamarin.Forms;
using atomex.ViewModel;

namespace atomex
{
    public partial class SendingConfirmationPage : ContentPage
    {
        private CurrencyViewModel _currencyViewModel;

        private string _to;

        private decimal _amount;

        private decimal _fee;

        private decimal _feePrice;

        private const int BACK_COUNT = 2;

        public SendingConfirmationPage()
        {
            InitializeComponent();
        }

        public SendingConfirmationPage(CurrencyViewModel currencyViewModel, string to, decimal amount, decimal fee, decimal feePrice)
        {
            InitializeComponent();
            _currencyViewModel = currencyViewModel;
            _to = to;
            _amount = amount;
            _fee = fee;
            _feePrice = feePrice;
            AddressFrom.Detail = currencyViewModel.FreeExternalAddress;
            AddressTo.Detail = to;
            Amount.Detail = amount.ToString() + " " + currencyViewModel.CurrencyCode;
            Fee.Detail = fee.ToString() + " " + currencyViewModel.CurrencyCode;
        }


        async void OnSendButtonClicked(object sender, EventArgs args) {
            try
            {
                BlockActions(true);
                var error = await _currencyViewModel.SendAsync(_to, _amount, _fee, _feePrice);
                if (error != null)
                {
                    BlockActions(false);
                    await DisplayAlert("Error", "Sending transaction error", "Ok");
                    return;
                }
                var res = await DisplayAlert("Success", _amount + " " + _currencyViewModel.CurrencyCode + " sent to " + _to, null, "Ok");
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
                await DisplayAlert("Error", "An error has occurred while sending transaction", "OK");
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
