using System;

using Xamarin.Forms;

namespace atomex
{
    public partial class StartPage : ContentPage
    {

        private CreateNewWalletViewModel _createNewWalletViewModel;

        public StartPage()
        {
            InitializeComponent();
            _createNewWalletViewModel = new CreateNewWalletViewModel();
        }
        private async void ShowMyWalletsButtonClicked(object sender, EventArgs args)
        {
            await Navigation.PushAsync(new MyWalletsPage());
        }
        private async void CreateNewWalletButtonClicked(object sender, EventArgs args)
        {
            await Navigation.PushAsync(new CreateNewWalletFirstStepPage(_createNewWalletViewModel));
        }
    }
}
