using System;
using System.Collections.Generic;

using Xamarin.Forms;

namespace atomex
{
    public partial class StartPage : ContentPage
    {
        public StartPage()
        {
            InitializeComponent();
        }
        private async void ShowMyWalletsButtonClicked(object sender, EventArgs args)
        {
            await Navigation.PushAsync(new MyWalletsPage());
        }
        private async void CreateNewWalletButtonClicked(object sender, EventArgs args)
        {
            await Navigation.PushAsync(new CreateNewWalletFirstStepPage());
        }
    }
}
