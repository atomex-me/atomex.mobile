using System;

using Xamarin.Forms;

namespace atomex.Views.CreateNewWallet
{
    public partial class WalletTypePage : ContentPage
    {

        private CreateNewWalletViewModel _createNewWalletViewModel;

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

        private void OnPickerFocused(object sender, EventArgs args)
        {
            Frame.HasShadow = true;
        }

        private void OnPickerUnfocused(object sender, EventArgs args)
        {
            Frame.HasShadow = false;
        }

        private async void OnNextButtonClicked(object sender, EventArgs args)
        {
            await Navigation.PushAsync(new WalletNamePage(_createNewWalletViewModel));
        }
    }
}
