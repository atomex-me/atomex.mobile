using System;

using Xamarin.Forms;

namespace atomex.Views.CreateNewWallet
{
    public partial class WalletTypePage : ContentPage
    {
        private readonly CreateNewWalletViewModel _createNewWalletViewModel;

        public WalletTypePage()
        {
            InitializeComponent();
        }

        public WalletTypePage(CreateNewWalletViewModel createNewWalletViewModel)
        {
            InitializeComponent();
            _createNewWalletViewModel = createNewWalletViewModel;
            BindingContext = createNewWalletViewModel;
        }

        private async void OnMainNetButtonClicked(object sender, EventArgs args)
        {
            _createNewWalletViewModel.Network = Atomex.Core.Network.MainNet;

            await Navigation.PushAsync(new WalletNamePage(_createNewWalletViewModel));
        }

        private async void OnTestNetButtonClicked(object sender, EventArgs args)
        {
            _createNewWalletViewModel.Network = Atomex.Core.Network.TestNet;

            await Navigation.PushAsync(new WalletNamePage(_createNewWalletViewModel));
        }
    }
}
