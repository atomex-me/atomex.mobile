using System;
using System.Collections.Generic;
using atomex.ViewModel.WalletBeacon;
using Xamarin.Forms;

namespace atomex.Views.WalletBeacon
{
    public partial class DappsPage : ContentPage
    {
        public DappsPage(DappsViewModel dappsViewModel)
        {
            InitializeComponent();
            BindingContext = dappsViewModel;
        }
    }
}
