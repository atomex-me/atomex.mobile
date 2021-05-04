using Xamarin.Forms;
using atomex.ViewModel;
using System;

namespace atomex
{
    public partial class Portfolio : ContentPage
    {
        Color selectedItemBackgroundColor;

        public Portfolio()
        {
            InitializeComponent();
        }

        public Portfolio(PortfolioViewModel portfolioViewModel)
        {
            InitializeComponent();

            string selectedColorName = "ListViewSelectedBackgroundColor";

            if (Application.Current.RequestedTheme == OSAppTheme.Dark)
                selectedColorName = "ListViewSelectedBackgroundColorDark";

            Application.Current.Resources.TryGetValue(selectedColorName, out var selectedColor);
            selectedItemBackgroundColor = (Color)selectedColor;

            BindingContext = portfolioViewModel;
        }

        private async void OnItemTapped(object sender, EventArgs args)
        {
            Grid selectedItem = (Grid)sender;
            selectedItem.IsEnabled = false;
            Color initColor = selectedItem.BackgroundColor;

            selectedItem.BackgroundColor = selectedItemBackgroundColor;

            await selectedItem.ScaleTo(1.01, 50);
            await selectedItem.ScaleTo(1, 50, Easing.SpringOut);

            selectedItem.BackgroundColor = initColor;
            selectedItem.IsEnabled = true;
        }
    }
}