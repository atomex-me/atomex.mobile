using System;
using atomex.ViewModel;
using atomex.Views.SettingsOptions;
using Xamarin.Forms;

namespace atomex
{
    public partial class SettingsPage : ContentPage
    {
        
        private SettingsViewModel _settingsViewModel;

        public SettingsPage()
        {
            InitializeComponent();
        }

        public SettingsPage(SettingsViewModel settingsViewModel)
        {
            InitializeComponent();
            _settingsViewModel = settingsViewModel;
            BindingContext = settingsViewModel;
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

        private async void OnLogOutButtonClicked(object sender, EventArgs args)
        {
            var res = await DisplayAlert("Log out", "Are you sure?", "Ok", "Cancel");
            if (res)
            {
                _settingsViewModel.LogOut();
                StartViewModel startViewModel = new StartViewModel(_settingsViewModel.AtomexApp);
                Application.Current.MainPage = new NavigationPage(new StartPage(startViewModel));
                ((NavigationPage)Application.Current.MainPage).BarBackgroundColor = Color.FromHex("#2B5286");
                ((NavigationPage)Application.Current.MainPage).BarTextColor = Color.White;
                //todo: check nav stack after logout
            }
        }
    }
}
