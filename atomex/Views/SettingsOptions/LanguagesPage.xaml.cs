using System;
using System.Threading.Tasks;
using atomex.ViewModel;
using Xamarin.Forms;

namespace atomex.Views.SettingsOptions
{
    public partial class LanguagesPage : ContentPage
    {
        Color selectedItemBackgroundColor;

        public LanguagesPage(SettingsViewModel settingsViewModel)
        {
            InitializeComponent();
            string selectedColorName = Application.Current.RequestedTheme == OSAppTheme.Dark
                ? "MainButtonBackgroundColorDark"
                : "ListViewSelectedBackgroundColor";

            Application.Current.Resources.TryGetValue(selectedColorName, out var selectedColor);
            selectedItemBackgroundColor = (Color)selectedColor;

            BindingContext = settingsViewModel;
        }

        public LanguagesPage(StartViewModel startViewModel)
        {
            InitializeComponent();
            string selectedColorName = Application.Current.RequestedTheme == OSAppTheme.Dark
                ? "MainButtonBackgroundColorDark"
                : "ListViewSelectedBackgroundColor";

            Application.Current.Resources.TryGetValue(selectedColorName, out var selectedColor);
            selectedItemBackgroundColor = (Color)selectedColor;

            BindingContext = startViewModel;
        }

        private async void OnItemTapped(object sender, EventArgs args)
        {
            StackLayout selectedItem = (StackLayout)sender;
            selectedItem.IsEnabled = false;
            Color initColor = selectedItem.BackgroundColor;

            selectedItem.BackgroundColor = selectedItemBackgroundColor;

            await Task.Delay(500);

            selectedItem.BackgroundColor = initColor;
            selectedItem.IsEnabled = true;
        }
    }
}
