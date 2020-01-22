using Xamarin.Forms;
using atomex.Models;
using atomex.ViewModel;
using Atomex;
using System;
using Atomex.Wallet;
using Atomex.Common;

namespace atomex
{
    public partial class WalletsListPage : ContentPage
    {
        private INavigationService navigationService;
        private TransactionsViewModel TransactionsViewModel;

        public WalletsListPage()
        {
            InitializeComponent();
        }

        public WalletsListPage(IAtomexApp AtomexApp, WalletsViewModel WalletsViewModel, TransactionsViewModel _TransactionsViewModel, INavigationService _navigationService)
        {
            InitializeComponent();

            TransactionsViewModel = _TransactionsViewModel;

            navigationService = _navigationService;

            if (walletsList != null)
            {
                //walletsList.SeparatorVisibility = SeparatorVisibility.None;
                walletsList.ItemsSource = WalletsViewModel.Wallets;
            }
        }

        async void OnListViewItemSelected(object sender, SelectedItemChangedEventArgs e)
        {
            if (e.SelectedItem != null)
            {
                await Navigation.PushAsync(new WalletPage(e.SelectedItem as Wallet, TransactionsViewModel, navigationService));
            }
        }
    }
}
