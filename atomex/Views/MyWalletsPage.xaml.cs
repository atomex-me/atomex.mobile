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
            //((NavigationPage)Application.Current.MainPage).BarBackgroundColor = Color.White;
            //BindingContext = Wallets;
        }

        private async void OnWalletTapped(object sender, EventArgs args)
        {
            await Navigation.PushAsync(new UnlockWalletPage(_unlockViewModel));
        }
    }
}
