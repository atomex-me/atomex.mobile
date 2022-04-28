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
                ? currencyViewModel.Transactions.Count * TransactionsListView.RowHeight +
                    currencyViewModel.GroupedTransactions.Count * CurrencyViewModel.DefaultGroupHeight +
                    TxsFooter.HeightRequest
                : currencyViewModel.TxsNumberPerPage * TransactionsListView.RowHeight +
                    currencyViewModel.GroupedTransactions.Count * CurrencyViewModel.DefaultGroupHeight +
                    TxsFooter.HeightRequest;
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

        private async void TabChanged(object sender, EventArgs e)
        {
            await TabScrollView.ScrollToAsync((VisualElement)sender, ScrollToPosition.End, true);
        }
    }
}