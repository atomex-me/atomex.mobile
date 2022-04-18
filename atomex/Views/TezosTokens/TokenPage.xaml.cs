using System;
using atomex.ViewModel.CurrencyViewModels;
using Xamarin.Forms;

namespace atomex.Views.TezosTokens
{
    public partial class TokenPage : ContentPage
    {
        public TokenPage(TezosTokensViewModel tezosTokensViewModel)
        {
            InitializeComponent();
            BindingContext = tezosTokensViewModel;
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

        private void ItemSelected(object sender, SelectedItemChangedEventArgs e)
        {
            if (e.SelectedItem != null)
                ((ListView)sender).SelectedItem = null;
        }
    }
}
