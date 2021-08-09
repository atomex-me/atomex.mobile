using System;
using System.Threading.Tasks;
using Xamarin.Forms;
using atomex.ViewModel.CurrencyViewModels;

namespace atomex.Views.TezosTokens
{
    public partial class TezosTokensListPage : ContentPage
    {

        Color selectedItemBackgroundColor;

        public TezosTokensListPage(TezosTokensViewModel tezosTokensViewModel)
        {
            InitializeComponent();

            BindingContext = tezosTokensViewModel;

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

            await Task.Delay(500);

            selectedItem.BackgroundColor = initColor;
            selectedItem.IsEnabled = true;
        }
    }
}
