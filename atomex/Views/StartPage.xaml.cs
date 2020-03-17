using System;

using Xamarin.Forms;

namespace atomex
{
    public partial class StartPage : ContentPage
    {

        private CreateNewWalletViewModel _createNewWalletViewModel;
        private LoginViewModel _loginViewModel;

        public StartPage()
        {
            InitializeComponent();
            _createNewWalletViewModel = new CreateNewWalletViewModel();
            _loginViewModel = new LoginViewModel();
        }
        private async void ShowMyWalletsButtonClicked(object sender, EventArgs args)
        {
            await Navigation.PushAsync(new MyWalletsPage(_loginViewModel));
        }
        private async void CreateNewWalletButtonClicked(object sender, EventArgs args)
        {
            await Navigation.PushAsync(new CreateNewWalletFirstStepPage(_createNewWalletViewModel));
        }

        void Label_Focused(System.Object sender, Xamarin.Forms.FocusEventArgs e)
        {
        }
    }
}
