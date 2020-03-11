using System;

using Xamarin.Forms;

namespace atomex
{
    public partial class CreateNewWalletFirstStepPage : ContentPage
    {
        public CreateNewWalletFirstStepPage()
        {
            InitializeComponent();
        }

        private void OnPickerSelectedIndexChanged(object sender, EventArgs args)
        {
            var picker = sender as Picker;
            int selectedIndex = picker.SelectedIndex;
            if (selectedIndex != -1)
            {
                
            }
        }
        private async void OnNextButtonClicked(object sender, EventArgs args)
        {
            await Navigation.PushAsync(new CreateNewWalletSecondStepPage());
        }
    }
}
