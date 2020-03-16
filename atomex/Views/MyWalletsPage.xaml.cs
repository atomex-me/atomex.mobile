using System;

using Xamarin.Forms;

namespace atomex
{
    public partial class MyWalletsPage : ContentPage
    {
        public MyWalletsPage()
        {
            InitializeComponent();
            ((NavigationPage)Application.Current.MainPage).BarBackgroundColor = Color.White;
            //BindingContext = Wallets;
        }

        private void OnWalletClicked(object sender, EventArgs args)
        {
            Content.Opacity = 0.5f;
            Loader.IsRunning = true;
            Application.Current.MainPage = new MainPage();
        }
    }
}
