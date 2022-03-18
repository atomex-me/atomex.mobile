﻿using atomex.ViewModel.SendViewModels;
using atomex.ViewModel.WalletBeacon;
using Xamarin.Forms;

namespace atomex
{
    public partial class ScanningQrPage : ContentPage
    {
        public ScanningQrPage(SendViewModel sendViewModel)
        {
            InitializeComponent();
            BindingContext = sendViewModel;
        }

        public ScanningQrPage(TezosTokensSendViewModel sendViewModel)
        {
            InitializeComponent();
            BindingContext = sendViewModel;
        }
        
        public ScanningQrPage(DappsViewModel dappsViewModel)
        {
            InitializeComponent();
            BindingContext = dappsViewModel;
        }
    }
}
