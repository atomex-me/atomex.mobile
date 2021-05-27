using Xamarin.Forms;
using atomex.ViewModel;
using System;
using System.Threading.Tasks;

namespace atomex
{
    public partial class CurrencyPage : ContentPage
    {
        Color selectedTxBackgroundColor;

        public CurrencyPage()
        {
            InitializeComponent();
        }
        public CurrencyPage(CurrencyViewModel currencyViewModel)
        {
            InitializeComponent();
            BindingContext = currencyViewModel;

            string selectedColorName = "ListViewSelectedBackgroundColor";

            if (Application.Current.RequestedTheme == OSAppTheme.Dark)
                selectedColorName = "ListViewSelectedBackgroundColorDark";

            Application.Current.Resources.TryGetValue(selectedColorName, out var selectedColor);
            selectedTxBackgroundColor = (Color)selectedColor;
        }

        private async void OnTxItemTapped(object sender, EventArgs args)
        {
            Grid selectedTx = (Grid)sender;
            selectedTx.IsEnabled = false;
            Color initColor = selectedTx.BackgroundColor;
            selectedTx.BackgroundColor = selectedTxBackgroundColor;

            await Task.Delay(500);

            selectedTx.BackgroundColor = initColor;
            selectedTx.IsEnabled = true;
        }
    }
}