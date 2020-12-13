﻿using System;
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

        private async void PasswordEntryFocused(object sender, FocusEventArgs args)
        {
            PasswordFrame.HasShadow = args.IsFocused;
            Error.IsVisible = false;

            if (args.IsFocused)
                await Content.TranslateTo(0, -Page.Height / 2 + PasswordFrame.Height + Labels.Height, 500, Easing.CubicInOut);
            else
                await Content.TranslateTo(0, 0, 1000, Easing.BounceOut);
        }

        private async void OnPasswordTextChanged(object sender, TextChangedEventArgs args)
        {
            if (!String.IsNullOrEmpty(args.NewTextValue))
            {
                if (!PasswordHint.IsVisible)
                {
                    PasswordHint.IsVisible = true;
                    PasswordHint.Text = PasswordEntry.Placeholder;

                    _ = PasswordHint.FadeTo(1, 500, Easing.Linear);
                    _ = PasswordEntry.TranslateTo(0, 10, 500, Easing.CubicOut);
                    _ = PasswordHint.TranslateTo(0, -15, 500, Easing.CubicOut);
                }
            }
            else
            {
                await Task.WhenAll(
                    PasswordHint.FadeTo(0, 500, Easing.Linear),
                    PasswordEntry.TranslateTo(0, 0, 500, Easing.CubicOut),
                    PasswordHint.TranslateTo(0, -10, 500, Easing.CubicOut)
                );
                PasswordHint.IsVisible = false;
            }
            _createNewWalletViewModel.SetPassword(CreateNewWalletViewModel.PasswordType.DerivedPassword, args.NewTextValue);
        }

        private async void PasswordConfirmationEntryFocused(object sender, FocusEventArgs args)
        {
            PasswordConfirmationFrame.HasShadow = args.IsFocused;
            Error.IsVisible = false;

            if (args.IsFocused)
                await Content.TranslateTo(0, -Page.Height / 2 + PasswordFrame.Height + Labels.Height, 500, Easing.CubicInOut);
            else
                await Content.TranslateTo(0, 0, 1000, Easing.BounceOut);
        }

        private async void OnPasswordConfirmationTextChanged(object sender, TextChangedEventArgs args)
        {
            if (!String.IsNullOrEmpty(args.NewTextValue))
            {
                if (!PasswordConfirmationHint.IsVisible)
                {
                    PasswordConfirmationHint.IsVisible = true;
                    PasswordConfirmationHint.Text = PasswordConfirmationEntry.Placeholder;

                    _ = PasswordConfirmationHint.FadeTo(1, 500, Easing.Linear);
                    _ = PasswordConfirmationEntry.TranslateTo(0, 10, 500, Easing.CubicOut);
                    _ = PasswordConfirmationHint.TranslateTo(0, -15, 500, Easing.CubicOut);
                }
            }
            else
            {
                await Task.WhenAll(
                    PasswordConfirmationHint.FadeTo(0, 500, Easing.Linear),
                    PasswordConfirmationEntry.TranslateTo(0, 0, 500, Easing.CubicOut),
                    PasswordConfirmationHint.TranslateTo(0, -10, 500, Easing.CubicOut)
                );
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
