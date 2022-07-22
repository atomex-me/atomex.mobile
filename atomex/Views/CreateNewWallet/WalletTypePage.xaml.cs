using atomex.ViewModels;
using Xamarin.Forms;

namespace atomex.Views.CreateNewWallet
{
    public partial class WalletTypePage : ContentPage
    {

        public WalletTypePage()
        {
            InitializeComponent();
        }

        public WalletTypePage(CreateNewWalletViewModel createNewWalletViewModel)
        {
            InitializeComponent();
            BindingContext = createNewWalletViewModel;
        }
    }
}
