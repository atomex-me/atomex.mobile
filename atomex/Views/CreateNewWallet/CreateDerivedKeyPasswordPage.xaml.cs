using System;
using Xamarin.Forms;

namespace atomex
{
    public partial class CreateDerivedKeyPasswordPage : ContentPage
    {

        private CreateNewWalletViewModel _createNewWalletViewModel;

        public CreateDerivedKeyPasswordPage()
        {
            InitializeComponent();
        }

        public CreateDerivedKeyPasswordPage(CreateNewWalletViewModel createNewWalletViewModel)
        {
            InitializeComponent();
            _createNewWalletViewModel = createNewWalletViewModel;
            BindingContext = createNewWalletViewModel;
        }

        private void PasswordEntryFocused(object sender, FocusEventArgs args)
        {
            PasswordFrame.HasShadow = args.IsFocused;
            Error.IsVisible = false;
        }

        private void OnPasswordTextChanged(object sender, TextChangedEventArgs args)
        {
            if (!String.IsNullOrEmpty(args.NewTextValue))
            {
                if (!PasswordHint.IsVisible)
                {
                    PasswordHint.IsVisible = true;
                    PasswordHint.Text = PasswordEntry.Placeholder;
                }
            }
            else
            {
                PasswordHint.IsVisible = false;
            }
            _createNewWalletViewModel.SetPassword("DerivedPassword", args.NewTextValue);
        }

        private void PasswordConfirmationEntryFocused(object sender, FocusEventArgs args)
        {
            PasswordConfirmationFrame.HasShadow = args.IsFocused;
            Error.IsVisible = false;
        }

        private void OnPasswordConfirmationTextChanged(object sender, TextChangedEventArgs args)
        {
            if (!String.IsNullOrEmpty(args.NewTextValue))
            {
                if (!PasswordConfirmationHint.IsVisible)
                {
                    PasswordConfirmationHint.IsVisible = true;
                    PasswordConfirmationHint.Text = PasswordConfirmationEntry.Placeholder;
                }
            }
            else
            {
                PasswordConfirmationHint.IsVisible = false;
            }
            _createNewWalletViewModel.SetPassword("DerivedPasswordConfirmation", args.NewTextValue);
        }

        private async void OnNextButtonClicked(object sender, EventArgs args)
        {
            var result = _createNewWalletViewModel.CheckDerivedPassword();
            if (result == null)
            {
                await Navigation.PushAsync(new CreateStoragePasswordPage(_createNewWalletViewModel));
            }
            else
            {
                Error.Text = result;
                Error.IsVisible = true;
            }
        }
    }
}
