using System;
using System.Globalization;
using atomex.Resources;
using atomex.ViewModel;
using Atomex.Core;
using Xamarin.Forms;

namespace atomex.Views.CreateSwap
{
    public partial class ConfirmationPage : ContentPage
    {
        private ConversionViewModel _conversionViewModel;

        public ConfirmationPage()
        {
            InitializeComponent();
        }

        public ConfirmationPage(ConversionViewModel conversionViewModel)
        {
            InitializeComponent();
            _conversionViewModel = conversionViewModel;
            BindingContext = _conversionViewModel;
        }

        private async void OnConvertButtonClicked(object sender, EventArgs args)
        {
            BlockActions(true);
            var error = await _conversionViewModel.ConvertAsync();

            if (error != null)
            {
                BlockActions(false);
                if (error.Code == Errors.PriceHasChanged)
                {
                    await DisplayAlert(AppResources.PriceChanged, error.Description, AppResources.AcceptButton);
                }
                else
                {
                    await DisplayAlert(AppResources.Error, error.Description, AppResources.AcceptButton);
                }
                return;
            }
            BlockActions(false);
            var res = await DisplayAlert(AppResources.Success, AppResources.SwapCreated, null, AppResources.AcceptButton);
            if (!res)
            {
                _conversionViewModel.Amount = 0;
                await Navigation.PopToRootAsync();
            }
        }

        private async void OnTotalFeeTapped(object sender, EventArgs args)
        {
            string message = string.Format(
                CultureInfo.InvariantCulture,
                AppResources.TotalNetworkFeeDetail,
                AppResources.PaymentFeeLabel,
                FormattableString.Invariant($"{_conversionViewModel.EstimatedPaymentFee} {_conversionViewModel.FromCurrencyViewModel.FeeCurrencyCode}"),
                FormattableString.Invariant($"{_conversionViewModel.EstimatedPaymentFeeInBase:(0.00$)}"),
                _conversionViewModel.HasRewardForRedeem ?
                    AppResources.RewardForRedeemLabel :
                    AppResources.RedeemFeeLabel,
                _conversionViewModel.HasRewardForRedeem ?
                    FormattableString.Invariant($"{_conversionViewModel.RewardForRedeem} {_conversionViewModel.ToCurrencyViewModel.FeeCurrencyCode}") :
                    FormattableString.Invariant($"{_conversionViewModel.EstimatedRedeemFee} {_conversionViewModel.ToCurrencyViewModel.FeeCurrencyCode}"),
                _conversionViewModel.HasRewardForRedeem ?
                    FormattableString.Invariant($"{_conversionViewModel.RewardForRedeemInBase:(0.00$)}") :
                    FormattableString.Invariant($"{_conversionViewModel.EstimatedRedeemFeeInBase:(0.00$)}"),
                AppResources.MakerFeeLabel,
                FormattableString.Invariant($"{_conversionViewModel.EstimatedMakerNetworkFee} {_conversionViewModel.FromCurrencyViewModel.FeeCurrencyCode}"),
                FormattableString.Invariant($"{_conversionViewModel.EstimatedMakerNetworkFeeInBase:(0.00$)}"),
                AppResources.TotalNetworkFeeLabel,
                FormattableString.Invariant($"{_conversionViewModel.EstimatedTotalNetworkFeeInBase:0.00$}"));

            await DisplayAlert(AppResources.NetworkFee, message, AppResources.AcceptButton);
        }

        private void BlockActions(bool flag)
        {
            SendingLoader.IsVisible = SendingLoader.IsRunning = flag;
            ConvertButton.IsEnabled = !flag;
            if (flag)
            {
                Content.Opacity = ConvertButton.Opacity = 0.5;
            }
            else
            {
                Content.Opacity = ConvertButton.Opacity = 1;
            }
        }
    }
}
