using System;
using atomex.ViewModel.CurrencyViewModels;
using Xamarin.Forms;

namespace atomex.Views.TezosTokens
{
    public partial class TokenPage : ContentPage
    {
        public TokenPage(TezosTokenViewModel tezosTokenViewModel)
        {
            InitializeComponent();
            BindingContext = tezosTokenViewModel;

            TransactionsListView.HeightRequest = tezosTokenViewModel.IsAllTxsShowed
                ? (tezosTokenViewModel.Transactions.Count + 1) * TransactionsListView.RowHeight +
                    tezosTokenViewModel.GroupedTransactions.Count * CurrencyViewModel.DefaultGroupHeight +
                    TxsFooter.HeightRequest
                : tezosTokenViewModel.TxsNumberPerPage * TransactionsListView.RowHeight +
                    (tezosTokenViewModel.GroupedTransactions.Count + 1) * CurrencyViewModel.DefaultGroupHeight +
                    TxsFooter.HeightRequest;
            AddressesListView.HeightRequest = tezosTokenViewModel.AddressesNumberPerPage * AddressesListView.RowHeight + AddressesFooter.HeightRequest;
        }

        private void ShowAllTxs(object sender, EventArgs args)
        {
            if (BindingContext is TezosTokenViewModel)
            {
                var vm = (TezosTokenViewModel)BindingContext;
                vm.ShowAllTxs();
                TransactionsListView.HeightRequest = vm.Transactions.Count * TransactionsListView.RowHeight +
                    vm.GroupedTransactions.Count * TezosTokenViewModel.DefaultGroupHeight +
                    TxsFooter.HeightRequest;
            }
        }
    }
}
