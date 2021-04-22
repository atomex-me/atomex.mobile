using System;
using atomex.ViewModel;
using Xamarin.Forms;

namespace atomex
{
    public partial class ConversionsListPage : ContentPage
    {
        Color selectedItemBackgroundColor;

        public ConversionsListPage()
        {
            InitializeComponent();
        }

        public ConversionsListPage(ConversionViewModel conversionViewModel)
        {
            InitializeComponent();
            BindingContext = conversionViewModel;
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
