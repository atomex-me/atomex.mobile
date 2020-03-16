using System;
using Xamarin.Forms;

namespace atomex
{
    public partial class CreateNewWalletFourthPage : ContentPage
    {

        private CreateNewWalletViewModel _createNewWalletViewModel;

        public CreateNewWalletFourthPage()
        {
            InitializeComponent();
        }

        public CreateNewWalletFourthPage(CreateNewWalletViewModel createNewWalletViewModel)
        {
            InitializeComponent();
            _createNewWalletViewModel = createNewWalletViewModel;
            BindingContext = createNewWalletViewModel;
        }

        private void PasswordEntryFocused(object sender, FocusEventArgs e)
        {
            PasswordFrame.HasShadow = true;
            Error.IsVisible = false;
        }
        private void PasswordEntryUnfocused(object sender, FocusEventArgs e)
        {
            PasswordFrame.HasShadow = false;
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

        private void PasswordConfirmationEntryFocused(object sender, FocusEventArgs e)
        {
            PasswordConfirmationFrame.HasShadow = true;
            Error.IsVisible = false;
        }
        private void PasswordConfirmationEntryUnfocused(object sender, FocusEventArgs e)
        {
            PasswordConfirmationFrame.HasShadow = false;
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
                await Navigation.PushAsync(new CreateNewWalletFifthStepPage(_createNewWalletViewModel));
            }
            else
            {
                Error.Text = result;
                Error.IsVisible = true;
            }
        }
    }
}
