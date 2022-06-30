using Xamarin.Forms;
using atomex.ViewModel.CurrencyViewModels;
using System;

namespace atomex
{
    public partial class CurrencyPage : ContentPage
    {
        public CurrencyPage(CurrencyViewModel currencyViewModel)
        {
            InitializeComponent();
            BindingContext = currencyViewModel;

            TransactionsListView.HeightRequest = currencyViewModel.IsAllTxsShowed
                ? (double)(currencyViewModel?.Transactions?.Count + 1 ?? 0) * TransactionsListView.RowHeight +
                    (currencyViewModel?.GroupedTransactions?.Count + 1 ?? 0) * CurrencyViewModel.DefaultGroupHeight +
                    TxsFooter.HeightRequest
                : currencyViewModel.TxsNumberPerPage * TransactionsListView.RowHeight +
                    (currencyViewModel?.GroupedTransactions?.Count + 1 ?? 0) * CurrencyViewModel.DefaultGroupHeight +
                    TxsFooter.HeightRequest;
            AddressesListView.HeightRequest = currencyViewModel.AddressesNumberPerPage * AddressesListView.RowHeight + AddressesFooter.HeightRequest;

            if (currencyViewModel.IsStakingAvailable)
            {
                var vm = currencyViewModel as TezosCurrencyViewModel;
                DelegationsListView.HeightRequest = (double)(vm.Delegations?.Count + 1 ?? 0) * DelegationsListView.RowHeight;
            }

            if (currencyViewModel.HasTokens)
            {
                var vm = currencyViewModel as TezosCurrencyViewModel;
                TokensListView.HeightRequest = (double)(vm.TezosTokensViewModel?.UserTokens?.Count + 1 ?? 0) * TokensListView.RowHeight +
                    TokensHeader.HeightRequest;
            }
        }

        private void ShowAllTxs(object sender, EventArgs args)
        {
            if (BindingContext is CurrencyViewModel)
            {
                var vm = (CurrencyViewModel)BindingContext;
                vm.ShowAllTxs();
                TransactionsListView.HeightRequest = vm.Transactions.Count * TransactionsListView.RowHeight +
                    vm.GroupedTransactions.Count * CurrencyViewModel.DefaultGroupHeight +
                    TxsFooter.HeightRequest;
            }
        }

        private async void ScrollToCenter(object sender, EventArgs e)
        {
            await TabScrollView.ScrollToAsync((VisualElement)sender, ScrollToPosition.Center, true);
        }
        private async void ScrollToEnd(object sender, EventArgs e)
        {
            await TabScrollView.ScrollToAsync((VisualElement)sender, ScrollToPosition.End, true);
        }
    }
}