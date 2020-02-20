using System;
using System.Collections.Generic;
using atomex.ViewModel;
using Atomex;
using Xamarin.Forms;

namespace atomex
{
    public partial class ConversionFirstStepPage : ContentPage
    {
        private IAtomexApp _app;

        private decimal _maxAmount;

        private ConversionViewModel _conversionViewModel;

        public ConversionFirstStepPage()
        {
            InitializeComponent();
        }

        public ConversionFirstStepPage(IAtomexApp app, ConversionViewModel conversionViewModel)
        {
            InitializeComponent();
            _app = app;
            _conversionViewModel = conversionViewModel;
            BindingContext = _conversionViewModel;
            EstimateMaxAmount();
        }

        private async void OnNextButtonClicked(object sender, EventArgs args)
        {
            await Navigation.PushAsync(new ConversionSecondStepPage(_app, _conversionViewModel, _maxAmount));
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
                var (maxAmount, _, _) = await _conversionViewMode?.EstimateMaxAmount();
                _maxAmount = maxAmount;
            }
        }
    }
}
