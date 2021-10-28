using System;
using System.Threading.Tasks;
using atomex.ViewModel;
using Xamarin.Forms;
using ZXing.Client.Result;

namespace atomex.Views.SettingsOptions.Dapps
{
    public partial class DappsPage : ContentPage
    {
        Color selectedItemBackgroundColor;

        public DappsPage()
        {
            InitializeComponent();
        }

        public DappsPage(DappsViewModel dappsViewModel)
        {
            InitializeComponent();
            BindingContext = dappsViewModel;
            string selectedColorName = "ListViewSelectedBackgroundColor";

            Application.Current.Resources.TryGetValue(selectedColorName, out var selectedColor);
            selectedItemBackgroundColor = (Color)selectedColor;
        }

        private async void OnItemTapped(object sender, EventArgs args)
        {
            Grid selectedItem = (Grid)sender;
            selectedItem.IsEnabled = false;
            Color initColor = selectedItem.BackgroundColor;

            selectedItem.BackgroundColor = selectedItemBackgroundColor;

            await Task.Delay(500);

            selectedItem.BackgroundColor = initColor;
            selectedItem.IsEnabled = true;
        }
    }
}