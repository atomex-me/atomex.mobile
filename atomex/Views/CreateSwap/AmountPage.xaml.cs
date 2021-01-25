using System;
using System.Globalization;
using System.Threading.Tasks;
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
        private async void AmountEntryFocused(object sender, FocusEventArgs args)
        {
            AmountFrame.HasShadow = args.IsFocused;
            if (!args.IsFocused)
            {
                try
                {
                    decimal.TryParse(Amount.Text?.Replace(",", "."), NumberStyles.Any, CultureInfo.InvariantCulture, out decimal amount);
                    BlockActions(true);
                    await _conversionViewModel.UpdateAmountAsync(amount);
                    BlockActions(false);
                    Amount.Text = _conversionViewModel.Amount.ToString();
                }
                catch (FormatException)
                {
                    _conversionViewModel.Amount = 0;
                    Amount.Text = "0";
                }
            }
        }

        private void AmountEntryTapped(object sender, EventArgs args)
        {
            Amount.Focus();
            Amount.CursorPosition = _conversionViewModel.Amount.ToString().Length;
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

        private async void OnMaxAmountButtonClicked(object sender, EventArgs args)
        {
            MaxButton.IsVisible = MaxButton.IsEnabled = false;
            Loader.IsRunning = Loader.IsVisible = true;
            BlockActions(true);
            await _conversionViewModel.OnMaxClick();
            BlockActions(false);
            Amount.Text = _conversionViewModel.Amount.ToString();
            Loader.IsRunning = Loader.IsVisible = false;
            MaxButton.IsVisible = MaxButton.IsEnabled = true;
        }

        private void BlockActions(bool flag)
        {
            FeeCalculationLoader.IsVisible = FeeCalculationLoader.IsRunning = flag;
            
            if (flag)
                Content.Opacity = 0.5;
            else
                Content.Opacity = 1;
        }

        private async void OnTotalFeeTapped(object sender, EventArgs args)
        {
            string message = string.Format(
                   CultureInfo.InvariantCulture,
                   AppResources.TotalNetworkFeeDetail,
                   FormattableString.Invariant($"{_conversionViewModel.EstimatedPaymentFee} {_conversionViewModel.FromCurrencyViewModel.FeeCurrencyCode}"),
                   FormattableString.Invariant($"{_conversionViewModel.EstimatedPaymentFeeInBase:(0.00$)}"),
                   FormattableString.Invariant($"{_conversionViewModel.EstimatedRedeemFee} {_conversionViewModel.ToCurrencyViewModel.FeeCurrencyCode}"),
                   FormattableString.Invariant($"{_conversionViewModel.EstimatedRedeemFeeInBase:(0.00$)}"),
                   FormattableString.Invariant($"{_conversionViewModel.EstimatedMakerNetworkFee} {_conversionViewModel.FromCurrencyViewModel.FeeCurrencyCode}"),
                   FormattableString.Invariant($"{_conversionViewModel.EstimatedMakerNetworkFeeInBase:(0.00$)}"),
                   FormattableString.Invariant($"{_conversionViewModel.EstimatedTotalNetworkFeeInBase:0.00$}"));

            await DisplayAlert(AppResources.NetworkFee, message, AppResources.AcceptButton);
        }

        private async void OnNextButtonClicked(object sender, EventArgs args)
        {
            if (String.IsNullOrWhiteSpace(Amount.Text))
            {
                await DisplayAlert(AppResources.Warning, AppResources.EnterAmountLabel, AppResources.AcceptButton);
                return;
            }

            if (_conversionViewModel.IsNoLiquidity)
            {
                await DisplayAlert(AppResources.Error, AppResources.NoLiquidityError, AppResources.AcceptButton);
                return;
            }

            if (_conversionViewModel.Amount <= 0)
            {
                await DisplayAlert(AppResources.Error, AppResources.AmountLessThanZeroError, AppResources.AcceptButton);
                return;
            }

            await Navigation.PushAsync(new ConfirmationPage(_conversionViewModel));
        }
    }
}
