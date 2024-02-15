using atomex.ViewModels;
using Rg.Plugins.Popup.Pages;

namespace atomex.Views.Popup
{
    public partial class TestNetWalletPopup : PopupPage
    {
        public TestNetWalletPopup(MainViewModel mainViewModel)
        {
            InitializeComponent();
            BindingContext = mainViewModel;
        }
    }
}
