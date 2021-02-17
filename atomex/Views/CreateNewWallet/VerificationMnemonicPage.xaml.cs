using System;
using Xamarin.Forms;

namespace atomex.Views.CreateNewWallet
{
    public partial class VerificationMnemonicPage : ContentPage
    {
        private CreateNewWalletViewModel _createNewWalletViewModel;

        public VerificationMnemonicPage()
        {
            InitializeComponent();
        }

        public VerificationMnemonicPage(CreateNewWalletViewModel createNewWalletViewModel)
        {
            InitializeComponent();
            _createNewWalletViewModel = createNewWalletViewModel;
            BindingContext = createNewWalletViewModel;
        }

        void OnSourceWordTapped(object sender, EventArgs args)
        {
            string word = ((TappedEventArgs)args).Parameter.ToString();
            _createNewWalletViewModel.UpdateMnemonicCollections(word, true);
        }

        void OnTargetWordTapped(object sender, EventArgs args)
        {
            var word = ((TappedEventArgs)args).Parameter.ToString();
            _createNewWalletViewModel.UpdateMnemonicCollections(word, false);
        }

        private async void OnNextButtonClicked(object sender, EventArgs args)
        {
            _createNewWalletViewModel.CreateHdWallet();
            _createNewWalletViewModel.ClearStoragePswd();
            await Navigation.PushAsync(new CreateStoragePasswordPage(_createNewWalletViewModel));
        }
    }
}
