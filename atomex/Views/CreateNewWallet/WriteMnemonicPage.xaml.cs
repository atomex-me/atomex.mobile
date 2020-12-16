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

        private void OnEditorFocused(object sender, FocusEventArgs args)
        {
            EditorFrame.HasShadow = args.IsFocused;
            Error.IsVisible = false;

            if (args.IsFocused)
            {
                Device.StartTimer(TimeSpan.FromSeconds(0.25), () =>
                {
                    Page.ScrollToAsync(0, Editor.Height, true);
                    return false;
                });
            }
            else
            {
                Device.StartTimer(TimeSpan.FromSeconds(0.25), () =>
                {
                    Page.ScrollToAsync(0, 0, true);
                    return false;
                });
            }
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
