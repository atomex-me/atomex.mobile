using System;

using Xamarin.Forms;

namespace atomex
{
    public partial class MyWalletsPage : ContentPage
    {

        private UnlockViewModel _unlockViewModel;

        public MyWalletsPage()
        {
            InitializeComponent();
        }

        public MyWalletsPage(UnlockViewModel unlockViewModel)
        {
            InitializeComponent();
            _unlockViewModel = unlockViewModel;
            //BindingContext = Wallets;
        }

        private async void OnWalletTapped(object sender, EventArgs args)
        {
            Frame walletFrame = sender as Frame;
            walletFrame.HasShadow = true;
            await Navigation.PushAsync(new UnlockWalletPage(_unlockViewModel));
            walletFrame.HasShadow = false;
        }
    }
}
