using Xamarin.Forms;
using atomex.ViewModel.CurrencyViewModels;

namespace atomex
{
    public partial class CurrencyPage : ContentPage
    {
        public CurrencyPage(CurrencyViewModel currencyViewModel)
        {
            InitializeComponent();
            BindingContext = currencyViewModel;

            TransactionsListView.HeightRequest =
                currencyViewModel.Transactions.Count * TransactionsListView.RowHeight +
                currencyViewModel.GroupedTransactions.Count * 0.75 * TransactionsListView.RowHeight;
        }

        private void ItemSelected(object sender, SelectedItemChangedEventArgs e)
        {
            if (e.SelectedItem != null)
                ((ListView)sender).SelectedItem = null;
        }
    }
}