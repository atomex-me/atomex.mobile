using atomex.ViewModels.CurrencyViewModels;
using Rg.Plugins.Popup.Pages;
using Xamarin.Forms.Xaml;

namespace atomex.Views.Popup
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class NftDescriptionPopup : PopupPage
    {
        public NftDescriptionPopup(TezosTokenViewModel tezosTokenViewModel)
        {
            InitializeComponent();
            BindingContext = tezosTokenViewModel;
        }
    }
}