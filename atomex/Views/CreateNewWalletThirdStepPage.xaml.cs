using System;

using Xamarin.Forms;

namespace atomex
{
    public partial class CreateNewWalletThirdStepPage : ContentPage
    {

        private CreateNewWalletViewModel _createNewWalletViewModel;

        public CreateNewWalletThirdStepPage()
        {
            InitializeComponent();
        }

        public CreateNewWalletThirdStepPage(CreateNewWalletViewModel createNewWalletViewModel)
        {
            InitializeComponent();
            _createNewWalletViewModel = createNewWalletViewModel;
            BindingContext = createNewWalletViewModel;
        }

        private async void OnNextButtonClicked(object sender, EventArgs args)
        {
            if (string.IsNullOrEmpty(_createNewWalletViewModel.Mnemonic))
            {
                await DisplayAlert("Alert", "Click Generate", "Ok");
            }
            else
            {
                await Navigation.PushAsync(new CreateNewWalletFourthPage(_createNewWalletViewModel));
            }
        }

        private void OnGenerateButtonClicked(object sender, EventArgs args)
        {
            _createNewWalletViewModel.GenerateMnemonic();
        }
    }
}
