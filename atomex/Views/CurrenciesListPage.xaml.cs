using Xamarin.Forms;
using atomex.ViewModel;
using System;

namespace atomex
{
    public partial class CurrenciesListPage : ContentPage
    {
        Color selectedCurrencyBackgroundColor;

        public CurrenciesListPage()
        {
            InitializeComponent();
        }

        public CurrenciesListPage(CurrenciesViewModel currenciesViewModel)
        {
            InitializeComponent();
            BindingContext = currenciesViewModel;

            string selectedColorName = "ListViewSelectedBackgroundColor";

            if (Application.Current.RequestedTheme == OSAppTheme.Dark)
                selectedColorName = "ListViewSelectedBackgroundColorDark";

            Application.Current.Resources.TryGetValue(selectedColorName, out var selectedColor);
            selectedCurrencyBackgroundColor = (Color)selectedColor;
        }

        private async void OnCurrencyItemTapped(object sender, EventArgs args)
        {
            Grid selectedCurrency = (Grid)sender;
            selectedCurrency.IsEnabled = false;
            Color initColor = selectedCurrency.BackgroundColor;

            selectedCurrency.BackgroundColor = selectedCurrencyBackgroundColor;

            await selectedCurrency.ScaleTo(1.01, 50);
            await selectedCurrency.ScaleTo(1, 50, Easing.SpringOut);

            selectedCurrency.BackgroundColor = initColor;
            selectedCurrency.IsEnabled = true;
        }
    }
}
