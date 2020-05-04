using System;
using atomex.Resources;
using atomex.ViewModel;
using Atomex.Core;
using Xamarin.Forms;

namespace atomex.Views.CreateSwap
{
    public partial class ConfirmationPage : ContentPage
    {
        private ConversionViewModel _conversionViewModel;

        private const int BACK_COUNT = 3;

        public ConfirmationPage()
        {
            InitializeComponent();
        }

        public ConfirmationPage(ConversionViewModel conversionViewModel)
        {
            InitializeComponent();
            _conversionViewModel = conversionViewModel;
            BindingContext = _conversionViewModel;
        }

        private async void OnConvertButtonClicked(object sender, EventArgs args)
        {
            BlockActions(true);
            var error = await _conversionViewModel.ConvertAsync();

            if (error != null)
            {
                BlockActions(false);
                if (error.Code == Errors.PriceHasChanged)
                {
                    await DisplayAlert(AppResources.PriceChanged, error.Description, AppResources.AcceptButton);
                }
                else
                {
                    await DisplayAlert(AppResources.Error, error.Description, AppResources.AcceptButton);
                }
                return;
            }
            BlockActions(false);
            var res = await DisplayAlert(AppResources.Success, AppResources.SwapCreated, null, AppResources.AcceptButton);
            if (!res)
            {
                _conversionViewModel.Amount = 0;
                for (var i = 1; i < BACK_COUNT; i++)
                {
                    Navigation.RemovePage(Navigation.NavigationStack[Navigation.NavigationStack.Count - 2]);
                }
                await Navigation.PopAsync();
            }
        }

        private void BlockActions(bool flag)
        {
            SendingLoader.IsVisible = SendingLoader.IsRunning = flag;
            ConvertButton.IsEnabled = !flag;
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
