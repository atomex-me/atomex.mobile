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

        private void OnPickerFocused(object sender, FocusEventArgs args)
        {
            Frame.HasShadow = args.IsFocused;
        }

        private async void OnNextButtonClicked(object sender, EventArgs args)
        {
            await Navigation.PushAsync(new WalletNamePage(_createNewWalletViewModel));
        }
    }
}
