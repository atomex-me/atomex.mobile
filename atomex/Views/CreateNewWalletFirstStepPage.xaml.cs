using System;

using Xamarin.Forms;

namespace atomex
{
    public partial class CreateNewWalletFirstStepPage : ContentPage
    {

        private CreateNewWalletViewModel _createNewWalletViewModel;

        public CreateNewWalletFirstStepPage()
        {
            InitializeComponent();
        }

        public CreateNewWalletFirstStepPage(CreateNewWalletViewModel createNewWalletViewModel)
        {
            InitializeComponent();
            _createNewWalletViewModel = createNewWalletViewModel;
            BindingContext = createNewWalletViewModel;
        }

        private async void OnNextButtonClicked(object sender, EventArgs args)
        {
            await Navigation.PushAsync(new CreateNewWalletSecondStepPage(_createNewWalletViewModel));
        }
    }
}
