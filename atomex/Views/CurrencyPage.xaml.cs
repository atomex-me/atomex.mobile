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
                ? currencyViewModel.Transactions?.Count * TransactionsListView.RowHeight +
                    currencyViewModel.GroupedTransactions?.Count * CurrencyViewModel.DefaultGroupHeight +
                    TransactionsListView.RowHeight / 2 ?? 0
                : currencyViewModel.TxsNumberPerPage * TransactionsListView.RowHeight +
                    currencyViewModel.GroupedTransactions?.Count * CurrencyViewModel.DefaultGroupHeight +
                    TransactionsListView.RowHeight / 2 ?? 0;

            AddressesListView.HeightRequest = currencyViewModel.IsAllAddressesShowed
                ? currencyViewModel.Addresses?.Count * AddressesListView.RowHeight ?? 0
                : currencyViewModel.AddressesNumberPerPage * AddressesListView.RowHeight;
        }

        private void ShowAllTxs(object sender, EventArgs args)
        {
            if (BindingContext is CurrencyViewModel)
            {
                var vm = (CurrencyViewModel)BindingContext;
                vm.ShowAllTxs();
                TransactionsListView.HeightRequest = vm.Transactions?.Count * TransactionsListView.RowHeight +
                    vm.GroupedTransactions?.Count * CurrencyViewModel.DefaultGroupHeight +
                    TransactionsListView.RowHeight / 2 ?? 0;
            }
        }

        private void ShowAllAddresses(object sender, EventArgs args)
        {
            if (BindingContext is CurrencyViewModel)
            {
                var vm = (CurrencyViewModel)BindingContext;
                vm.ShowAllAddresses();
                AddressesListView.HeightRequest = vm.AddressesNumberPerPage * AddressesListView.RowHeight;
            }
        }
    }
}