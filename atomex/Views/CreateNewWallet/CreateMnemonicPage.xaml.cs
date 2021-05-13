using System;
using Xamarin.Forms;
using System.Threading.Tasks;

namespace atomex.Views.CreateNewWallet
{
    public partial class CreateMnemonicPage : ContentPage
    {
        public CreateMnemonicPage()
        {
            InitializeComponent();
        }

        public CreateMnemonicPage(CreateNewWalletViewModel createNewWalletViewModel)
        {
            InitializeComponent();
            BindingContext = createNewWalletViewModel;
        }

        private void OnLanguagePickerFocused(object sender, FocusEventArgs args)
        {
            LanguageFrame.HasShadow = args.IsFocused;
        }

        private void OnLanguagePickerClicked(object sender, EventArgs args)
        {
            LanguagePicker.Focus();
        }

        private void OnWordCountPickerFocused(object sender, FocusEventArgs args)
        {
            WordCountFrame.HasShadow = args.IsFocused;
        }

        private void OnWordCountPickerClicked(object sender, EventArgs args)
        {
            WordCountPicker.Focus();
        }

        private void OnUseDerivedPswdToggled(object sender, ToggledEventArgs args)
        {
            if (args.Value)
            {
                Device.StartTimer(TimeSpan.FromSeconds(0.25), () =>
                {
                    Page.ScrollToAsync(0, PasswordEntry.Height, true);
                    return false;
                });
                PasswordEntry.Focus();
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
            await Task.WhenAll(
                MnemonicPhraseFrame.FadeTo(1, 250, Easing.Linear),
                LoseMnemonicLabel.FadeTo(1, 250, Easing.Linear)
            );
            Device.StartTimer(TimeSpan.FromSeconds(0.25), () =>
            {
                Page.ScrollToAsync(0, NextButton.Height, true);
                return false;
            });
        }

        private void PasswordEntryFocused(object sender, FocusEventArgs args)
        {
            PasswordFrame.HasShadow = args.IsFocused;

            if (args.IsFocused)
            {
                Device.StartTimer(TimeSpan.FromSeconds(0.25), () =>
                {
                    Page.ScrollToAsync(0, PasswordEntry.Height, true);
                    return false;
                });
            }
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
        }

        private void PasswordConfirmationEntryFocused(object sender, FocusEventArgs args)
        {
            PasswordConfirmationFrame.HasShadow = args.IsFocused;

            if (args.IsFocused)
            {
                Device.StartTimer(TimeSpan.FromSeconds(0.25), () =>
                {
                    Page.ScrollToAsync(0, PasswordConfirmationEntry.Height, true);
                    return false;
                });
            }
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
        }

        protected override void OnDisappearing()
        {
            var vm = (CreateNewWalletViewModel)BindingContext;
            if (vm.ClearWarningCommand.CanExecute(null))
            {
                vm.ClearWarningCommand.Execute(null);
            }
            base.OnDisappearing();
        }
    }
}
