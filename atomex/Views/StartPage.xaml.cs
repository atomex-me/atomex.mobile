using System;
using atomex.Services;
using atomex.Views.CreateNewWallet;
using Xamarin.Forms;

namespace atomex
{
    public partial class StartPage : ContentPage
    {

        private StartViewModel _startViewModel;
        private CreateNewWalletViewModel _createNewWalletViewModel;
        private INotificationManager notificationManager; // Test

        public StartPage(StartViewModel startViewModel)
        {
            InitializeComponent();
            NavigationPage.SetHasNavigationBar(this, false);
            _startViewModel = startViewModel;
            BindingContext = startViewModel;
            _createNewWalletViewModel = new CreateNewWalletViewModel(startViewModel.AtomexApp);

            notificationManager = DependencyService.Get<INotificationManager>(); // Test
        }
        private async void ShowMyWalletsButtonClicked(object sender, EventArgs args)
        {
            await Navigation.PushAsync(new MyWalletsPage(new MyWalletsViewModel(_startViewModel.AtomexApp)));
        }
        private async void CreateNewWalletButtonClicked(object sender, EventArgs args)
        {
            _createNewWalletViewModel.Clear();
            _createNewWalletViewModel.CurrentAction = CreateNewWalletViewModel.Action.Create;
            await Navigation.PushAsync(new WalletTypePage(_createNewWalletViewModel));
        }
        private async void OnRestoreWalletButtonClicked(object sender, EventArgs args)
        {
            _createNewWalletViewModel.Clear();
            _createNewWalletViewModel.CurrentAction = CreateNewWalletViewModel.Action.Restore;
            await Navigation.PushAsync(new WalletTypePage(_createNewWalletViewModel));
        }


        // Test
        private void OnScheduleClick(object sender, EventArgs e)
        {
            string title = $"Local Notification";
            string message = $"You have now received notifications!";
            notificationManager.ScheduleNotification(title, message);
        }
    }
}
