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
                    PasswordEntry.VerticalTextAlignment = TextAlignment.Start;
                    PasswordHint.IsVisible = true;
                    PasswordHint.Text = PasswordEntry.Placeholder;
                }
            }
            else
            {
                PasswordEntry.VerticalTextAlignment = TextAlignment.Center;
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
