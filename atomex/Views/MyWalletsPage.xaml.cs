using System;
using System.Collections.Generic;

using Xamarin.Forms;

namespace atomex
{
    public partial class MyWalletsPage : ContentPage
    {
        public MyWalletsPage()
        {
            InitializeComponent();
        }

        private void OnWalletClicked(object sender, EventArgs args)
        {
            Content.Opacity = 0.5f;
            Loader.IsRunning = true;
            Application.Current.MainPage = new MainPage();
        }
    }
}
