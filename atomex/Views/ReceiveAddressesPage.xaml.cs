using System;
using System.Threading.Tasks;
using atomex.ViewModel;
using Xamarin.Forms;

namespace atomex.Views
{
    public partial class ReceiveAddressesPage : ContentPage
    {
        Color selectedItemBackgroundColor;

        public ReceiveAddressesPage(ReceiveViewModel receiveViewModel)
        {
            InitializeComponent();
            string selectedColorName = "ListViewSelectedBackgroundColor";

            if (Application.Current.RequestedTheme == OSAppTheme.Dark)
                selectedColorName = "ListViewSelectedBackgroundColorDark";

            Application.Current.Resources.TryGetValue(selectedColorName, out var selectedColor);
            selectedItemBackgroundColor = (Color)selectedColor;
        
            BindingContext = receiveViewModel;
            
        }   

        private async void OnItemTapped(object sender, EventArgs args)
        {
            Frame selectedItem = (Frame)sender;
            selectedItem.IsEnabled = false;
            Color initColor = selectedItem.BackgroundColor;
            selectedItem.BackgroundColor = selectedItemBackgroundColor;

            await Task.Delay(500);

            selectedItem.BackgroundColor = initColor;
        }
    }
}
