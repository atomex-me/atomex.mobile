using System;
using atomex.ViewModel.SendViewModels;
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

        private void AmountEntryFocus(object sender, EventArgs args)
        {
            AmountEntry.Focus();
        }
    }
}
