using System;
using System.Threading.Tasks;
using atomex.ViewModels;
using Xamarin.Forms;

namespace atomex.Views
{
    public partial class MyWalletsPage : ContentPage
    {
        Color selectedItemBackgroundColor;

        public MyWalletsPage()
        {
            InitializeComponent();
        }

        public MyWalletsPage(MyWalletsViewModel myWalletsViewModel)
        {
            InitializeComponent();
            string selectedColorName = Application.Current.RequestedTheme == OSAppTheme.Dark
                ? "ListViewSelectedBackgroundColorDark"
                : "ListViewSelectedBackgroundColor";

            Application.Current.Resources.TryGetValue(selectedColorName, out var selectedColor);
            selectedItemBackgroundColor = (Color) selectedColor;

            BindingContext = myWalletsViewModel;
        }

        private async void OnItemTapped(object sender, EventArgs args)
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