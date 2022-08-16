using atomex.ViewModels.CurrencyViewModels;
using Xamarin.Forms;

namespace atomex.Views.TezosTokens
{
    public partial class TokenPage : ContentPage
    {
        public TokenPage(TezosTokenViewModel tezosTokenViewModel)
        {
            InitializeComponent();
            BindingContext = tezosTokenViewModel;
        }
    }
}
