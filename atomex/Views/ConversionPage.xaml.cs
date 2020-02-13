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
            
            if (pickerFrom.SelectedIndex != -1 && pickerTo.SelectedIndex != -1)
            {
                await DisplayAlert("Warning", "In progress", "Ok");
            }
            else
            {
                await DisplayAlert("Warning", "Select currencies to convert", "Ok");
            }
            
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
            EstimateMaxAmount(null);
            Amount.Text = _maxAmount.ToString();
        }

        async void EstimateMaxAmount(string address)
        {
            var (maxAmount, _, _) = await _conversionViewModel?.EstimateMaxAmount(address);
            _maxAmount = maxAmount;
        }
    }
}
