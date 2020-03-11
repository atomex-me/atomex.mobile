using System;

using Xamarin.Forms;

namespace atomex
{
    public partial class CreateNewWalletSecondStepPage : ContentPage
    {
        public CreateNewWalletSecondStepPage()
        {
            InitializeComponent();
        }

        private void EntryFocused(object sender, FocusEventArgs e)
        {
            Frame.HasShadow = true;
        }
        private void EntryUnfocused(object sender, FocusEventArgs e)
        {
            Frame.HasShadow = false;
        }

        private void OnTextChanged(object sender, TextChangedEventArgs args)
        {
            if (!String.IsNullOrEmpty(args.NewTextValue))
            {
                if (!NameHint.IsVisible)
                {
                    NameHint.IsVisible = true;
                    NameHint.Text = Entry.Placeholder;
                }
            }
            else
            {
                NameHint.IsVisible = false;
            }
        }

        private async void OnNextButtonClicked(object sender, EventArgs args)
        {
            await Navigation.PushAsync(new CreateNewWalletThirdStepPage());
        }
    }
}
