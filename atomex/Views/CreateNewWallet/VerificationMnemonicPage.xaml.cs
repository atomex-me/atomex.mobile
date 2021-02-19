using System;
using System.Threading.Tasks;
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

        protected override void OnDisappearing()
        {
            _createNewWalletViewModel.Warning = string.Empty;
            base.OnDisappearing();
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
                _createNewWalletViewModel.VerificateDerivedPassword();
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
            _createNewWalletViewModel.SetPassword(CreateNewWalletViewModel.PasswordType.DerivedPasswordConfirmation, args.NewTextValue);
        }

        private async void OnNextButtonClicked(object sender, EventArgs args)
        {
            _createNewWalletViewModel.CreateHdWallet();
            _createNewWalletViewModel.ClearStoragePswd();
            await Navigation.PushAsync(new CreateStoragePasswordPage(_createNewWalletViewModel));
        }
    }
}
