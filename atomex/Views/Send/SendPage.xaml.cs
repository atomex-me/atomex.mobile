using System;
using atomex.ViewModels.SendViewModels;
using Xamarin.Forms;

namespace atomex.Views.Send
{
    public partial class SendPage : ContentPage
    {
        public SendPage(SendViewModel sendViewModel)
        {
            InitializeComponent();
            BindingContext = sendViewModel;
        }

        public SendPage(TezosTokensSendViewModel sendViewModel)
        {
            InitializeComponent();
            BindingContext = sendViewModel;
        }

        private void AmountEntryFocus(object sender, EventArgs args)
        {
            AmountEntry.Focus();
        }

        private void FeeEntryFocus(object sender, EventArgs args)
        {
            FeeEntry.Focus();
        }

        private void GasPriceEntryFocus(object sender, EventArgs args)
        {
            GasPriceEntry.Focus();
        }
    }
}
