using System;
using System.Collections.Generic;
using atomex.ViewModel.WalletBeacon;
using Xamarin.Forms;

namespace atomex.Views.WalletBeacon
{
    public partial class TezosTransactionRequestPage : ContentPage
    {
        public TezosTransactionRequestPage(TezosTransactionRequestViewModel tezosTransactionRequestViewModel)
        {
            InitializeComponent();
            BindingContext = tezosTransactionRequestViewModel;
        }
    }
}
