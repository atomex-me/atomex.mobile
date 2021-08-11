using System;
using System.Threading.Tasks;
using atomex.ViewModel.SendViewModels;
using Xamarin.Forms;

namespace atomex.Views.TezosTokens
{
    public partial class SendTokenPage : ContentPage
    {
        public SendTokenPage(TezosTokensSendViewModel tezosTokensSendViewModel)
        {
            InitializeComponent();
            BindingContext = tezosTokensSendViewModel;
        }

        private void AddressEntryFocused(object sender, FocusEventArgs args)
        {
            AddressLabel.IsVisible = !args.IsFocused;

            if (!args.IsFocused)
                Address.TextColor = Color.Transparent;
            else
            {
                string textColorName = "MainTextColor";
                if (App.Current.RequestedTheme == OSAppTheme.Dark)
                    textColorName = "MainTextColorDark";
                App.Current.Resources.TryGetValue(textColorName, out var textColor);
                Address.TextColor = (Color)textColor;
            }
        }

        private void OnAddressEntryTapped(object sender, EventArgs args)
        {
            Address.Focus();
        }
        private void OnAmountEntryTapped(object sender, EventArgs args)
        {
            Amount.Focus();
        }
        private void OnTokenIdEntryTapped(object sender, EventArgs args)
        {
            TokenId.Focus();
        }
        private void OnTokenContractEntryTapped(object sender, EventArgs args)
        {
            TokenContract.Focus();
        }

        private void OnFeeEntryTapped(object sender, EventArgs args)
        {
            Fee.Focus();
            if (!string.IsNullOrEmpty(Fee.Text))
                Fee.CursorPosition = Fee.Text.Length;
        }

        private async void OnAmountTextChanged(object sender, TextChangedEventArgs args)
        {
            if (!String.IsNullOrEmpty(args.NewTextValue))
            {
                if (!AmountHint.IsVisible)
                {
                    AmountHint.IsVisible = true;

                    _ = AmountHint.FadeTo(1, 500, Easing.Linear);
                    _ = Amount.TranslateTo(0, 10, 500, Easing.CubicOut);
                    _ = AmountHint.TranslateTo(0, -15, 500, Easing.CubicOut);
                }
            }
            else
            {
                await Task.WhenAll(
                    AmountHint.FadeTo(0, 500, Easing.Linear),
                    Amount.TranslateTo(0, 0, 500, Easing.CubicOut),
                    AmountHint.TranslateTo(0, -10, 500, Easing.CubicOut)
                );

                AmountHint.IsVisible = false;
            }
        }

        private async void OnTokenContractTextChanged(object sender, TextChangedEventArgs args)
        {
            if (!String.IsNullOrEmpty(args.NewTextValue))
            {
                if (!TokenContractHint.IsVisible)
                {
                    TokenContractHint.IsVisible = true;

                    _ = TokenContractHint.FadeTo(1, 500, Easing.Linear);
                    _ = TokenContract.TranslateTo(0, 10, 500, Easing.CubicOut);
                    _ = TokenContractHint.TranslateTo(0, -15, 500, Easing.CubicOut);
                }
            }
            else
            {
                await Task.WhenAll(
                    TokenContractHint.FadeTo(0, 500, Easing.Linear),
                    TokenContract.TranslateTo(0, 0, 500, Easing.CubicOut),
                    TokenContractHint.TranslateTo(0, -10, 500, Easing.CubicOut)
                );

                TokenContractHint.IsVisible = false;
            }
        }

        private async void OnTokenIdTextChanged(object sender, TextChangedEventArgs args)
        {
            if (!String.IsNullOrEmpty(args.NewTextValue))
            {
                if (!TokenIdHint.IsVisible)
                {
                    TokenIdHint.IsVisible = true;

                    _ = TokenIdHint.FadeTo(1, 500, Easing.Linear);
                    _ = TokenId.TranslateTo(0, 10, 500, Easing.CubicOut);
                    _ = TokenIdHint.TranslateTo(0, -15, 500, Easing.CubicOut);
                }
            }
            else
            {
                await Task.WhenAll(
                    TokenIdHint.FadeTo(0, 500, Easing.Linear),
                    TokenId.TranslateTo(0, 0, 500, Easing.CubicOut),
                    TokenIdHint.TranslateTo(0, -10, 500, Easing.CubicOut)
                );

                TokenIdHint.IsVisible = false;
            }
        }

        private async void OnAddressTextChanged(object sender, TextChangedEventArgs args)
        {
            if (!String.IsNullOrEmpty(args.NewTextValue))
            {
                if (!AddressHint.IsVisible)
                {
                    AddressHint.IsVisible = true;

                    _ = AddressHint.FadeTo(1, 500, Easing.Linear);
                    _ = Address.TranslateTo(0, 10, 500, Easing.CubicOut);
                    _ = AddressHint.TranslateTo(0, -15, 500, Easing.CubicOut);
                }
            }
            else
            {
                await Task.WhenAll(
                    AddressHint.FadeTo(0, 500, Easing.Linear),
                    Address.TranslateTo(0, 0, 500, Easing.CubicOut),
                    AddressHint.TranslateTo(0, -10, 500, Easing.CubicOut)
                );

                AddressHint.IsVisible = false;
            }
        }

        private async void OnFeeTextChanged(object sender, TextChangedEventArgs args)
        {
            if (!String.IsNullOrEmpty(args.NewTextValue))
            {
                if (!FeeHint.IsVisible)
                {
                    FeeHint.IsVisible = true;

                    _ = FeeHint.FadeTo(1, 500, Easing.Linear);
                    _ = Fee.TranslateTo(0, 10, 500, Easing.CubicOut);
                    _ = FeeHint.TranslateTo(0, -15, 500, Easing.CubicOut);
                }
            }
            else
            {
                await Task.WhenAll(
                    FeeHint.FadeTo(0, 500, Easing.Linear),
                    Fee.TranslateTo(0, 0, 500, Easing.CubicOut),
                    FeeHint.TranslateTo(0, -10, 500, Easing.CubicOut)
                );

                FeeHint.IsVisible = false;
            }
        }

        private void OnSetMaxAmountButtonClicked(object sender, EventArgs args)
        {
            if (!string.IsNullOrEmpty(Amount.Text))
                Amount.CursorPosition = Amount.Text.Length;
            Amount.Unfocus();
        }

        private void OnPasteButtonClicked(object sender, EventArgs args)
        {
            Address.TextColor = Color.Transparent;
            AddressLabel.IsVisible = true;
            if (!string.IsNullOrEmpty(Address.Text))
                Address.CursorPosition = Address.Text.Length;
            Address.Unfocus();
        }
    }
}
