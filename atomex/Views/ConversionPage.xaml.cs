using System;
using Xamarin.Forms;
using atomex.ViewModel;

namespace atomex
{
    public partial class ConversionPage : ContentPage
    {
        private ConversionViewModel _conversionViewModel;

        private decimal _maxAmount;

        public ConversionPage()
        {
            InitializeComponent();
        }

        public ConversionPage(ConversionViewModel conversionViewModel)
        {
            InitializeComponent();
            _conversionViewModel = conversionViewModel;
            BindingContext = _conversionViewModel;
        }

        private async void OnConvertButtonClicked(object sender, EventArgs args)
        {
            decimal amount = Convert.ToDecimal(Amount.Text);

            if (amount <= 0)
            {
                InvalidAmountFrame.IsVisible = true;
                InvalidAmountLabel.Text = "Amount must be greater than 0 " + _conversionViewModel.FromCurrency.Name;
                return;
            }
            if (amount > _maxAmount)
            {
                InvalidAmountFrame.IsVisible = true;
                InvalidAmountLabel.Text = "Insufficient funds";
                return;
            }
            await DisplayAlert("Warning","In progress","Ok");
        }

        private void OnPickerFromCurrencySelectedIndexChanged(object sender, EventArgs args)
        {
            var picker = sender as Picker;
            int selectedIndex = picker.SelectedIndex;
            if (selectedIndex != -1)
            {
                var wallet = picker.SelectedItem as CurrencyViewModel;
                Amount.Placeholder = "Amount, " + wallet.Name;
                Amount.Text = "";
            }
        }

        private void AmountEntryFocused(object sender, FocusEventArgs e)
        {
            InvalidAmountFrame.IsVisible = false;
        }

        private void AmountEntryUnfocused(object sender, FocusEventArgs e)
        {
            _conversionViewModel.Amount = decimal.Parse(Amount.Text);
        }

        private void OnAmountTextChanged(object sender, TextChangedEventArgs args)
        {
            if (!String.IsNullOrEmpty(args.NewTextValue))
            {
                _conversionViewModel.Amount = decimal.Parse(args.NewTextValue);
            }
            else
            {
                _conversionViewModel.Amount = 0;
            }
        }

        private void OnSetMaxAmountButtonClicked(object sender, EventArgs args)
        {
            InvalidAmountFrame.IsVisible = false;
            EstimateMaxAmount();
            Amount.Text = _maxAmount.ToString();
            _conversionViewModel.Amount = decimal.Parse(Amount.Text);
        }

        async void EstimateMaxAmount()
        {
            var (maxAmount, _, _) = await _conversionViewModel?.EstimateMaxAmount();
            _maxAmount = maxAmount;
        }
    }
}
