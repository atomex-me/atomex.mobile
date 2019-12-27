using System;

using Xamarin.Forms;
using atomex.ViewModel;
using atomex.Models;

namespace atomex
{
    public partial class ConversionPage : ContentPage
    {
        ConversionViewModel ConversionViewModel;

        public ConversionPage()
        {
            InitializeComponent();
        }

        public ConversionPage(ConversionViewModel _ConversionViewModel)
        {
            InitializeComponent();
            ConversionViewModel = _ConversionViewModel;
            BindingContext = ConversionViewModel;
            Amount.IsEnabled = false;
            Amount.Opacity = 0.5f;
            ConvertBtn.Opacity = 0.7f;
        }

        async void OnConvertButtonClicked(object sender, EventArgs args)
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

        void OnPickerFromCurrencySelectedIndexChanged(object sender, EventArgs args)
        {
            var picker = sender as Picker;
            int selectedIndex = picker.SelectedIndex;
            if (selectedIndex != -1)
            {
                //Console.WriteLine(picker.Items[selectedIndex]);
                Amount.IsEnabled = true;
                Amount.Opacity = 1f;
                var wallet = picker.SelectedItem as Wallet;
                Amount.Placeholder = "Amount, " + wallet.Name;
                if (pickerTo.SelectedIndex != -1)
                {
                    ConvertBtn.Opacity = 1f;
                }
                else
                {
                    ConvertBtn.Opacity = 0.7f;
                }
            }
        }

        void OnPickerToCurrencySelectedIndexChanged(object sender, EventArgs args)
        {
            var picker = sender as Picker;
            int selectedIndex = picker.SelectedIndex;
            if (selectedIndex != -1 && pickerFrom.SelectedIndex != -1)
            {
                ConvertBtn.Opacity = 1f;
            }
        }
    }
}
