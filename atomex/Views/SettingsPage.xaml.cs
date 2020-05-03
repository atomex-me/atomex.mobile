using System;
using atomex.ViewModel;
using atomex.Views.SettingsOptions;
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

        async void OnBalanceUpdateIntervalSettingTapped(object sender, EventArgs args)
        {
            var optionsPage = new BalanceUpdateIntervalListPage(_settingsViewModel, selected =>
            {
                _settingsViewModel.BalanceUpdateIntervalInSec = selected;
            });

            await Navigation.PushAsync(optionsPage);
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
            var res = await DisplayAlert("Sign out", "Are you sure?", "Ok", "Cancel");
            if (res)
            {
                _mainPage._mainViewModel.Locked.Invoke(this, EventArgs.Empty);
                //todo: check nav stack + threads after logout
            }
        }
    }
}
