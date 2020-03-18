using System;
using Xamarin.Forms;

namespace atomex
{
    public partial class UnlockWalletPage : ContentPage
    {

        private UnlockViewModel _loginViewModel;
        public UnlockWalletPage()
        {
            InitializeComponent();
        }

        public UnlockWalletPage(UnlockViewModel loginViewModel)
        {
            InitializeComponent();
            _loginViewModel = loginViewModel;
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
            _loginViewModel.SetPassword(args.NewTextValue);
        }

        private void OnLoginButtonClicked(object sender, EventArgs args)
        {
            Content.Opacity = 0.3f;
            Loader.IsRunning = true;
            //Application.Current.MainPage = new MainPage();
        }
    }
}
