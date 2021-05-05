using System;
using atomex.ViewModel;
using Xamarin.Forms;

namespace atomex
{
    public partial class DelegatePage : ContentPage
    {
        public DelegatePage()
        {
            InitializeComponent();
        }

        public DelegatePage(DelegateViewModel delegateViewModel)
        {
            InitializeComponent();
            BindingContext = delegateViewModel;
        }

        private void OnFromAddressPickerClicked(object sender, EventArgs args)
        {
            AddressPicker.Focus();
        }

        private void OnFeeEntryFocused(object sender, FocusEventArgs args)
        {
            if (args.IsFocused)
            {
                Device.StartTimer(TimeSpan.FromSeconds(0.25), () =>
                {
                    Page.ScrollToAsync(0, FeeFrame.Height, true);
                    return false;
                });
            }
            else
            {
                Device.StartTimer(TimeSpan.FromSeconds(0.25), () =>
                {
                    Page.ScrollToAsync(0, 0, true);
                    return false;
                });
            }
        }

        private void OnFeeEntryTapped(object sender, EventArgs args)
        {
            Fee.Focus();
        }
    }
}
