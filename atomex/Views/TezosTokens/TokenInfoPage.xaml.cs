using Xamarin.Forms;
using atomex.ViewModel.CurrencyViewModels;

namespace atomex.Views.TezosTokens
{
    public partial class TokenInfoPage : ContentPage
    {
        public TokenInfoPage(TezosTokenViewModel tezosTokenViewModel)
        {
            InitializeComponent();
            BindingContext = tezosTokenViewModel;
        }
    }
}
