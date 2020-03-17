using System;

using Xamarin.Forms;

namespace atomex
{
    public partial class MyWalletsPage : ContentPage
    {

        private LoginViewModel _loginViewModel;

        public MyWalletsPage()
        {
            InitializeComponent();
        }

        public MyWalletsPage(LoginViewModel loginViewModel)
        {
            InitializeComponent();
            _loginViewModel = loginViewModel;
            //((NavigationPage)Application.Current.MainPage).BarBackgroundColor = Color.White;
            //BindingContext = Wallets;
        }

        private async void OnWalletTappeed(object sender, EventArgs args)
        {
            await Navigation.PushAsync(new LoginPage(_loginViewModel));
        }
    }
}
