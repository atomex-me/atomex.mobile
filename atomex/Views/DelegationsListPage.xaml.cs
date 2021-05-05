using System;
using atomex.ViewModel;
using Xamarin.Forms;

namespace atomex
{
    public partial class DelegationsListPage : ContentPage
    {
        Color selectedItemBackgroundColor;

        public DelegationsListPage()
        {
            InitializeComponent();
        }

        public DelegationsListPage(DelegateViewModel delegateViewModel)
        {
            InitializeComponent();
            BindingContext = delegateViewModel;

            string selectedColorName = "ListViewSelectedBackgroundColor";

            if (Application.Current.RequestedTheme == OSAppTheme.Dark)
                selectedColorName = "ListViewSelectedBackgroundColorDark";

            Application.Current.Resources.TryGetValue(selectedColorName, out var selectedColor);
            selectedItemBackgroundColor = (Color)selectedColor;
        }

        private async void OnItemTapped(object sender, EventArgs args)
        {
            Frame selectedItem = (Frame)sender;
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
