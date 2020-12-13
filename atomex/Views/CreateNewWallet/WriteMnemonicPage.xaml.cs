using System;
using Xamarin.Forms;

namespace atomex.Views.CreateNewWallet
{
    public partial class WriteMnemonicPage : ContentPage
    {
        private CreateNewWalletViewModel _createNewWalletViewModel;

        public WriteMnemonicPage()
        {
            InitializeComponent();
        }
        public WriteMnemonicPage(CreateNewWalletViewModel createNewWalletViewModel)
        {
            InitializeComponent();
            _createNewWalletViewModel = createNewWalletViewModel;
            BindingContext = createNewWalletViewModel;
        }

        private void OnPickerFocused(object sender, FocusEventArgs args)
        {
            PickerFrame.HasShadow = args.IsFocused;
            Error.IsVisible = false;
        }

        private void OnPickerClicked(object sender, EventArgs args)
        {
            LanguagesPicker.Focus();
        }

        private async void OnEditorFocused(object sender, FocusEventArgs args)
        {
            EditorFrame.HasShadow = args.IsFocused;
            Error.IsVisible = false;

            if (args.IsFocused)
                await Content.TranslateTo(0, -Page.Height / 2 + EditorFrame.Height + Labels.Height, 500, Easing.CubicInOut);
            else
                await Content.TranslateTo(0, 0, 1000, Easing.BounceOut);
        }

        private async void OnNextButtonClicked(object sender, EventArgs args)
        {
            var result = _createNewWalletViewModel.WriteMnemonic();
            if (result == null)
            {
                await Navigation.PushAsync(new WriteDerivedKeyPasswordPage(_createNewWalletViewModel));
            }
            else
            {
                Error.Text = result;
                Error.IsVisible = true;
            }
        }
    }
}
