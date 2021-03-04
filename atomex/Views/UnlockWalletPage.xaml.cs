using System;
using System.Threading.Tasks;
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

            unlockViewModel.BiometricAuth();
        }

        private void PasswordEntryFocused(object sender, FocusEventArgs args)
        {
            PasswordFrame.HasShadow = args.IsFocused;

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

        private void PasswordEntryClicked(object sender, EventArgs args)
        {
            PasswordEntry.Focus();
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
            _unlockViewModel.SetPassword(args.NewTextValue);
        }
    }
}
