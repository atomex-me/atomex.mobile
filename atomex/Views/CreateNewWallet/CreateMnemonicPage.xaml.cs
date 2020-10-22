using System;
using atomex.Resources;
using Xamarin.Forms;

namespace atomex.Views.CreateNewWallet
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
            if (!string.IsNullOrEmpty(createNewWalletViewModel.Mnemonic))
            {
                HighlightMnemonicPhrase();
            }
            else
            {
                MnemonicLabel.IsVisible = true;
                LoseMnemonicPhraseText.IsVisible = false;
            }
        }

        private void OnLanguagePickerFocused(object sender, FocusEventArgs args)
        {
            LanguageFrame.HasShadow = args.IsFocused;
        }

        private void OnLanguagePickerClicked(object sender, EventArgs args)
        {
            LanguagePicker.Focus();
        }

        private void OnWordCountPickerFocused(object sender, FocusEventArgs args)
        {
            WordCountFrame.HasShadow = args.IsFocused;
        }

        private void OnWordCountPickerClicked(object sender, EventArgs args)
        {
            WordCountPicker.Focus();
        }

        private async void OnNextButtonClicked(object sender, EventArgs args)
        {
            if (string.IsNullOrEmpty(_createNewWalletViewModel.Mnemonic))
            {
                await DisplayAlert(AppResources.Warning, AppResources.GenerateMnemonic, AppResources.AcceptButton);
            }
            else
            {
                await Navigation.PushAsync(new CreateDerivedKeyPasswordPage(_createNewWalletViewModel));
            }
        }

        private void OnGenerateButtonClicked(object sender, EventArgs args)
        {
            HighlightMnemonicPhrase();
            _createNewWalletViewModel.GenerateMnemonic();
        }

        private void MnemonicPhraseChanged(object sender, EventArgs args)
        {
            DeleteMnemonicPhrase();
        }

        private void DeleteMnemonicPhrase()
        {
            MnemonicLabel.IsVisible = true;
            LoseMnemonicPhraseText.IsVisible = false;
            if (Application.Current.Resources.TryGetValue("DefaultFrameBackgroundColor", out var bgColor))
                MnemonicPhraseFrame.BackgroundColor = (Color)bgColor;
        }

        private void HighlightMnemonicPhrase()
        {
            MnemonicLabel.IsVisible = false;
            LoseMnemonicPhraseText.IsVisible = true;
            if (Application.Current.Resources.TryGetValue("ErrorFrameBackgroundColor", out var bgColor))
                MnemonicPhraseFrame.BackgroundColor = (Color)bgColor;
        }
    }
}
