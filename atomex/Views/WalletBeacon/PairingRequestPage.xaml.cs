using System;
using System.Collections.Generic;
using atomex.ViewModel.WalletBeacon;
using Xamarin.Forms;

namespace atomex.Views.WalletBeacon
{
    public partial class PairingRequestPage : ContentPage
    {
        private readonly PairingRequestViewModel pairingRequestViewModel;

        public PairingRequestPage()
        {
            InitializeComponent();
        }

        public PairingRequestPage(PairingRequestViewModel pairingRequestViewModel)
        {
            InitializeComponent();
            BindingContext = pairingRequestViewModel;

            this.pairingRequestViewModel = pairingRequestViewModel;
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();

            pairingRequestViewModel.OnDisappearing();
        }
    }
}
