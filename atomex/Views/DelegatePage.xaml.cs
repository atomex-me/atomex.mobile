using System;
using System.Collections.Generic;
using atomex.ViewModel;
using Xamarin.Forms;

namespace atomex
{
    public partial class DelegatePage : ContentPage
    {
        private DelegateViewModel _delegateViewModel;

        public DelegatePage()
        {
            InitializeComponent();
        }

        public DelegatePage(DelegateViewModel delegateViewModel)
        {
            InitializeComponent();
            _delegateViewModel = delegateViewModel;
            BindingContext = delegateViewModel;
        }

        private void OnBakerPickerFocused(object sender, FocusEventArgs args)
        {
            BakerFrame.HasShadow = args.IsFocused;
        }

        private void OnFromAddressPickerFocused(object sender, FocusEventArgs args)
        {
            FromAddressFrame.HasShadow = args.IsFocused;
        }

        private async void OnNextButtonClicked(object sender, EventArgs args)
        {
            var error = await _delegateViewModel.Validate();
            if (error != null)
            {
                await DisplayAlert("Error", error, "Ok");
                return;
            }
            else
            {
                await Navigation.PushAsync(new DelegationConfirmationPage(_delegateViewModel));
            }
        }
    }
}
