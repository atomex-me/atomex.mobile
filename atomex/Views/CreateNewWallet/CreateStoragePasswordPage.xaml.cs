using System;

using Xamarin.Forms;

namespace atomex.Views.CreateNewWallet
{
    public partial class CreateStoragePasswordPage : ContentPage
    {

        private CreateNewWalletViewModel _createNewWalletViewModel;

        public CreateStoragePasswordPage()
        {
            InitializeComponent();
        }

        public CreateStoragePasswordPage(CreateNewWalletViewModel createNewWalletViewModel)
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
                    PasswordEntry.VerticalTextAlignment = TextAlignment.Start;
                }
            }
            else
            {
                PasswordEntry.VerticalTextAlignment = TextAlignment.Center;
                PasswordHint.IsVisible = false;
            }
            _createNewWalletViewModel.SetPassword("StoragePassword", args.NewTextValue);
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
                    PasswordConfirmationEntry.VerticalTextAlignment = TextAlignment.Start;
                }
            }
            else
            {
                PasswordConfirmationEntry.VerticalTextAlignment = TextAlignment.Center;
                PasswordConfirmationHint.IsVisible = false;
            }
            _createNewWalletViewModel.SetPassword("StoragePasswordConfirmation", args.NewTextValue);
        }

        private void OnCreateButtonClicked(object sender, EventArgs args)
        {
            var result = _createNewWalletViewModel.CheckStoragePassword();
            if (result == null)
            {
                _createNewWalletViewModel.ConnectToWallet();
            }
            else
            {
                Error.Text = result;
                Error.IsVisible = true;
            }
        }
    }
}
