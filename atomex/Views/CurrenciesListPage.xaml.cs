using Xamarin.Forms;
using atomex.ViewModel;

namespace atomex
{
    public partial class CurrenciesListPage : ContentPage
    {
        private INavigationService _navigationService;
        private DelegateViewModel _delegateViewModel;

        public CurrenciesListPage()
        {
            InitializeComponent();
        }

        public CurrenciesListPage(CurrenciesViewModel currenciesViewModel, DelegateViewModel delegateViewModel, INavigationService navigationService)
        {
            InitializeComponent();
            _navigationService = navigationService;
            _delegateViewModel = delegateViewModel;
            BindingContext = currenciesViewModel;
        }

        async void OnListViewItemTapped(object sender, ItemTappedEventArgs e)
        {
            if (e.Item != null)
            {
                await Navigation.PushAsync(new CurrencyPage(e.Item as CurrencyViewModel, _delegateViewModel, _navigationService));
            }
        }
    }
}
