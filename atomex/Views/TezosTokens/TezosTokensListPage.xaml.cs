using Xamarin.Forms;
using atomex.ViewModel.CurrencyViewModels;

namespace atomex.Views.TezosTokens
{
    public partial class TezosTokensListPage : ContentPage
    {
        public TezosTokensListPage(TezosTokensViewModel tezosTokensViewModel)
        {
            InitializeComponent();
            BindingContext = tezosTokensViewModel;
        }

        private void ItemSelected(object sender, SelectedItemChangedEventArgs e)
        {
            if (e.SelectedItem != null)
                ((ListView)sender).SelectedItem = null;
        }
    }
}
