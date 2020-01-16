using System;
using atomex.ViewModel;
using Atomex;
using Xamarin.Forms;

namespace atomex
{
    public partial class SettingsPage : ContentPage
    {
        
        public SettingsViewModel SettingsViewModel;

        private IAtomexApp AtomexApp;

        public SettingsPage()
        {
            InitializeComponent();
        }

        public SettingsPage(IAtomexApp _AtomexApp, SettingsViewModel _SettingsViewModel)
        {
            InitializeComponent();
            AtomexApp = _AtomexApp;
            SettingsViewModel = _SettingsViewModel;
            BindingContext = _SettingsViewModel;
        }

        async void OnBalanceUpdateIntervalSettingTapped(object sender, EventArgs args)
        {
            var optionsPage = new SettingsBalanceUpdateIntervalListOptionsPage(SettingsViewModel, selected =>
            {
                SettingsViewModel.BalanceUpdateIntervalInSec = selected;
                AtomexApp.Account.UseUserSettings(SettingsViewModel.Settings);
            });

            await Navigation.PushAsync(optionsPage);
        }

        async void OnPeriodOfInactiveSettingTapped(object sender, EventArgs args)
        {
            var optionsPage = new SettingsPeriodOfInactiveListOptionsPage(SettingsViewModel, selected =>
            {
                SettingsViewModel.PeriodOfInactivityInMin = selected;
                AtomexApp.Account.UseUserSettings(SettingsViewModel.Settings);
            });

            await Navigation.PushAsync(optionsPage);
        }
    }
}
