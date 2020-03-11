using System;

using Xamarin.Forms;

namespace atomex
{
    public partial class CreateNewWalletFourthPage : ContentPage
    {
        public CreateNewWalletFourthPage()
        {
            InitializeComponent();
        }

        private void PasswordEntryFocused(object sender, FocusEventArgs e)
        {
            PasswordFrame.HasShadow = true;
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
        }

        private void PasswordConfirmationEntryFocused(object sender, FocusEventArgs e)
        {
            PasswordConfirmationFrame.HasShadow = true;
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
        }

        private async void OnNextButtonClicked(object sender, EventArgs args)
        {
            await Navigation.PushAsync(new CreateNewWalletFifthStepPage());
        }
    }
}
