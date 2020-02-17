using System;
using atomex.ViewModel;
using Atomex;
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
            await DisplayAlert("Warning","In progress","Ok");
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
