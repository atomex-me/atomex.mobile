using System;
using System.Threading.Tasks;
using atomex.ViewModel;
using Atomex;
using Atomex.Core;
using Xamarin.Forms;

namespace atomex
{
    public partial class ConversionConfirmationPage : ContentPage
    {
        private IAtomexApp _app;

        private ConversionViewModel _conversionViewModel;

        public ConversionConfirmationPage()
        {
            InitializeComponent();
        }

        public ConversionConfirmationPage(IAtomexApp app,  ConversionViewModel conversionViewModel)
        {
            InitializeComponent();
            _app = app;
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
                    await DisplayAlert("Price has changed", error.Description, "Ok");
                }
                else
                {
                    await DisplayAlert("Error", error.Description, "Ok");
                }
                return;
            }
            BlockActions(false);
            var res = await DisplayAlert("Success","Swap succesfully created", null, "Ok");
            if (!res)
            {
                _conversionViewModel.Amount = 0;
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
