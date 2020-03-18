﻿using System;

using Xamarin.Forms;

namespace atomex
{
    public partial class StartPage : ContentPage
    {

        private CreateNewWalletViewModel _createNewWalletViewModel;
        private UnlockViewModel _unlockViewModel;

        public StartPage()
        {
            InitializeComponent();
            _createNewWalletViewModel = new CreateNewWalletViewModel();
            _unlockViewModel = new UnlockViewModel();
        }
        private async void ShowMyWalletsButtonClicked(object sender, EventArgs args)
        {
            await Navigation.PushAsync(new MyWalletsPage(_unlockViewModel));
        }
        private async void CreateNewWalletButtonClicked(object sender, EventArgs args)
        {
            _createNewWalletViewModel.Clear();
            _createNewWalletViewModel.CurrentAction = CreateNewWalletViewModel.Action.Create;
            await Navigation.PushAsync(new WalletTypePage(_createNewWalletViewModel));
        }
        private async void OnRestoreWalletClicked(object sender, EventArgs args)
        {
            _createNewWalletViewModel.Clear();
            _createNewWalletViewModel.CurrentAction = CreateNewWalletViewModel.Action.Restore;
            await Navigation.PushAsync(new WalletTypePage(_createNewWalletViewModel));
        }
    }
}