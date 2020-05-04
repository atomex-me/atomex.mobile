using System;
using atomex.Resources;
using atomex.ViewModel;
using Xamarin.Forms;

namespace atomex
{
    public partial class UnlockWalletPage : ContentPage
    {

        private UnlockViewModel _unlockViewModel;
        public UnlockWalletPage()
        {
            InitializeComponent();
        }

        public UnlockWalletPage(UnlockViewModel unlockViewModel)
        {
            InitializeComponent();
            _unlockViewModel = unlockViewModel;
            BindingContext = unlockViewModel;
        }

        private void PasswordEntryFocused(object sender, FocusEventArgs args)
        {
            PasswordFrame.HasShadow = args.IsFocused;
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
            _unlockViewModel.SetPassword(args.NewTextValue);
        }

        private async void OnUnlockButtonClicked(object sender, EventArgs args)
        {
            Content.Opacity = 0.3f;
            Loader.IsRunning = true;

            var account = _unlockViewModel.Unlock();
            if (account != null)
            {
                Application.Current.MainPage = new MainPage(new MainViewModel(_unlockViewModel.AtomexApp, account));
            }
            else
            {
                Content.Opacity = 1f;
                Loader.IsRunning = false;
                await DisplayAlert(AppResources.Error, AppResources.InvalidPassword, AppResources.AcceptButton);
            }
        }
    }
}
