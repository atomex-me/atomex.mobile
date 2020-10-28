using System;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace atomex.Views.CreateNewWallet
{
    public partial class CreateDerivedKeyPasswordPage : ContentPage
    {

        private CreateNewWalletViewModel _createNewWalletViewModel;

        public CreateDerivedKeyPasswordPage()
        {
            InitializeComponent();
        }

        public CreateDerivedKeyPasswordPage(CreateNewWalletViewModel createNewWalletViewModel)
        {
            InitializeComponent();
            _createNewWalletViewModel = createNewWalletViewModel;
            BindingContext = createNewWalletViewModel;
        }

        private void PasswordEntryFocused(object sender, FocusEventArgs args)
        {
            PasswordFrame.HasShadow = args.IsFocused;
            Error.IsVisible = false;

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
            _createNewWalletViewModel.SetPassword(CreateNewWalletViewModel.PasswordType.DerivedPassword, args.NewTextValue);
        }

        private void PasswordConfirmationEntryFocused(object sender, FocusEventArgs args)
        {
            PasswordConfirmationFrame.HasShadow = args.IsFocused;
            Error.IsVisible = false;

            Device.StartTimer(TimeSpan.FromSeconds(0.25), () =>
            {
                if (args.IsFocused)
                    ScrollView.ScrollToAsync(0, ScrollView.Height / 2 - (PasswordFrame.Height + Labels.Height), true);
                else
                    ScrollView.ScrollToAsync(0, 0, true);
                return false;
            });
        }

        private async void OnPasswordConfirmationTextChanged(object sender, TextChangedEventArgs args)
        {
            if (!String.IsNullOrEmpty(args.NewTextValue))
            {
                if (!PasswordConfirmationHint.IsVisible)
                {
                    PasswordConfirmationHint.IsVisible = true;
                    PasswordConfirmationHint.Text = PasswordConfirmationEntry.Placeholder;

                    await Task.Run(() =>
                    {
                        PasswordConfirmationHint.FadeTo(1, 500, Easing.Linear);
                    });
                    await Task.Run(() =>
                    {
                        PasswordConfirmationEntry.TranslateTo(0, 10, 500, Easing.CubicOut);
                    });
                    await Task.Run(() =>
                    {
                        PasswordConfirmationHint.TranslateTo(0, -20, 500, Easing.CubicOut);
                    });
                }
            }
            else
            {
                await Task.Run(() =>
                {
                    PasswordConfirmationHint.FadeTo(0, 500, Easing.Linear);
                });
                await Task.Run(() =>
                {
                    PasswordConfirmationEntry.TranslateTo(0, 0, 500, Easing.CubicOut);
                });
                await PasswordConfirmationHint.TranslateTo(0, -10, 500, Easing.CubicOut);
                PasswordConfirmationHint.IsVisible = false;
            }
            _createNewWalletViewModel.SetPassword(CreateNewWalletViewModel.PasswordType.DerivedPasswordConfirmation, args.NewTextValue);
        }

        private async void OnNextButtonClicked(object sender, EventArgs args)
        {
            var result = _createNewWalletViewModel.CheckDerivedPassword();
            if (result == null)
            {
                _createNewWalletViewModel.CreateHdWallet();
                await Navigation.PushAsync(new CreateStoragePasswordPage(_createNewWalletViewModel));
            }
            else
            {
                Error.Text = result;
                Error.IsVisible = true;
            }
        }
    }
}
