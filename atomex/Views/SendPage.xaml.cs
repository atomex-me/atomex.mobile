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

        private decimal _fee;

        private decimal _feePrice;

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
                Amount.Placeholder = "Amount, " + _currencyViewModel.Name;
                EstimateMaxAmount(null);
            }
        }

        private void AmountEntryFocused(object sender, FocusEventArgs e)
        {
            AmountFrame.HasShadow = true;
            InvalidAmountFrame.IsVisible = false;
        }

        private void AmountEntryUnfocused(object sender, FocusEventArgs e)
        {
            AmountFrame.HasShadow = false;
            decimal amount;
            if (String.IsNullOrEmpty(Amount?.Text) || String.IsNullOrWhiteSpace(Amount?.Text))
                amount = 0;
            else
                amount = Convert.ToDecimal(Amount?.Text);
            EstimateFee(Address?.Text, amount);
        }

        private void AddressEntryFocused(object sender, FocusEventArgs e)
        {
            AddressFrame.HasShadow = true;
            InvalidAddressFrame.IsVisible = false;
        }
        private void AddressEntryUnfocused(object sender, FocusEventArgs e)
        {
            AddressFrame.HasShadow = false;
        }

        private void OnAmountTextChanged(object sender, TextChangedEventArgs args)
        {
            if (!String.IsNullOrEmpty(args.NewTextValue))
            {
                if (!AmountHint.IsVisible)
                {
                    AmountHint.IsVisible = true;
                    AmountHint.Text = Amount.Placeholder;
                }
            }
            else
            {
                AmountHint.IsVisible = false;
            }
        }

        private void OnAddressTextChanged(object sender, TextChangedEventArgs args)
        {
            if (!String.IsNullOrEmpty(args.NewTextValue))
            {
                if (!AddressHint.IsVisible)
                {
                    AddressHint.IsVisible = true;
                    AddressHint.Text = Address.Placeholder;
                }
            }
            else
            {
                AddressHint.IsVisible = false;
            }
        }

        async void EstimateFee(string to, decimal amount)
        {
            ShowFeeLoader(true);
            var fee = (await _app.Account.EstimateFeeAsync(_currencyViewModel.Name, to, amount, Atomex.Blockchain.Abstract.BlockchainTransactionType.Output));
            _fee = fee ?? 0;
            _feePrice = _currencyViewModel.Currency.GetDefaultFeePrice();
            fee = _fee * _feePrice;
            FeeValue.Text = fee.ToString() + " " + _currencyViewModel.Name;
            ShowFeeLoader(false);
        }

        async void EstimateMaxAmount(string address)
        {
            var (maxAmount, _, _) = await _currencyViewModel?.EstimateMaxAmount(address);
            _maxAmount = maxAmount;
        }

        private void ShowFeeLoader(bool flag)
        {
            EstimateFeeLayout.IsVisible = FeeLoader.IsRunning = flag;
            NextButton.IsEnabled = FeeLabel.IsVisible = !flag;
        }

        private void OnSetMaxAmountButtonClicked(object sender, EventArgs args) {
            InvalidAmountFrame.IsVisible = false;
            EstimateMaxAmount(Address.Text);
            Amount.Text = _maxAmount.ToString();
            EstimateFee(Address.Text, _maxAmount);
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
                await DisplayAlert("Error", "Clipboard is empty", "Ok");
            }
        }

        private async void OnNextButtonClicked(object sender, EventArgs args)
        {
            if (String.IsNullOrWhiteSpace(Address.Text) || String.IsNullOrWhiteSpace(Amount.Text))
            {
                await DisplayAlert("Warning", "All fields must be filled", "Ok");
                return;
            }

            if (!_currencyViewModel.Currency.IsValidAddress(Address.Text))
            {
                InvalidAddressFrame.IsVisible = true;
                return;
            }

            decimal amount = Convert.ToDecimal(Amount.Text);

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
            await Navigation.PushAsync(new SendingConfirmationPage(_app, _currencyViewModel, Address.Text, amount, _fee, _feePrice));
        }
    }
}
