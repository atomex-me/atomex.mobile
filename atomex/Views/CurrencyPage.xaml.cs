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
                    TransactionsListView.RowHeight / 2
                : currencyViewModel.TxsNumberPerPage * TransactionsListView.RowHeight +
                    currencyViewModel.GroupedTransactions.Count * CurrencyViewModel.DefaultGroupHeight +
                    TransactionsListView.RowHeight / 2;
        }

        private void ItemSelected(object sender, SelectedItemChangedEventArgs e)
        {
            if (e.SelectedItem != null)
                ((ListView)sender).SelectedItem = null;
        }

        private void ShowAllTxs(object sender, EventArgs args)
        {
            if (BindingContext is CurrencyViewModel)
            {
                var vm = (CurrencyViewModel)BindingContext;
                vm.ShowAllTxs();
                TransactionsListView.HeightRequest = vm.Transactions.Count * TransactionsListView.RowHeight +
                    vm.GroupedTransactions.Count * CurrencyViewModel.DefaultGroupHeight +
                    TransactionsListView.RowHeight / 2;
            }
        }
    }
}