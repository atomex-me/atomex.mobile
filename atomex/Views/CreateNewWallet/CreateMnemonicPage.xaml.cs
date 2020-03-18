using System;

using Xamarin.Forms;

namespace atomex
{
    public partial class CreateMnemonicPage : ContentPage
    {

        private CreateNewWalletViewModel _createNewWalletViewModel;

        public CreateMnemonicPage()
        {
            InitializeComponent();
        }

        public CreateMnemonicPage(CreateNewWalletViewModel createNewWalletViewModel)
        {
            InitializeComponent();
            _createNewWalletViewModel = createNewWalletViewModel;
            BindingContext = createNewWalletViewModel;
        }

        private void OnLanguagePickerFocused(object sender, FocusEventArgs args)
        {
            LanguageFrame.HasShadow = args.IsFocused;
        }

        private void OnWordCountPickerFocused(object sender, FocusEventArgs args)
        {
            WordCountFrame.HasShadow = args.IsFocused;
        }

        private async void OnNextButtonClicked(object sender, EventArgs args)
        {
            if (string.IsNullOrEmpty(_createNewWalletViewModel.Mnemonic))
            {
                await DisplayAlert("Warning", "Click Generate button", "Ok");
            }
            else
            {
                await Navigation.PushAsync(new CreateDerivedKeyPasswordPage(_createNewWalletViewModel));
            }
        }

        private void OnGenerateButtonClicked(object sender, EventArgs args)
        {
            MnemonicLabel.IsVisible = false;
            _createNewWalletViewModel.GenerateMnemonic();
        }
    }
}
