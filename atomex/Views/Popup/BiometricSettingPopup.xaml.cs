using System;
using System.IO;
using System.Threading.Tasks;
using atomex.Resources;
using atomex.ViewModel;
using Rg.Plugins.Popup.Extensions;
using Rg.Plugins.Popup.Pages;
using Serilog;
using Xamarin.Essentials;
using Xamarin.Forms;

namespace atomex.Views.Popup
{
    public partial class BiometricSettingPopup : PopupPage
    {
        private SettingsViewModel _settingsViewModel;

        public BiometricSettingPopup(SettingsViewModel settingsViewModel)
        {
            InitializeComponent();

            BindingContext = settingsViewModel;
            _settingsViewModel = settingsViewModel;
        }

        public async void OnCloseButtonClicked(object sender, EventArgs args)
        {
            try
            {
                bool.TryParse(await SecureStorage.GetAsync("UseBiometric"), out bool useBiometric);
                _settingsViewModel.UseBiometric = useBiometric;
            }
            catch(Exception ex)
            {
                await DisplayAlert(AppResources.Error, AppResources.NotSupportSecureStorage, AppResources.AcceptButton);
                Log.Error(ex, AppResources.NotSupportSecureStorage);
            }
            _ = Navigation.PopPopupAsync();
        }

        private async void OnPasswordTextChanged(object sender, TextChangedEventArgs args)
        {
            if (!String.IsNullOrEmpty(args.NewTextValue))
            {
                Error.IsVisible = false;
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
            _settingsViewModel.SetPassword(args.NewTextValue);
        }

        private void PasswordEntryFocused(object sender, FocusEventArgs args)
        {
            PasswordFrame.HasShadow = args.IsFocused;

            if (args.IsFocused)
            {
                Device.StartTimer(TimeSpan.FromSeconds(0.25), () =>
                {
                    Popup.ScrollToAsync(0, PasswordEntry.Height, true);
                    return false;
                });
            }
            else
            {
                Device.StartTimer(TimeSpan.FromSeconds(0.25), () =>
                {
                    Popup.ScrollToAsync(0, 0, true);
                    return false;
                });
            }
        }

        private void PasswordEntryClicked(object sender, EventArgs args)
        {
            PasswordEntry.Focus();
        }

        private async void OnEnableButtonClicked(object sender, EventArgs args)
        {
            BlockActions(true);
            bool accountExist = _settingsViewModel.CheckAccountExist();
            BlockActions(false);
            if (accountExist)
            {
                try
                {
                    string walletName = Path.GetFileName(Path.GetDirectoryName(_settingsViewModel.AtomexApp.Account.Wallet.PathToWallet));
                    await SecureStorage.SetAsync("UseBiometric", true.ToString());
                    await SecureStorage.SetAsync(_settingsViewModel.AtomexApp.Account.Wallet.PathToWallet, PasswordEntry.Text);
                    _settingsViewModel.UseBiometric = true;
                    OnCloseButtonClicked(this, EventArgs.Empty);
                }
                catch (Exception ex)
                {
                    _settingsViewModel.UseBiometric = false;
                    OnCloseButtonClicked(this, EventArgs.Empty);
                    await DisplayAlert(AppResources.Error, AppResources.NotSupportSecureStorage, AppResources.AcceptButton);
                    Log.Error(ex, AppResources.NotSupportSecureStorage);
                }
            }
            else
            {
                _settingsViewModel.UseBiometric = false;
                Error.IsVisible = true;
            }
        }

        private void BlockActions(bool flag)
        {
            Loader.IsVisible = Loader.IsRunning = flag;
            EnableButton.IsEnabled = !flag;
            if (flag)
            {
                Content.Opacity = 0.5;
            }
            else
            {
                Content.Opacity = 1;
            }
        }
    }
}
