using Xamarin.Forms;
using atomex.ViewModel;
using Atomex;

namespace atomex
{
    public partial class CurrenciesListPage : ContentPage
    {
        private INavigationService _navigationService;
        private TransactionsViewModel _transactionsViewModel;

        public CurrenciesListPage()
        {
            InitializeComponent();
        }

        public CurrenciesListPage(IAtomexApp AtomexApp, CurrenciesViewModel CurrenciesViewModel, TransactionsViewModel transactionsViewModel, INavigationService navigationService)
        {
            InitializeComponent();

            _transactionsViewModel = transactionsViewModel;

            _navigationService = navigationService;

            if (currenciesList != null)
            {
                //walletsList.SeparatorVisibility = SeparatorVisibility.None;
                currenciesList.ItemsSource = CurrenciesViewModel.Currencies;
            }
        }

        async void OnListViewItemSelected(object sender, SelectedItemChangedEventArgs e)
        {
            if (e.SelectedItem != null)
            {
                await Navigation.PushAsync(new CurrencyPage(e.SelectedItem as CurrencyViewModel, _transactionsViewModel, _navigationService));
            }
        }
    }
}
