using System;
using Xamarin.Forms;
using atomex.ViewModel;
using Xamarin.Essentials;
using Atomex;

namespace atomex
{
    public partial class SendPage : ContentPage
    {
        private CurrencyViewModel _currencyViewModel;

        private IAtomexApp _app;

        private decimal _maxAmount;

        public SendPage()
        {
            InitializeComponent();
        }

        public SendPage(IAtomexApp app, CurrencyViewModel selectedCurrency)
        {
            InitializeComponent();
            if (selectedCurrency != null)
            {
                _currencyViewModel = selectedCurrency;
                _app = app;
                EstimateMaxAmount();
            }
        }

        private void AmountEntryFocused(object sender, FocusEventArgs e)
        {
            //e.VisualElement.Focus();
            InvalidAmountFrame.IsVisible = false;
        }

        private void AddressEntryFocused(object sender, FocusEventArgs e)
        {
            //e.VisualElement.Focus();
            InvalidAddressFrame.IsVisible = false;
        }

        private void OnSetMaxAmountButtonClicked(object sender, EventArgs args) {
            InvalidAmountFrame.IsVisible = false;
            EstimateMaxAmount();
            Amount.Text = _maxAmount.ToString();
        }

        private async void EstimateMaxAmount()
        {
            var (maxAmount, fee) = await _app.Account
               .EstimateMaxAmountToSendAsync(_currencyViewModel.Name, Address.Text, Atomex.Blockchain.Abstract.BlockchainTransactionType.Output)
               .ConfigureAwait(false);
            _maxAmount = maxAmount;
        }

        private async void OnScanButtonClicked(object sender, EventArgs args) {

            var optionsPage = new ScanningQrPage(selected =>
            {
                Address.Text = selected;
            });

            await Navigation.PushAsync(optionsPage);
        }

        private async void OnPasteButtonClicked(object sender, EventArgs args) {
            if (Clipboard.HasText)
            {
                var text = await Clipboard.GetTextAsync();
                Address.Text = text;
            }
            else
            {
                await DisplayAlert("Ошибка", "Буфер обмена пуст", "OK");
            }
        }

        private async void OnNextButtonClicked(object sender, EventArgs args)
        {
            decimal amount = Convert.ToDecimal(Amount.Text);
            if (String.IsNullOrWhiteSpace(Address.Text) || String.IsNullOrWhiteSpace(Amount.Text))
            {
                await DisplayAlert("Warning", "Все поля должны быть заполнены", "OK");
                return;
            }

            if (!_currencyViewModel.Currency.IsValidAddress(Address.Text))
            {
                InvalidAddressFrame.IsVisible = true;
                return;
            }

            if (amount <= 0)
            {
                InvalidAmountFrame.IsVisible = true;
                InvalidAmountLabel.Text = "Amount must be greater than 0 " + _currencyViewModel.Name;
                return;
            }
            if (amount > _maxAmount)
            {
                InvalidAmountFrame.IsVisible = true;
                InvalidAmountLabel.Text = "Insufficient funds";
                return;
            }
            else
            {
                await Navigation.PushAsync(new AcceptSendPage(_app, _currencyViewModel, Address.Text, amount));
            }
        }
    }
}
