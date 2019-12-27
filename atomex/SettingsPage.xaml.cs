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

        public SettingsPage(SettingsViewModel settingsViewModel)
        {
            InitializeComponent();
            SettingsViewModel = settingsViewModel;
            BindingContext = settingsViewModel;
        }

        async void OnBalanceUpdateIntervalSettingTapped(object sender, EventArgs args)
        {
            var optionsPage = new SettingsBalanceUpdateIntervalListOptionsPage(SettingsViewModel, selected =>
            {
                SettingsViewModel.BalanceUpdateInterval = selected;
                OnPropertyChanged(nameof(SettingsViewModel.BalanceUpdateInterval));
            });

            await Navigation.PushAsync(optionsPage);
        }

        async void OnPeriodOfInactiveSettingTapped(object sender, EventArgs args)
        {
            var optionsPage = new SettingsPeriodOfInactiveListOptionsPage(SettingsViewModel, selected =>
            {
                SettingsViewModel.PeriodOfInactive = selected;
                OnPropertyChanged(nameof(SettingsViewModel.PeriodOfInactive));
            });

            await Navigation.PushAsync(optionsPage);
        }
    }
}
