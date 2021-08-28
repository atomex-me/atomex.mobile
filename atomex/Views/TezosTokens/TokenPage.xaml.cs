using System;
using System.Threading.Tasks;
using atomex.ViewModel.CurrencyViewModels;
using Xamarin.Forms;

namespace atomex.Views.TezosTokens
{
    public partial class TokenPage : ContentPage
    {
        Color selectedItemBackgroundColor;

        public TokenPage(TezosTokensViewModel tezosTokensViewModel)
        {
            InitializeComponent();
            BindingContext = tezosTokensViewModel;

            string selectedColorName = "ListViewSelectedBackgroundColor";

            if (Application.Current.RequestedTheme == OSAppTheme.Dark)
                selectedColorName = "ListViewSelectedBackgroundColorDark";

            Application.Current.Resources.TryGetValue(selectedColorName, out var selectedColor);
            selectedItemBackgroundColor = (Color)selectedColor;
        }

        private void OnTokensButtonClicked(object sender, EventArgs e)
        {
            TransfersListView.IsVisible = false;
            TokensListView.IsVisible = true;
        }

        private void OnTransfersButtonClicked(object sender, EventArgs e)
        {
            TokensListView.IsVisible = false;
            TransfersListView.IsVisible = true;
        }

        private async void OnListItemTapped(object sender, EventArgs args)
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
