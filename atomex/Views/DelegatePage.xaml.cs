using System;
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
            try
            {
                BlockActions(true);
                var error = await _delegateViewModel.Validate();
                BlockActions(false);
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
            catch (Exception e)
            {
                BlockActions(false);
                await DisplayAlert("Error", "An error has occurred while delegation validation.", "OK");
            }
        }

        private void BlockActions(bool flag)
        {
            ValidatingLoader.IsVisible = ValidatingLoader.IsRunning = flag;
            NextButton.IsEnabled = !flag;
            if (flag)
            {
                Content.Opacity = 0.5;
            }
            else
            {
                Content.Opacity = 1;
            }
        }
    }
}
