using System;
using System.Collections.Generic;
using atomex.ViewModel.WalletBeacon;
using Xamarin.Forms;

namespace atomex.Views.WalletBeacon
{
    public partial class PairingRequestPage : ContentPage
    {
        public PairingRequestPage()
        {
            InitializeComponent();
        }

        public PairingRequestPage(PairingRequestViewModel dappViewModel)
        {
            InitializeComponent();
            BindingContext = dappViewModel;
        }
    }
}
