using System;
using System.Collections.Generic;
using atomex.ViewModel.WalletBeacon;
using Xamarin.Forms;

namespace atomex.Views.WalletBeacon
{
    public partial class DappsPage : ContentPage
    {
        private readonly DappsViewModel dappsViewModel;

        public DappsPage(DappsViewModel dappsViewModel)
        {
            InitializeComponent();
            BindingContext = dappsViewModel;

            this.dappsViewModel = dappsViewModel;
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();

            dappsViewModel.OnDisappearing();
        }
    }
}
