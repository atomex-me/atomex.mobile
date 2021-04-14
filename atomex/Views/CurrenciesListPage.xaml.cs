using Xamarin.Forms;
using atomex.ViewModel;

namespace atomex
{
    public partial class CurrenciesListPage : ContentPage
    {
        public CurrenciesListPage()
        {
            InitializeComponent();
        }

        public CurrenciesListPage(CurrenciesViewModel currenciesViewModel)
        {
            InitializeComponent();
            BindingContext = currenciesViewModel;
        }

        protected override void OnAppearing()
        {
            CurrenciesCollectionView.ClearValue(CollectionView.SelectedItemProperty);
            base.OnAppearing();
        }
    }
}
