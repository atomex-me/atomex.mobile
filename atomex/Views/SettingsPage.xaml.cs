using System;
using atomex.Resources;
using atomex.ViewModel;
using atomex.Views.SettingsOptions;
using Xamarin.Essentials;
using Xamarin.Forms;
using System.Globalization;
using atomex.Common;
using System.Linq;

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

        private async void OnLanguagesTapped(object sender, EventArgs args)
        {
            var optionsPage = new LanguagesPage(_settingsViewModel, selected =>
            {
                _settingsViewModel.Language = selected;
            });

            await Navigation.PushAsync(optionsPage);
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

        async void OnWalletTapped(object sender, EventArgs e)
        {
            var walletName = ((TappedEventArgs)e).Parameter.ToString();
            WalletInfo selectedWallet = _settingsViewModel.Wallets.Where(w => w.Name == walletName).Single();

            var confirm = await DisplayAlert(AppResources.DeletingWallet, AppResources.DeletingWalletText, AppResources.UnderstandButton, AppResources.CancelButton);
            if (confirm)
            {
                var confirm2 = await DisplayAlert(AppResources.DeletingWallet, string.Format(CultureInfo.InvariantCulture, AppResources.DeletingWalletConfirmationText, selectedWallet?.Name), AppResources.DeleteButton, AppResources.CancelButton);
                if (confirm2)
                {
                    _settingsViewModel.DeleteWallet(selectedWallet.Path);
                    if (_settingsViewModel.AtomexApp.Account.Wallet.PathToWallet.Equals(selectedWallet.Path))
                        _mainPage._mainViewModel.Locked.Invoke(this, EventArgs.Empty);
                }
            }
        }
    }
}
