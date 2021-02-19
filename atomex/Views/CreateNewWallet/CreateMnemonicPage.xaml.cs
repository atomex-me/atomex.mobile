using System;
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
                return;
            }
            if (string.IsNullOrEmpty(_createNewWalletViewModel.Mnemonic))
            {
                await DisplayAlert(AppResources.Warning, AppResources.GenerateMnemonic, AppResources.AcceptButton);
                return;
            }
            if (_createNewWalletViewModel.UseDerivedKeyPswd)
            {
                _createNewWalletViewModel.CheckDerivedPassword();

                if (_createNewWalletViewModel.Warning != string.Empty)
                    return;
            }
            _createNewWalletViewModel.ResetMnemonicCollections();
            await Navigation.PushAsync(new VerificationMnemonicPage(_createNewWalletViewModel));
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

        private void PasswordEntryFocused(object sender, FocusEventArgs args)
        {
            PasswordFrame.HasShadow = args.IsFocused;
            _createNewWalletViewModel.Warning = string.Empty;

            if (args.IsFocused)
            {
                Device.StartTimer(TimeSpan.FromSeconds(0.25), () =>
                {
                    Page.ScrollToAsync(0, PasswordEntry.Height, true);
                    return false;
                });
            }
            else
            {
                Device.StartTimer(TimeSpan.FromSeconds(0.25), () =>
                {
                    Page.ScrollToAsync(0, 0, true);
                    return false;
                });
            }
        }

        private async void OnPasswordTextChanged(object sender, TextChangedEventArgs args)
        {
            if (!String.IsNullOrEmpty(args.NewTextValue))
            {
                if (!PasswordHint.IsVisible)
                {
                    PasswordHint.IsVisible = true;
                    PasswordHint.Text = PasswordEntry.Placeholder;

                    _ = PasswordHint.FadeTo(1, 500, Easing.Linear);
                    _ = PasswordEntry.TranslateTo(0, 10, 500, Easing.CubicOut);
                    _ = PasswordHint.TranslateTo(0, -15, 500, Easing.CubicOut);
                }
            }
            else
            {
                await Task.WhenAll(
                    PasswordHint.FadeTo(0, 500, Easing.Linear),
                    PasswordEntry.TranslateTo(0, 0, 500, Easing.CubicOut),
                    PasswordHint.TranslateTo(0, -10, 500, Easing.CubicOut)
                );
                PasswordHint.IsVisible = false;
            }
            _createNewWalletViewModel.SetPassword(CreateNewWalletViewModel.PasswordType.DerivedPassword, args.NewTextValue);
        }

        private void PasswordConfirmationEntryFocused(object sender, FocusEventArgs args)
        {
            PasswordConfirmationFrame.HasShadow = args.IsFocused;
            _createNewWalletViewModel.Warning = string.Empty;

            if (args.IsFocused)
            {
                Device.StartTimer(TimeSpan.FromSeconds(0.25), () =>
                {
                    Page.ScrollToAsync(0, PasswordConfirmationEntry.Height, true);
                    return false;
                });
            }
            else
            {
                Device.StartTimer(TimeSpan.FromSeconds(0.25), () =>
                {
                    Page.ScrollToAsync(0, 0, true);
                    return false;
                });
            }
        }

        private async void OnPasswordConfirmationTextChanged(object sender, TextChangedEventArgs args)
        {
            if (!String.IsNullOrEmpty(args.NewTextValue))
            {
                if (!PasswordConfirmationHint.IsVisible)
                {
                    PasswordConfirmationHint.IsVisible = true;
                    PasswordConfirmationHint.Text = PasswordConfirmationEntry.Placeholder;

                    _ = PasswordConfirmationHint.FadeTo(1, 500, Easing.Linear);
                    _ = PasswordConfirmationEntry.TranslateTo(0, 10, 500, Easing.CubicOut);
                    _ = PasswordConfirmationHint.TranslateTo(0, -15, 500, Easing.CubicOut);
                }
            }
            else
            {
                await Task.WhenAll(
                    PasswordConfirmationHint.FadeTo(0, 500, Easing.Linear),
                    PasswordConfirmationEntry.TranslateTo(0, 0, 500, Easing.CubicOut),
                    PasswordConfirmationHint.TranslateTo(0, -10, 500, Easing.CubicOut)
                );
                PasswordConfirmationHint.IsVisible = false;
            }
            _createNewWalletViewModel.SetPassword(CreateNewWalletViewModel.PasswordType.DerivedPasswordConfirmation, args.NewTextValue);
        }

        private async void OnUseDerivedPswdRowTapped(object sender, EventArgs args)
        {
            await DisplayAlert("", AppResources.DerivedPasswordDescriptionText, AppResources.AcceptButton);
        }

        protected override void OnDisappearing()
        {
            _createNewWalletViewModel.Warning = string.Empty;
            base.OnDisappearing();
        }
    }
}
