using System;
using atomex.Views.CreateNewWallet;
using Xamarin.Forms;

namespace atomex
{
    public partial class StartPage : ContentPage
    {
        private StartViewModel _startViewModel;
        private CreateNewWalletViewModel _createNewWalletViewModel;

        public StartPage(StartViewModel startViewModel)
        {
            InitializeComponent();
            NavigationPage.SetHasNavigationBar(this, false);
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
    }
}
