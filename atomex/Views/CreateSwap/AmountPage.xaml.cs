using System;
using System.Globalization;
using atomex.Resources;
using atomex.ViewModel;
using Xamarin.Forms;

namespace atomex.Views.CreateSwap
{
    public partial class AmountPage : ContentPage
    {

        private ConversionViewModel _conversionViewModel;

        public AmountPage()
        {
            InitializeComponent();
        }
        public AmountPage(ConversionViewModel conversionViewModel)
        {
            InitializeComponent();
            _conversionViewModel = conversionViewModel;
            Amount.Placeholder = AppResources.AmountEntryPlaceholder + ", " + conversionViewModel.FromCurrencyViewModel.CurrencyCode;
            BindingContext = _conversionViewModel;
        }
        private void AmountEntryFocused(object sender, FocusEventArgs args)
        {
            InvalidAmountLabel.IsVisible = false;
            AmountFrame.HasShadow = args.IsFocused;
            if (!args.IsFocused)
            {
                try
                {
                    decimal.TryParse(Amount.Text?.Replace(",", "."), NumberStyles.Any, CultureInfo.InvariantCulture, out decimal amount);
                    _conversionViewModel.Amount = amount;
                    Amount.Text = _conversionViewModel.Amount.ToString();
                }
                catch (FormatException)
                {
                    _conversionViewModel.Amount = 0;
                    Amount.Text = "0";
                }
            }
        }

        private void OnAmountTextChanged(object sender, TextChangedEventArgs args)
        {
            try
            {
                decimal.TryParse(Amount.Text?.Replace(",", "."), NumberStyles.Any, CultureInfo.InvariantCulture, out decimal amount);
                _conversionViewModel.Amount = amount;
            }
            catch (FormatException)
            {
                _conversionViewModel.Amount = 0;
            }

            if (!String.IsNullOrEmpty(args.NewTextValue))
            {
                if (!AmountHint.IsVisible)
                {
                    AmountHint.IsVisible = true;
                    AmountHint.Text = Amount.Placeholder;
                    Amount.VerticalTextAlignment = TextAlignment.Start;
                }
            }
            else
            {
                AmountHint.IsVisible = false;
                Amount.VerticalTextAlignment = TextAlignment.Center;
            }
        }

        private void OnSetMaxAmountButtonClicked(object sender, EventArgs args)
        {
            InvalidAmountLabel.IsVisible = false;
            Amount.Text = _conversionViewModel.MaxAmount.ToString();
            _conversionViewModel.Amount = _conversionViewModel.MaxAmount;
        }
        private async void OnNextButtonClicked(object sender, EventArgs args)
        {
            if (String.IsNullOrWhiteSpace(Amount.Text))
            {
                await DisplayAlert(AppResources.Warning, AppResources.EnterAmountLabel, AppResources.AcceptButton);
                return;
            }

            decimal amount;
            try
            {
                decimal.TryParse(Amount.Text?.Replace(",", "."), NumberStyles.Any, CultureInfo.InvariantCulture, out amount);
            }
            catch (FormatException)
            {
                amount = 0;
            }

            if (amount <= 0)
            {
                InvalidAmountLabel.IsVisible = true;
                InvalidAmountLabel.Text = AppResources.AmountLessThanZeroError + " " + _conversionViewModel.FromCurrencyViewModel.CurrencyCode;
                return;
            }
            if (amount > _conversionViewModel.MaxAmount)
            {
                InvalidAmountLabel.IsVisible = true;
                InvalidAmountLabel.Text = AppResources.InsufficientFunds;
                return;
            }
            if (_conversionViewModel.IsNoLiquidity)
            {
                await DisplayAlert(AppResources.Warning, AppResources.NoLiquidityError, AppResources.AcceptButton);
                return;
            }
            await Navigation.PushAsync(new ConfirmationPage(_conversionViewModel));
        }
    }
}
