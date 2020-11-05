using System;
using atomex.Resources;
using atomex.ViewModel;
using atomex.Views.SettingsOptions;
using Xamarin.Essentials;
using Xamarin.Forms;

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
            Launcher.OpenAsync(new Uri("https://t.me/atomex_official"));
        }

        private void OnTelegramTapped(object sender, EventArgs args)
        {
            Launcher.OpenAsync(new Uri("https://t.me/atomex_official"));
        }
    }
}
