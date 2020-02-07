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

        public CurrenciesListPage(IAtomexApp AtomexApp, CurrenciesViewModel CurrenciesViewModel, INavigationService navigationService)
        {
            InitializeComponent();
            _navigationService = navigationService;
            _app = AtomexApp;            
            BindingContext = CurrenciesViewModel;
        }

        async void OnListViewItemTapped(object sender, ItemTappedEventArgs e)
        {
            if (e.Item != null)
            {
                await Navigation.PushAsync(new CurrencyPage(_app, e.Item as CurrencyViewModel, _navigationService));
            }
        }
    }
}
