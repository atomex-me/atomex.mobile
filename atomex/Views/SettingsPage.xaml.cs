using System;
using System.Threading.Tasks;
using atomex.ViewModels;
using Xamarin.Forms;

namespace atomex.Views
{
    public partial class SettingsPage : ContentPage
    {
        Color selectedItemBackgroundColor;

        public SettingsPage()
        {
            InitializeComponent();
        }

        public SettingsPage(SettingsViewModel settingsViewModel)
        {
            InitializeComponent();
            string selectedColorName = Application.Current.RequestedTheme == OSAppTheme.Dark
                ? "MainButtonBackgroundColorDark"
                : "ListViewSelectedBackgroundColor";

            Application.Current.Resources.TryGetValue(selectedColorName, out var selectedColor);
            selectedItemBackgroundColor = (Color) selectedColor;

            BindingContext = settingsViewModel;
        }

        private async void OnWalletItemTapped(object sender, EventArgs args)
        {
            StackLayout selectedItem = (StackLayout) sender;
            selectedItem.IsEnabled = false;
            Color initColor = selectedItem.BackgroundColor;

            selectedItem.BackgroundColor = selectedItemBackgroundColor;

            await Task.Delay(100);

            selectedItem.BackgroundColor = initColor;
            selectedItem.IsEnabled = true;
        }

        private async void OnFrameItemTapped(object sender, EventArgs args)
        {
            Frame selectedItem = (Frame) sender;
            selectedItem.IsEnabled = false;
            Color initColor = selectedItem.BackgroundColor;

            selectedItem.BackgroundColor = selectedItemBackgroundColor;

            await Task.Delay(500);

            selectedItem.BackgroundColor = initColor;
            selectedItem.IsEnabled = true;
        }
    }
}