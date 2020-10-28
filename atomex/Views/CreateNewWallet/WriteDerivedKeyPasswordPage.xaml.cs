using System;
using System.Threading.Tasks;
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
            Device.StartTimer(TimeSpan.FromSeconds(0.25), () =>
            {
                if (args.IsFocused)
                    ScrollView.ScrollToAsync(0, ScrollView.Height / 2 - (PasswordFrame.Height + Labels.Height), true);
                else
                    ScrollView.ScrollToAsync(0, 0, true);
                return false;
            });
        }

        private async void OnPasswordTextChanged(object sender, TextChangedEventArgs args)
        {
            if (!String.IsNullOrEmpty(args.NewTextValue))
            {
                if (!PasswordHint.IsVisible)
                {
                    PasswordHint.IsVisible = true;
                    PasswordHint.Text = PasswordEntry.Placeholder;

                    await Task.Run(() =>
                    {
                        PasswordHint.FadeTo(1, 500, Easing.Linear);
                    });
                    await Task.Run(() =>
                    {
                        PasswordEntry.TranslateTo(0, 10, 500, Easing.CubicOut);
                    });
                    await Task.Run(() =>
                    {
                        PasswordHint.TranslateTo(0, -20, 500, Easing.CubicOut);
                    });
                }
            }
            else
            {
                await Task.Run(() =>
                {
                    PasswordHint.FadeTo(0, 500, Easing.Linear);
                });
                await Task.Run(() =>
                {
                    PasswordEntry.TranslateTo(0, 0, 500, Easing.CubicOut);
                });
                await PasswordHint.TranslateTo(0, -10, 500, Easing.CubicOut);
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
