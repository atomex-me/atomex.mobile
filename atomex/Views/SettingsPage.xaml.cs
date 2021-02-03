using System;
using System.Threading.Tasks;
using atomex.Resources;
using atomex.ViewModel;
using atomex.Views.SettingsOptions;
using Plugin.Fingerprint;
using Serilog;
using Xamarin.Essentials;
using Xamarin.Forms;
using Plugin.Fingerprint.Abstractions;
using atomex.Views.Popup;
using Rg.Plugins.Popup.Extensions;

namespace atomex
{
    public partial class SettingsPage : ContentPage
    {
        
        private SettingsViewModel _settingsViewModel;

        private MainPage _mainPage;

        public SettingsPage()
        {
            InitializeComponent();
        }

        public SettingsPage(SettingsViewModel settingsViewModel, MainPage mainPage)
        {
            InitializeComponent();
            _settingsViewModel = settingsViewModel;
            BindingContext = settingsViewModel;
            _mainPage = mainPage;

            _ = CheckFingerprintSensor();
        }

        async Task CheckFingerprintSensor()
        {
            var availability = await CrossFingerprint.Current.GetAvailabilityAsync();
            BiometricSetting.IsVisible =
                (availability != FingerprintAvailability.NoSensor) ||
                (availability != FingerprintAvailability.NoApi) ||
                (availability != FingerprintAvailability.Unknown);
        }

        private async void OnUseBiometricToggled(object sender, ToggledEventArgs args)
        {
            if (args.Value)
            {
                var availability = await CrossFingerprint.Current.GetAvailabilityAsync();

                if (availability == FingerprintAvailability.Available)
                {
                    await Navigation.PushPopupAsync(new BiometricSettingPopup(_settingsViewModel));
                }
                if (availability == FingerprintAvailability.NoPermission ||
                    availability == FingerprintAvailability.NoFingerprint)
                {
                    //TODO: + setting button
                    _settingsViewModel.UseBiometric = false;
                    await DisplayAlert("", AppResources.NeedPermissionsForBiometricLogin, AppResources.AcceptButton);
                }
            }
            else
            {
                try
                {
                    _settingsViewModel.UseBiometric = false;
                    await SecureStorage.SetAsync("UseBiometric", false.ToString());
                }
                catch(Exception ex)
                {
                    await DisplayAlert(AppResources.Error,AppResources.NotSupportSecureStorage, AppResources.AcceptButton);
                    Log.Error(ex, AppResources.NotSupportSecureStorage);
                }
            }
        }

        async void OnPeriodOfInactiveSettingTapped(object sender, EventArgs args)
        {
            var optionsPage = new PeriodOfInactiveListPage(_settingsViewModel, selected =>
            {
                _settingsViewModel.PeriodOfInactivityInMin = selected;
            });

            await Navigation.PushAsync(optionsPage);
        }

        private async void OnSignOutButtonClicked(object sender, EventArgs args)
        {
            var res = await DisplayAlert(AppResources.SignOut, AppResources.AreYouSure, AppResources.AcceptButton, AppResources.CancelButton);
            if (res)
            {
                _mainPage._mainViewModel.Locked.Invoke(this, EventArgs.Empty);
            }
        }

        private void OnYoutubeTapped(object sender, EventArgs args)
        {
            Launcher.OpenAsync(new Uri("https://www.youtube.com/c/BakingBad"));
        }

        private void OnTwitterTapped(object sender, EventArgs args)
        {
            Launcher.OpenAsync(new Uri("https://twitter.com/atomex_official"));
        }

        private void OnSupportTapped(object sender, EventArgs args)
        {
            Launcher.OpenAsync(new Uri("mailto:support@atomex.me"));
        }

        private void OnTelegramTapped(object sender, EventArgs args)
        {
            Launcher.OpenAsync(new Uri("https://t.me/atomex_official"));
        }
    }
}
