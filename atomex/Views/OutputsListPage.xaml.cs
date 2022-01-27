using System;
using System.Threading.Tasks;
using atomex.ViewModel.SendViewModels;
using Xamarin.Forms;

namespace atomex.Views
{
    public partial class OutputsListPage : ContentPage
    {
        Color selectedItemBackgroundColor;

        public OutputsListPage(SelectOutputsViewModel selectOutputsViewModel)
        {
            InitializeComponent();
            BindingContext = selectOutputsViewModel;

            string selectedColorName = "ListViewSelectedBackgroundColor";

            if (Application.Current.RequestedTheme == OSAppTheme.Dark)
                selectedColorName = "ListViewSelectedBackgroundColorDark";

            Application.Current.Resources.TryGetValue(selectedColorName, out var selectedColor);
            selectedItemBackgroundColor = (Color)selectedColor;
        }

        private async void OnItemTapped(object sender, EventArgs args)
        {
            Grid selectedItem = (Grid)sender;
            selectedItem.IsEnabled = false;
            Color initColor = selectedItem.BackgroundColor;
            selectedItem.BackgroundColor = selectedItemBackgroundColor;

            await Task.Delay(200);

            selectedItem.BackgroundColor = initColor;
            selectedItem.IsEnabled = true;
        }
    }
}
