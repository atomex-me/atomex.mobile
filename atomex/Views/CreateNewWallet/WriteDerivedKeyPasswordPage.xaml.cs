using System;
using Xamarin.Forms;

namespace atomex.Views.CreateNewWallet
{
    public partial class WriteDerivedKeyPasswordPage : ContentPage
    {

        private CreateNewWalletViewModel _createNewWalletViewModel;

        public WriteDerivedKeyPasswordPage()
        {
            InitializeComponent();
        }

        public WriteDerivedKeyPasswordPage(CreateNewWalletViewModel createNewWalletViewModel)
        {
            InitializeComponent();
            _createNewWalletViewModel = createNewWalletViewModel;
            BindingContext = createNewWalletViewModel;
        }

        private void PasswordEntryFocused(object sender, FocusEventArgs args)
        {
            PasswordFrame.HasShadow = args.IsFocused;
        }

        private void OnPasswordTextChanged(object sender, TextChangedEventArgs args)
        {
            if (!String.IsNullOrEmpty(args.NewTextValue))
            {
                if (!PasswordHint.IsVisible)
                {
                    PasswordHint.IsVisible = true;
                    PasswordHint.Text = PasswordEntry.Placeholder;
                    PasswordEntry.VerticalTextAlignment = TextAlignment.Start;
                }
            }
            else
            {
                PasswordEntry.VerticalTextAlignment = TextAlignment.Center;
                PasswordHint.IsVisible = false;
            }
            _createNewWalletViewModel.SetPassword(CreateNewWalletViewModel.PasswordType.DerivedPassword, PasswordEntry.Text);
        }

        private async void OnNextButtonClicked(object sender, EventArgs args)
        {
            _createNewWalletViewModel.CreateHdWallet();
            await Navigation.PushAsync(new CreateStoragePasswordPage(_createNewWalletViewModel));
        }
    }
}
