using System;
using System.Collections.Generic;
using atomex.ViewModel;
using Xamarin.Forms;

namespace atomex.Views.TezosTokens
{
    public partial class TokenInfoPage : ContentPage
    {
        public TokenInfoPage(TezosTokenViewModel tezosTokenViewModel)
        {
            InitializeComponent();
            BindingContext = tezosTokenViewModel;
        }
    }
}
