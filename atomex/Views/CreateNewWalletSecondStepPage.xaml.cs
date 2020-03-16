using System;

using Xamarin.Forms;

namespace atomex
{
    public partial class CreateNewWalletSecondStepPage : ContentPage
    {
        private CreateNewWalletViewModel _createNewWalletViewModel;

        public CreateNewWalletSecondStepPage()
        {
            InitializeComponent();
        }

        public CreateNewWalletSecondStepPage(CreateNewWalletViewModel createNewWalletViewModel)
        {
            InitializeComponent();
            _createNewWalletViewModel = createNewWalletViewModel;
            BindingContext = createNewWalletViewModel;
        }

        private void EntryFocused(object sender, FocusEventArgs e)
        {
            Frame.HasShadow = true;
            Error.IsVisible = false;
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
            var result = _createNewWalletViewModel.SaveWalletName();
            if (result == null)
            {
                await Navigation.PushAsync(new CreateNewWalletThirdStepPage(_createNewWalletViewModel));
            }
            else
            {
                Error.Text = result;
                Error.IsVisible = true;
            }
        }
    }
}
