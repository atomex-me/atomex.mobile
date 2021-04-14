using Xamarin.Forms;
using atomex.ViewModel;
using System;

namespace atomex
{
    public partial class CurrencyPage : ContentPage
    {
        public CurrencyPage()
        {
            InitializeComponent();
        }
        public CurrencyPage(CurrencyViewModel currencyViewModel)
        {
            InitializeComponent();
            BindingContext = currencyViewModel;
        }

        private async void OnTxItemTapped(object sender, EventArgs args)
        {
            Grid selected = (Grid)sender;
            Color initColor = selected.BackgroundColor;

            string selectedColorName = "ListViewSelectedBackgroundColor";

            if (Application.Current.RequestedTheme == OSAppTheme.Dark)
                selectedColorName = "ListViewSelectedBackgroundColorDark";

            Application.Current.Resources.TryGetValue(selectedColorName, out var selectedColor);
            selected.BackgroundColor = (Color)selectedColor;

            await selected.ScaleTo(1.1, 100);
            await selected.ScaleTo(1, 100, Easing.SpringOut);

            selected.BackgroundColor = initColor;
        }
    }
}