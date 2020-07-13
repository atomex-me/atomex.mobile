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
    }
}
