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
                ? (double)(tezosTokenViewModel?.Transactions?.Count + 1 ?? 0) * TransactionsListView.RowHeight +
                    (tezosTokenViewModel?.GroupedTransactions?.Count + 1 ?? 0) * TezosTokenViewModel.DefaultGroupHeight +
                    TxsFooter.HeightRequest
                : tezosTokenViewModel.TxsNumberPerPage * TransactionsListView.RowHeight +
                    tezosTokenViewModel.TxsNumberPerPage * TezosTokenViewModel.DefaultGroupHeight +
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
