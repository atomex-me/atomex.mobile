using System;
using atomex.ViewModel;
using Xamarin.Forms;

namespace atomex.Views.CreateSwap
{
    public partial class CurrenciesPage : ContentPage
    {

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
        }

        private async void OnNextButtonClicked(object sender, EventArgs args)
        {
            _conversionViewModel.Amount = 0;
            _conversionViewModel.Warning = string.Empty;
            await Navigation.PushAsync(new AmountPage(_conversionViewModel));
        }

        private void OnFromCurrencyPickerFocused(object sender, FocusEventArgs args)
        {
            FromCurrencyFrame.HasShadow = args.IsFocused;
        }

        private void OnToCurrencyPickerFocused(object sender, FocusEventArgs args)
        {
            ToCurrencyFrame.HasShadow = args.IsFocused;
        }

        private void OnFromCurrencyPickerClicked(object sender, EventArgs args)
        {
            PickerFrom.Focus();
        }

        private void OnToCurrencyPickerClicked(object sender, EventArgs args)
        {
            PickerTo.Focus();
        }

        private void OnSwapCurrenciesClicked(object sender, EventArgs args)
        {
            CurrencyViewModel fromVM = _conversionViewModel.FromCurrencyViewModel;
            CurrencyViewModel toVM = _conversionViewModel.ToCurrencyViewModel;
            _conversionViewModel.FromCurrencyViewModel = toVM;
            _conversionViewModel.ToCurrencyViewModel = fromVM;
        }
    }
}
