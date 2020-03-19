using System;
using atomex.ViewModel;
using Xamarin.Forms;

namespace atomex.Views.CreateSwap
{
    public partial class CurrenciesPage : ContentPage
    {
        private decimal _maxAmount;

        private ConversionViewModel _conversionViewModel;

        public CurrenciesPage()
        {
            InitializeComponent();
        }

        public CurrenciesPage(ConversionViewModel conversionViewModel)
        {
            InitializeComponent();
            _conversionViewModel = conversionViewModel;
            BindingContext = _conversionViewModel;
            EstimateMaxAmount();
        }

        private async void OnNextButtonClicked(object sender, EventArgs args)
        {
            await Navigation.PushAsync(new AmountPage(_conversionViewModel, _maxAmount));
        }

        private void OnPickerFromCurrencySelectedIndexChanged(object sender, EventArgs args)
        {
            var picker = sender as Picker;
            int selectedIndex = picker.SelectedIndex;
            if (selectedIndex != -1)
            {
                EstimateMaxAmount();
            }
        }

        private async void EstimateMaxAmount()
        {
            if (_conversionViewModel.FromCurrency != null)
            {
                var (maxAmount, _, _) = await _conversionViewModel.EstimateMaxAmount();
                _maxAmount = maxAmount;
            }
        }

        private void OnFromCurrencyPickerFocused(object sender, FocusEventArgs args)
        {
            FromCurrencyFrame.HasShadow = args.IsFocused;
        }

        private void OnToCurrencyPickerFocused(object sender, FocusEventArgs args)
        {
            ToCurrencyFrame.HasShadow = args.IsFocused;
        }
    }
}
