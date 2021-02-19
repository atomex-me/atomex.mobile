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
            _createNewWalletViewModel.Warning = string.Empty;
        }

        private void OnPickerClicked(object sender, EventArgs args)
        {
            LanguagesPicker.Focus();
        }

        private void OnEditorFocused(object sender, FocusEventArgs args)
        {
            EditorFrame.HasShadow = args.IsFocused;
            _createNewWalletViewModel.Warning = string.Empty;

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
            _createNewWalletViewModel.WriteMnemonic();

            if (_createNewWalletViewModel.Warning != string.Empty)
                return;

            await Navigation.PushAsync(new WriteDerivedKeyPasswordPage(_createNewWalletViewModel));
        }
    }
}
