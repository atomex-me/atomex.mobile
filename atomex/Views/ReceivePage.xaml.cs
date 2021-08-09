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
    }
}
