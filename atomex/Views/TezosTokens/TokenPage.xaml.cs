using System;
using System.Threading.Tasks;
using atomex.ViewModel;
using Xamarin.Forms;

namespace atomex.Views.TezosTokens
{
    public partial class TokenPage : ContentPage
    {
        Color selectedTxBackgroundColor;

        public TokenPage(TezosTokensViewModel tezosTokensViewModel)
        {
            InitializeComponent();
            BindingContext = tezosTokensViewModel;

            //TransfersListView.IsVisible = false;
            //TokensListView.IsVisible = true;
            //VisualStateManager.GoToState(StatesGrid, "TokensState");

            string selectedColorName = "ListViewSelectedBackgroundColor";

            if (Application.Current.RequestedTheme == OSAppTheme.Dark)
                selectedColorName = "ListViewSelectedBackgroundColorDark";

            Application.Current.Resources.TryGetValue(selectedColorName, out var selectedColor);
            selectedTxBackgroundColor = (Color)selectedColor;
        }

        private void OnTokensButtonClicked(object sender, EventArgs e)
        {
            TransfersListView.IsVisible = false;
            TokensListView.IsVisible = true;
            //VisualStateManager.GoToState(StatesGrid, "TokensState");
        }

        private void OnTransfersButtonClicked(object sender, EventArgs e)
        {
            TokensListView.IsVisible = false;
            TransfersListView.IsVisible = true;
            //VisualStateManager.GoToState(StatesGrid, "TransfersState");
        }

        private async void OnListItemTapped(object sender, EventArgs args)
        {
            Grid selectedItem = (Grid)sender;
            selectedItem.IsEnabled = false;
            Color initColor = selectedItem.BackgroundColor;
            selectedItem.BackgroundColor = selectedTxBackgroundColor;

            await Task.Delay(500);

            selectedItem.BackgroundColor = initColor;
            selectedItem.IsEnabled = true;
        }
    }
}
