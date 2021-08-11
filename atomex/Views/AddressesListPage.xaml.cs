using System;
using System.Threading.Tasks;
using atomex.ViewModel;
using atomex.ViewModel.SendViewModels;
using Xamarin.Forms;

namespace atomex.Views
{
    public partial class AddressesListPage : ContentPage
    {
        Color selectedItemBackgroundColor;

        public AddressesListPage(ReceiveViewModel receiveViewModel)
        {
            InitializeComponent();
            BindingContext = receiveViewModel;
            string selectedColorName = "ListViewSelectedBackgroundColor";

            if (Application.Current.RequestedTheme == OSAppTheme.Dark)
                selectedColorName = "ListViewSelectedBackgroundColorDark";

            Application.Current.Resources.TryGetValue(selectedColorName, out var selectedColor);
            selectedItemBackgroundColor = (Color)selectedColor;
        }

        public AddressesListPage(TezosTokensSendViewModel receiveViewModel)
        {
            InitializeComponent();
            BindingContext = receiveViewModel;
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

            await Task.Delay(500);

            selectedItem.BackgroundColor = initColor;
        }
    }
}
