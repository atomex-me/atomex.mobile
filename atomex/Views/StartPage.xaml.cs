using System;
using atomex.Models;
using atomex.Resources;
using atomex.Services;
using atomex.Views.CreateNewWallet;
using Xamarin.Forms;

namespace atomex
{
    public partial class StartPage : ContentPage
    {
        private readonly INotificationManager notificationManager;

        private StartViewModel _startViewModel;
        private CreateNewWalletViewModel _createNewWalletViewModel;

        public StartPage(StartViewModel startViewModel)
        {
            InitializeComponent();
            NavigationPage.SetHasNavigationBar(this, false);
            notificationManager = DependencyService.Get<INotificationManager>();
            notificationManager.NotificationReceived += (sender, eventArgs) =>
            {
                ShowNotification((NotificationEventArgs)eventArgs);
            };
            _startViewModel = startViewModel;
            BindingContext = startViewModel;
            _createNewWalletViewModel = new CreateNewWalletViewModel(startViewModel.AtomexApp);
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

        private void ShowNotification(NotificationEventArgs args)
        {
            Device.BeginInvokeOnMainThread(async () =>
            {
                string text = $"Swap completed.\n" +
                    $"Swap ID: {args.SwapId}\n" +
                    $"Currency: {args.Currency}\n" +
                    $"Transaction ID: {args.TxId}\n";
                await DisplayAlert("New swap status", text, AppResources.AcceptButton);
            });
        }
    }
}
