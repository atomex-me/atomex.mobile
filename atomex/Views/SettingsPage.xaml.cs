using System;
using atomex.ViewModel;
using Xamarin.Forms;

namespace atomex
{
    public partial class SettingsPage : ContentPage
    {
        
        public SettingsViewModel SettingsViewModel;

        public SettingsPage()
        {
            InitializeComponent();
        }

        public SettingsPage(SettingsViewModel _SettingsViewModel)
        {
            InitializeComponent();
            SettingsViewModel = _SettingsViewModel;
            BindingContext = _SettingsViewModel;
        }

        async void OnBalanceUpdateIntervalSettingTapped(object sender, EventArgs args)
        {
            var optionsPage = new SettingsBalanceUpdateIntervalListOptionsPage(SettingsViewModel, selected =>
            {
                SettingsViewModel.BalanceUpdateIntervalInSec = selected;
            });

            await Navigation.PushAsync(optionsPage);
        }

        async void OnPeriodOfInactiveSettingTapped(object sender, EventArgs args)
        {
            var optionsPage = new SettingsPeriodOfInactiveListOptionsPage(SettingsViewModel, selected =>
            {
                SettingsViewModel.PeriodOfInactivityInMin = selected;
            });

            await Navigation.PushAsync(optionsPage);
        }
    }
}
