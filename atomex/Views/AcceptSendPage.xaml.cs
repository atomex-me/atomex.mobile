using System;

using Xamarin.Forms;
using atomex.ViewModel;
using Atomex;

namespace atomex
{
    public partial class AcceptSendPage : ContentPage
    {
        private CurrencyViewModel _currencyViewModel;

        private IAtomexApp _app;

        private string _to;

        private decimal _amount;

        private decimal _fee;

        private decimal _feePrice;

        private const int BACK_COUNT = 2;

        public AcceptSendPage()
        {
            InitializeComponent();
        }

        public AcceptSendPage(IAtomexApp app, CurrencyViewModel currencyViewModel, string to, decimal amount)
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
            EstimateFee(to, amount);
        }

        async void EstimateFee(string to, decimal amount)
        {
            var fee = (await _app.Account.EstimateFeeAsync(_currencyViewModel.Name, to, amount, Atomex.Blockchain.Abstract.BlockchainTransactionType.Output));
            _fee = fee ?? 0;
            _feePrice = _currencyViewModel.Currency.GetDefaultFeePrice();
            fee *= _feePrice;
            Fee.Detail = fee.ToString() + " " + _currencyViewModel.Name;
        }

        async void OnSendButtonClicked(object sender, EventArgs args) {
            try
            {
                ShowLoader(true);
                var error = await _app.Account.SendAsync(_currencyViewModel.Name, _to, _amount, _fee, _feePrice);
                if (error != null)
                {
                    ShowLoader(false);
                    await DisplayAlert("Оповещение", "Ошибка при отправке транзы", "OK");
                    return;
                }
                for (var i = 1; i < BACK_COUNT; i++)
                {
                    Navigation.RemovePage(Navigation.NavigationStack[Navigation.NavigationStack.Count - 2]);
                }
                var res = await DisplayAlert("Оповещение", "Транзакция отправлена", null, "Ok");
                if (!res)
                {
                    await Navigation.PopAsync();
                }
            }
            catch (Exception e)
            {
                await DisplayAlert("Error", "An error has occurred while sending transaction", "OK");
                ShowLoader(false);
            }
        }

        private void ShowLoader(bool flag)
        {
            Loader.IsVisible = flag;
            Loader.IsRunning = flag;
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
