using System;
using Xamarin.Forms;
using Xamarin.Essentials;
using atomex.ViewModel.SendViewModels;
using atomex.Resources;
using System.Globalization;

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
            if (sendViewModel.Currency.Name == "ETH" ||
                sendViewModel.Currency.Name =="USDT")
            {
                GasLayout.IsVisible = true;
                FeeLayout.IsVisible = false;
                return;
            }
            if (sendViewModel.Currency.Name == "BTC" ||
                sendViewModel.Currency.Name == "LTC")
            {
                FeeRateLayout.IsVisible = true;
                return;
            }
        }

        private async void AmountEntryFocused(object sender, FocusEventArgs args)
        {
            AmountFrame.HasShadow = args.IsFocused;
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
            FeeFrame.HasShadow = args.IsFocused;
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
            AddressFrame.HasShadow = args.IsFocused;
        }

        private void OnAmountTextChanged(object sender, TextChangedEventArgs args)
        {
            if (!String.IsNullOrEmpty(args.NewTextValue))
            {
                if (!AmountHint.IsVisible)
                {
                    AmountHint.IsVisible = true;
                    AmountHint.Text = Amount.Placeholder + ", " + _sendViewModel.CurrencyCode;
                    Amount.VerticalTextAlignment = TextAlignment.Start;
                }
            }
            else
            {
                AmountHint.IsVisible = false;
                Amount.VerticalTextAlignment = TextAlignment.Center;
            }
        }

        private void OnAddressTextChanged(object sender, TextChangedEventArgs args)
        {
            if (!String.IsNullOrEmpty(args.NewTextValue))
            {
                if (!AddressHint.IsVisible)
                {
                    AddressHint.IsVisible = true;
                    AddressHint.Text = Address.Placeholder;
                    Address.VerticalTextAlignment = TextAlignment.Start;
                }
            }
            else
            {
                AddressHint.IsVisible = false;
                Address.VerticalTextAlignment = TextAlignment.Center;
            }
        }

        private void GasPriceEntryFocused(object sender, FocusEventArgs args)
        {
            GasPriceFrame.HasShadow = args.IsFocused;
        }

        private void OnFeeTextChanged(object sender, TextChangedEventArgs args)
        {
            if (!String.IsNullOrEmpty(args.NewTextValue))
            {
                if (!FeeHint.IsVisible)
                {
                    FeeHint.IsVisible = true;
                    FeeHint.Text = Fee.Placeholder + ", " + _sendViewModel.CurrencyViewModel.FeeCurrencyCode;
                    Fee.VerticalTextAlignment = TextAlignment.Start;
                }
            }
            else
            {
                FeeHint.IsVisible = false;
                Fee.VerticalTextAlignment = TextAlignment.Center;
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
