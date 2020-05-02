using System;
using atomex.ViewModel;
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
            _createNewWalletViewModel.SetPassword(CreateNewWalletViewModel.PasswordType.StoragePassword, args.NewTextValue);
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
            _createNewWalletViewModel.SetPassword(CreateNewWalletViewModel.PasswordType.StoragePasswordConfirmation, args.NewTextValue);
        }

        private async void OnCreateButtonClicked(object sender, EventArgs args)
        {
            var result = _createNewWalletViewModel.CheckStoragePassword();
            if (result == null)
            {
                var account = await _createNewWalletViewModel.ConnectToWallet();
                if (account != null)
                {
                    Application.Current.MainPage = new MainPage(new MainViewModel(_createNewWalletViewModel.AtomexApp, account));
                }
                else
                {
                    await DisplayAlert("Error", "Create wallet error", "Ok");
                }
            }
            else
            {
                Error.Text = result;
                Error.IsVisible = true;
            }
        }
    }
}
