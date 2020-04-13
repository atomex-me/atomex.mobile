using Xamarin.Forms;
using atomex.ViewModel;
using Atomex;

namespace atomex
{
    public partial class CurrenciesListPage : ContentPage
    {
        private INavigationService _navigationService;
        private IAtomexApp _app;

        public CurrenciesListPage()
        {
            InitializeComponent();
        }

        public CurrenciesListPage(CurrenciesViewModel currenciesViewModel, IAtomexApp app, INavigationService navigationService)
        {
            InitializeComponent();
            _navigationService = navigationService;
            _app = app;
            BindingContext = currenciesViewModel;
        }

        async void OnListViewItemTapped(object sender, ItemTappedEventArgs e)
        {
            if (e.Item != null)
            {
                await Navigation.PushAsync(new CurrencyPage(e.Item as CurrencyViewModel, _app, _navigationService));
            }
        }
    }
}
