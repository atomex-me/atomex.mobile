using System;
using System.Collections.Generic;
using atomex.ViewModel.WalletBeacon;
using Xamarin.Forms;

namespace atomex.Views.WalletBeacon
{
    public partial class ConfirmDappPage : ContentPage
    {
        public ConfirmDappPage()
        {
            InitializeComponent();
        }

        public ConfirmDappPage(ConfirmDappViewModel dappViewModel)
        {
            InitializeComponent();
            BindingContext = dappViewModel;
        }
    }
}
