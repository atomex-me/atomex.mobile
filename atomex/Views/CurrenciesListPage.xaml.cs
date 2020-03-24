using Xamarin.Forms;
using atomex.ViewModel;

namespace atomex
{
    public partial class CurrenciesListPage : ContentPage
    {
        private INavigationService _navigationService;

        public CurrenciesListPage()
        {
            InitializeComponent();
        }

        public CurrenciesListPage(CurrenciesViewModel CurrenciesViewModel, INavigationService navigationService)
        {
            InitializeComponent();
            _navigationService = navigationService;     
            BindingContext = CurrenciesViewModel;
        }

        async void OnListViewItemTapped(object sender, ItemTappedEventArgs e)
        {
            if (e.Item != null)
            {
                await Navigation.PushAsync(new CurrencyPage(e.Item as CurrencyViewModel, _navigationService));
            }
        }
    }
}
