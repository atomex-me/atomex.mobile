using System;

using Xamarin.Forms;
using atomex.ViewModel;

namespace atomex
{
    public partial class ConversionPage : ContentPage
    {
        private ConversionViewModel _conversionViewModel;

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
                await DisplayAlert("Оповещение", "В разработке", "OK");
            }
            else
            {
                await DisplayAlert("Оповещение", "Выберите кошельки для конвертации", "OK");
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
    }
}
