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
    }
}
