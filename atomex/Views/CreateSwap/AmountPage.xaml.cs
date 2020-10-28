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
        private void AmountEntryFocused(object sender, FocusEventArgs args)
        {
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

        private async void OnAmountTextChanged(object sender, TextChangedEventArgs args)
        {
            //try
            //{
            //    decimal.TryParse(Amount.Text?.Replace(",", "."), NumberStyles.Any, CultureInfo.InvariantCulture, out decimal amount);
            //    _conversionViewModel.Amount = amount;
            //}
            //catch (FormatException)
            //{
            //    _conversionViewModel.Amount = 0;
            //}

            if (!String.IsNullOrEmpty(args.NewTextValue))
            {
                if (!AmountHint.IsVisible)
                {
                    AmountHint.IsVisible = true;
                    AmountHint.Text = Amount.Placeholder;

                    await Task.Run(() =>
                    {
                        AmountHint.FadeTo(1, 1000, Easing.Linear);
                    });
                    await Task.Run(() =>
                    {
                        Amount.TranslateTo(0, 10, 500, Easing.CubicOut);
                    });
                    await Task.Run(() =>
                    {
                        AmountHint.TranslateTo(0, -20, 500, Easing.CubicOut);
                    });
                }
            }
            else
            {
                await Task.Run(() =>
                {
                    AmountHint.FadeTo(0, 500, Easing.Linear);
                });
                await Task.Run(() =>
                {
                    Amount.TranslateTo(0, 0, 500, Easing.CubicOut);
                });
                await AmountHint.TranslateTo(0, -10, 500, Easing.CubicOut);
                AmountHint.IsVisible = false;
            }
        }

        private async void OnMaxAmountButtonClicked(object sender, EventArgs args)
        {
            await _conversionViewModel.OnMaxClick();
            Amount.Text = _conversionViewModel.Amount.ToString();
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

            if (!string.IsNullOrEmpty(_conversionViewModel.Warning))
            {
                await DisplayAlert(AppResources.Error, AppResources.FailedToConvert, AppResources.AcceptButton);
                return;
            }
            await Navigation.PushAsync(new ConfirmationPage(_conversionViewModel));
        }
    }
}
