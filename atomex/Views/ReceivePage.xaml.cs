using System;
using Xamarin.Forms;
using atomex.ViewModel;

namespace atomex
{
    public partial class ReceivePage : ContentPage
    {
        public ReceivePage()
        {
            InitializeComponent();
        }

        public ReceivePage(ReceiveViewModel receiveViewModel)
        {
            InitializeComponent();
            BindingContext = receiveViewModel;
        }

        private void OnPickerFocused(object sender, FocusEventArgs args)
        {
            AddressFrame.HasShadow = args.IsFocused;
        }

        private void OnPickerClicked(object sender, EventArgs args)
        {
            AddressPicker.Focus();
        }
    }
}
