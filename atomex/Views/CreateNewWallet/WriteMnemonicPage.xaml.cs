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

        private void OnEditorFocused(object sender, FocusEventArgs args)
        {
            EditorFrame.HasShadow = args.IsFocused;
            Error.IsVisible = false;

            Device.StartTimer(TimeSpan.FromSeconds(0.25), () =>
            {
                if (args.IsFocused)
                    ScrollView.ScrollToAsync(0, ScrollView.Height / 2 - (EditorFrame.Height + Labels.Height), true);
                else
                    ScrollView.ScrollToAsync(0, 0, true);
                return false;
            });
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
