using System;

using Xamarin.Forms;

namespace atomex
{
    public partial class CreateNewWalletThirdStepPage : ContentPage
    {
        public CreateNewWalletThirdStepPage()
        {
            InitializeComponent();
        }

        private void OnPickerLanguageSelectedIndexChanged(object sender, EventArgs args)
        {
            var picker = sender as Picker;
            int selectedIndex = picker.SelectedIndex;
            if (selectedIndex != -1)
            {

            }
        }

        private void OnPickerWordCountSelectedIndexChanged(object sender, EventArgs args)
        {
            var picker = sender as Picker;
            int selectedIndex = picker.SelectedIndex;
            if (selectedIndex != -1)
            {

            }
        }

        private async void OnNextButtonClicked(object sender, EventArgs args)
        {
            await Navigation.PushAsync(new CreateNewWalletFourthPage());
        }
    }
}
