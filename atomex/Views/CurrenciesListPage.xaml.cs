using Xamarin.Forms;
using atomex.ViewModel.CurrencyViewModels;

namespace atomex
{
    public partial class CurrenciesListPage : ContentPage
    {
        public CurrenciesListPage(CurrenciesViewModel currenciesViewModel)
        {
            InitializeComponent();
            BindingContext = currenciesViewModel;
        }

        private void ItemSelected(object sender, SelectedItemChangedEventArgs e)
        {
            if (e.SelectedItem != null)
                ((ListView)sender).SelectedItem = null;
        }
    }
}
