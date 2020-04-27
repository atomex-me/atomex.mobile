using System;
using Xamarin.Forms;
using Xamarin.Essentials;
using atomex.ViewModel.SendViewModels;

namespace atomex
{
    public partial class SendPage : ContentPage
    {

        private SendViewModel _sendViewModel;

        public SendPage(SendViewModel sendViewModel)
        {
            InitializeComponent();
            _sendViewModel = sendViewModel;
            if (sendViewModel.Currency.Name == "ETH" ||
                sendViewModel.Currency.Name =="USDT")
            {
                GasLayout.IsVisible = true;
                UseDefaultFeeSwitch.Opacity = 0.5;
                FeeLayout.IsVisible = UseDefaultFeeSwitch.IsEnabled = false;
            }
            BindingContext = sendViewModel;
        }

        private void AmountEntryFocused(object sender, FocusEventArgs args)
        {
            AmountFrame.HasShadow = args.IsFocused;
            if (!args.IsFocused)
            {
                decimal amount;
                if (String.IsNullOrEmpty(Amount?.Text) || String.IsNullOrWhiteSpace(Amount?.Text))
                    amount = 0;
                else
                    amount = Convert.ToDecimal(Amount?.Text);
                _sendViewModel.UpdateAmount(amount);
                Amount.Text = _sendViewModel.AmountString;
                Fee.Text = _sendViewModel.FeeString;
            }
        }

        private void FeeEntryFocused(object sender, FocusEventArgs args)
        {
            FeeFrame.HasShadow = args.IsFocused;
            if (!args.IsFocused)
            {
                decimal fee;
                if (String.IsNullOrEmpty(Fee?.Text) || String.IsNullOrWhiteSpace(Fee?.Text))
                    fee = 0;
                else
                    fee = Convert.ToDecimal(Fee?.Text);
                _sendViewModel.UpdateFee(fee);
                Fee.Text = _sendViewModel.FeeString;
            }
        }
        

        private void AddressEntryFocused(object sender, FocusEventArgs args)
        {
            AddressFrame.HasShadow = args.IsFocused;
            if (!args.IsFocused)
            {
                _sendViewModel.EstimateMaxAmountAndFee();
            }
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

        private void OnSetMaxAmountButtonClicked(object sender, EventArgs args)
        {
            _sendViewModel.EstimateMaxAmountAndFee();
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
                await DisplayAlert("Error", "Clipboard is empty", "Ok");
            }
        }

        private async void OnNextButtonClicked(object sender, EventArgs args)
        {
            var error = _sendViewModel.OnNextCommand();
            if (error != null)
            {
                await DisplayAlert("Warning", error, "Ok");
            }
            else
            {
                await Navigation.PushAsync(new SendingConfirmationPage(_sendViewModel));
            }
        }
    }
}
