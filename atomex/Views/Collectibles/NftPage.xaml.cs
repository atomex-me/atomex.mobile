using atomex.ViewModels.CurrencyViewModels;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace atomex.Views.Collectibles
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class NftPage : ContentPage
    {
        public NftPage(TezosTokenViewModel tezosTokenViewModel)
        {
            InitializeComponent();
            BindingContext = tezosTokenViewModel;
        }
    }
}