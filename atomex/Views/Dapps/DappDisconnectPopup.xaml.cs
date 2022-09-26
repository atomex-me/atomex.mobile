using atomex.ViewModels.DappsViewModels;
using Rg.Plugins.Popup.Pages;
using Xamarin.Forms.Xaml;

namespace atomex.Views.Dapps
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class DappDisconnectPopup : PopupPage
    {
        public DappDisconnectPopup(DappViewModel dappViewModel)
        {
            InitializeComponent();
            BindingContext = dappViewModel;
        }
    }
}