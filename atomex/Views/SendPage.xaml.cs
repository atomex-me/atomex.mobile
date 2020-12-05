using System;
using Xamarin.Forms;
using Xamarin.Essentials;
using atomex.ViewModel.SendViewModels;
using atomex.Resources;
using System.Globalization;
using System.Threading.Tasks;

namespace atomex
{
    public partial class SendPage : ContentPage
    {

        private SendViewModel _sendViewModel;

        public SendPage(SendViewModel sendViewModel)
        {
            InitializeComponent();
            _sendViewModel = sendViewModel;
            BindingContext = sendViewModel;
            Amount.Placeholder = AppResources.AmountEntryPlaceholder + ", " + sendViewModel.CurrencyCode;
            if (sendViewModel.Currency.FeeCode == "ETH")
            {
                GasLayout.IsVisible = true;
                FeeLayout.IsVisible = false;
                return;
            }
            if (sendViewModel.Currency.FeeCode == "BTC" ||
                sendViewModel.Currency.FeeCode == "LTC")
            {
                FeeRateLayout.IsVisible = true;
                return;
            }
        }

        private async void AmountEntryFocused(object sender, FocusEventArgs args)
        {
            //AmountFrame.HasShadow = args.IsFocused;
            if (!args.IsFocused)
            {
                decimal amount;
                try
                {
                    decimal.TryParse(Amount.Text?.Replace(",","."), NumberStyles.Any, CultureInfo.InvariantCulture, out amount);
                }
                catch (FormatException)
                {
                    amount = 0;
                }
                await _sendViewModel.UpdateAmount(amount);
                Amount.Text = _sendViewModel.AmountString;
                Fee.Text = _sendViewModel.FeeString;
            }
        }

        private async void FeeEntryFocused(object sender, FocusEventArgs args)
        {
            //FeeFrame.HasShadow = args.IsFocused;
            if (!args.IsFocused)
            {
                decimal fee;
                try
                {
                    decimal.TryParse(Fee.Text?.Replace(",", "."), NumberStyles.Any, CultureInfo.InvariantCulture, out fee);
                }
                catch (FormatException)
                {
                    fee = 0;
                }
                await _sendViewModel.UpdateFee(fee);
                Fee.Text = _sendViewModel.FeeString;
                Amount.Text = _sendViewModel.AmountString;
            }
        }
        

        private void AddressEntryFocused(object sender, FocusEventArgs args)
        {
            //AddressFrame.HasShadow = args.IsFocused;
        }

        private void OnAddressEntryTapped(object sender, EventArgs args)
        {
            Address.Focus();
        }
        private void OnAmountEntryTapped(object sender, EventArgs args)
        {
            Amount.Focus();
        }
        private void OnFeeEntryTapped(object sender, EventArgs args)
        {
            Fee.Focus();
            Fee.CursorPosition = _sendViewModel.FeeString.Length;
        }

        private async void OnAmountTextChanged(object sender, TextChangedEventArgs args)
        {
            if (!String.IsNullOrEmpty(args.NewTextValue))
            {
                if (!AmountHint.IsVisible)
                {
                    AmountHint.IsVisible = true;
                    AmountHint.Text = Amount.Placeholder;

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

        private async void OnAddressTextChanged(object sender, TextChangedEventArgs args)
        {
            if (!String.IsNullOrEmpty(args.NewTextValue))
            {
                if (!AddressHint.IsVisible)
                {
                    AddressHint.IsVisible = true;
                    AddressHint.Text = Address.Placeholder;

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

        private void GasPriceEntryFocused(object sender, FocusEventArgs args)
        {
            //GasPriceFrame.HasShadow = args.IsFocused;
        }

        private async void OnFeeTextChanged(object sender, TextChangedEventArgs args)
        {
            if (!String.IsNullOrEmpty(args.NewTextValue))
            {
                if (!FeeHint.IsVisible)
                {
                    FeeHint.IsVisible = true;
                    FeeHint.Text = Fee.Placeholder + ", " + _sendViewModel.CurrencyViewModel.FeeCurrencyCode;

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

        private void OnUseDefaultFeeToggled(object sender, ToggledEventArgs args)
        {
            if (args.Value)
            {
                _sendViewModel.UseDefaultFee = true;
                Fee.Text = _sendViewModel.FeeString;
                if (GasLayout.IsVisible)
                    GasPrice.Text = _sendViewModel.FeePrice.ToString();
            }
        }

        private async void OnSetMaxAmountButtonClicked(object sender, EventArgs args)
        {
            await _sendViewModel.OnMaxClick();
            Amount.Text = _sendViewModel.AmountString;
            Fee.Text = _sendViewModel.FeeString;

            Amount.CursorPosition = _sendViewModel.AmountString.Length;
        }

        private async void OnScanButtonClicked(object sender, EventArgs args)
        {

            var scanningQrPage = new ScanningQrPage(selected =>
            {
                Address.Text = selected;
            });

            await Navigation.PushAsync(scanningQrPage);
        }

        private async void OnPasteButtonClicked(object sender, EventArgs args)
        {
            if (Clipboard.HasText)
            {
                var text = await Clipboard.GetTextAsync();
                Address.Text = text;
            }
            else
            {
                await DisplayAlert(AppResources.Error, AppResources.EmptyClipboard, AppResources.AcceptButton);
            }
        }

        private async void OnNextButtonClicked(object sender, EventArgs args)
        {
            var error = _sendViewModel.OnNextCommand();
            if (error != null)
            {
                await DisplayAlert(AppResources.Error, error, AppResources.AcceptButton);
            }
            else
            {
                await Navigation.PushAsync(new SendingConfirmationPage(_sendViewModel));
            }
        }
    }
}
