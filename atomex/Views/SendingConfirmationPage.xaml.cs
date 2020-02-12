using System;
using Xamarin.Forms;
using atomex.ViewModel;
using Atomex;

namespace atomex
{
    public partial class SendingConfirmationPage : ContentPage
    {
        private CurrencyViewModel _currencyViewModel;

        private IAtomexApp _app;

        private string _to;

        private decimal _amount;

        private decimal _fee;

        private decimal _feePrice;

        private const int BACK_COUNT = 2;

        public SendingConfirmationPage()
        {
            InitializeComponent();
        }

        public SendingConfirmationPage(IAtomexApp app, CurrencyViewModel currencyViewModel, string to, decimal amount, decimal fee)
        {
            InitializeComponent();
            _app = app;
            _currencyViewModel = currencyViewModel;
            _to = to;
            _amount = amount;
            currencyViewModel.Currency.GetDefaultFeePrice();
            AddressFrom.Detail = currencyViewModel.Address;
            AddressTo.Detail = to;
            Amount.Detail = amount.ToString() + " " + currencyViewModel.Name;
            Fee.Detail = fee.ToString() + " " + currencyViewModel.Name;
        }


        async void OnSendButtonClicked(object sender, EventArgs args) {
            try
            {
                BlockActions(true);
                var error = await _app.Account.SendAsync(_currencyViewModel.Name, _to, _amount, _fee, _feePrice);
                if (error != null)
                {
                    BlockActions(false);
                    await DisplayAlert("Оповещение", "Ошибка при отправке транзы", "OK");
                    return;
                }
                var res = await DisplayAlert("Оповещение", _amount + " " + _currencyViewModel.Name + " успешно отправлено на адрес " + _to, null, "Ok");
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
