using System;
using System.Threading.Tasks;
using atomex.Resources;
using atomex.ViewModel;
using Atomex.Wallet;
using Plugin.Fingerprint;
using Plugin.Fingerprint.Abstractions;
using Serilog;
using Xamarin.Essentials;
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

            BiometricAuth(this, EventArgs.Empty);
        }

        private void PasswordEntryFocused(object sender, FocusEventArgs args)
        {
            PasswordFrame.HasShadow = args.IsFocused;
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
                   
                    await Task.Run(() =>
                    {
                        PasswordHint.FadeTo(1, 500, Easing.Linear);
                    });
                    await Task.Run(() =>
                    {
                        PasswordEntry.TranslateTo(0, 10, 500, Easing.CubicOut);
                    });
                    await Task.Run(() =>
                    {
                        PasswordHint.TranslateTo(0, -20, 500, Easing.CubicOut);
                    });
                }
            }
            else
            {
                await Task.Run(() =>
                {
                    PasswordHint.FadeTo(0, 1000, Easing.Linear);
                });
                await Task.Run(() =>
                {
                    PasswordEntry.TranslateTo(0, 0, 500, Easing.CubicOut);
                });
                await PasswordHint.TranslateTo(0, -10, 500, Easing.CubicOut);
                PasswordHint.IsVisible = false;
            }
            _unlockViewModel.SetPassword(args.NewTextValue);
        }

        private async void OnUnlockButtonClicked(object sender, EventArgs args)
        {
            try
            {
                Content.Opacity = 0.3f;
                Loader.IsRunning = true;
                UnlockButton.IsEnabled = false;

                Account account = null;

                await Task.Run(() =>
                {
                    account = _unlockViewModel.Unlock();
                });

                if (account != null)
                {
                    MainViewModel mainViewModel = null;

                    await Task.Run(() =>
                    {
                        mainViewModel = new MainViewModel(_unlockViewModel.AtomexApp, account, false);
                    });

                    Application.Current.MainPage = new MainPage(mainViewModel);
                }
                else
                {
                    Content.Opacity = 1f;
                    Loader.IsRunning = false;
                    UnlockButton.IsEnabled = true;

                    await DisplayAlert(AppResources.Error, AppResources.InvalidPassword, AppResources.AcceptButton);
                }
            }
            catch (Exception e)
            {
                Log.Error(e, "Unlock error");
            }
        }

        private async void BiometricAuth(object sender, EventArgs e)
        {
            bool isFingerprintAvailable = await CrossFingerprint.Current.IsAvailableAsync(false);
            if (isFingerprintAvailable)
            {
                AuthenticationRequestConfiguration conf = new AuthenticationRequestConfiguration(
                        "Authentication",
                        AppResources.UseFingerprint + $"'{_unlockViewModel.WalletName}'");

                var authResult = await CrossFingerprint.Current.AuthenticateAsync(conf);
                if (authResult.Authenticated)
                {
                    //Success  
                    try
                    {
                        string pswd = await SecureStorage.GetAsync(_unlockViewModel.WalletName);
                        _unlockViewModel.SetPassword(pswd);
                        OnUnlockButtonClicked(this, EventArgs.Empty);
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex, "Device doesn't support secure storage on device");
                        // Possible that device doesn't support secure storage on device.
                    }
                }
            }
        }
    }
}
