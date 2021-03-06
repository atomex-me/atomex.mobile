﻿using System;
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
            try
            {
                bool.TryParse(await SecureStorage.GetAsync("UseBiometric"), out bool useBiometric);
                if (!useBiometric)
                    return;
            }
            catch (Exception ex)
            {
                Log.Error(ex, AppResources.NotSupportSecureStorage);
                return;
            }
            bool isFingerprintAvailable = await CrossFingerprint.Current.IsAvailableAsync();
            if (isFingerprintAvailable)
            {
                AuthenticationRequestConfiguration conf = new AuthenticationRequestConfiguration(
                        "Authentication",
                        AppResources.UseBiometric + $"'{_unlockViewModel.WalletName}'");

                var authResult = await CrossFingerprint.Current.AuthenticateAsync(conf);
                if (authResult.Authenticated)
                {
                    try
                    {
                        string pswd = await SecureStorage.GetAsync(_unlockViewModel.WalletName);
                        _unlockViewModel.SetPassword(pswd);
                        OnUnlockButtonClicked(this, EventArgs.Empty);
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex, AppResources.NotSupportSecureStorage);
                    }
                }
                else
                {
                    await DisplayAlert(AppResources.SorryLabel, AppResources.NotAuthenticated, AppResources.AcceptButton);
                }
            }
        }
    }
}
