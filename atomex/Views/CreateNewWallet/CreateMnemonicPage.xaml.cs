﻿using System;
using atomex.Resources;
using Xamarin.Forms;
using System.Threading.Tasks;

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
                LoseMnemonicPhrase.IsVisible = true;
            else
                LoseMnemonicPhrase.IsVisible = false;
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
                _createNewWalletViewModel.GenerateMnemonic();
                MnemonicPhraseFrame.Opacity = 0;
                LoseMnemonicPhrase.IsVisible = true;
                MnemonicPhraseFrame.IsVisible = true;
                await Task.WhenAll(
                    MnemonicPhraseFrame.FadeTo(1, 500, Easing.Linear),
                    LoseMnemonicPhrase.FadeTo(1, 500, Easing.Linear)
                );
            }
            else
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
        }

        private async void MnemonicPhraseChanged(object sender, EventArgs args)
        {
            if (string.IsNullOrEmpty(_createNewWalletViewModel.Mnemonic))
            {
                MnemonicPhraseFrame.IsVisible = false;
                await Task.WhenAll(
                    LoseMnemonicPhrase.FadeTo(0, 500, Easing.Linear)
                );
                LoseMnemonicPhrase.IsVisible = false;
            }
        }
    }
}
