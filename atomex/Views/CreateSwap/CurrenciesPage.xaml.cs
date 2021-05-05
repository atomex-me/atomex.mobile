using System;
using atomex.ViewModel;
using Xamarin.Forms;

namespace atomex.Views.CreateSwap
{
    public partial class CurrenciesPage : ContentPage
    {
        public CurrenciesPage()
        {
            InitializeComponent();
        }

        public CurrenciesPage(ConversionViewModel conversionViewModel)
        {
            InitializeComponent();
            BindingContext = conversionViewModel;
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
