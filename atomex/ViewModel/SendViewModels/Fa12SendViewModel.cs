using System;
using System.Globalization;
using System.Threading.Tasks;
using atomex.Resources;
using Atomex.Blockchain.Abstract;
using Atomex.MarketData.Abstract;

namespace atomex.ViewModel.SendViewModels
{
    public class Fa12SendViewModel : SendViewModel
    {

        public Fa12SendViewModel(
            CurrencyViewModel currencyViewModel)
            : base(currencyViewModel)
        {
        }

        public override bool UseDefaultFee
        {
            get => _useDefaultFee;
            set
            {
                Warning = string.Empty;

                _useDefaultFee = value;
                OnPropertyChanged(nameof(UseDefaultFee));

                Amount = _amount; // recalculate amount
            }
        }

        public override async Task UpdateAmount(decimal amount)
        {
            Warning = string.Empty;

            var availableAmount = CurrencyViewModel.AvailableAmount;
            _amount = amount;


            if (UseDefaultFee)
            {
                var (maxAmount, maxFeeAmount, _) = await App.Account
                     .EstimateMaxAmountToSendAsync(Currency.Name, To, BlockchainTransactionType.Output, 0, 0, false);

                if (_amount > maxAmount)
                {
                    if (_amount <= availableAmount)
                        Warning = string.Format(CultureInfo.InvariantCulture, AppResources.InsufficientChainFunds, Currency.FeeCurrencyName);
                    else
                        Warning = string.Format(CultureInfo.InvariantCulture, AppResources.InsufficientFunds);
                }

                var estimatedFeeAmount = _amount != 0
                        ? await App.Account.EstimateFeeAsync(Currency.Name, To, _amount, BlockchainTransactionType.Output)
                        : 0;

                OnPropertyChanged(nameof(AmountString));

                _fee = Currency.GetFeeFromFeeAmount(estimatedFeeAmount ?? Currency.GetDefaultFee(), Currency.GetDefaultFeePrice());
                OnPropertyChanged(nameof(FeeString));

            }
            else
            {
                var (maxAmount, _, _) = await App.Account
                       .EstimateMaxAmountToSendAsync(Currency.Name, To, BlockchainTransactionType.Output, 0, 0, false);

                if (_amount > maxAmount)
                {
                    if (_amount <= availableAmount)
                        Warning = string.Format(CultureInfo.InvariantCulture, AppResources.InsufficientChainFunds, Currency.FeeCurrencyName);
                    else
                        Warning = string.Format(CultureInfo.InvariantCulture, AppResources.InsufficientFunds);
                }
                OnPropertyChanged(nameof(AmountString));

                Fee = _fee;
            }

            OnQuotesUpdatedEventHandler(App.QuotesProvider, EventArgs.Empty);
        }

        public override async Task UpdateFee(decimal fee)
        {
            Warning = string.Empty;

            _fee = Math.Min(fee, Currency.GetMaximumFee());

            if (_amount == 0)
            {
                if (Currency.GetFeeAmount(_fee, Currency.GetDefaultFeePrice()) > CurrencyViewModel.AvailableAmount)
                    Warning = string.Format(CultureInfo.InvariantCulture, AppResources.InsufficientFunds);
                return;
            }

            if (!UseDefaultFee)
            {
                var availableAmount = CurrencyViewModel.AvailableAmount;

                var (maxAmount, maxAvailableFee, _) = await App.Account
                    .EstimateMaxAmountToSendAsync(Currency.Name, To, BlockchainTransactionType.Output, decimal.MaxValue, 0, false);

                var feeAmount = Currency.GetFeeAmount(_fee, Currency.GetDefaultFeePrice());

                var estimatedFeeAmount = _amount != 0
                    ? await App.Account.EstimateFeeAsync(Currency.Name, To, _amount, BlockchainTransactionType.Output)
                    : 0;

                if (_amount > maxAmount)
                {
                    if (_amount <= availableAmount)
                        Warning = string.Format(CultureInfo.InvariantCulture, AppResources.InsufficientChainFunds, Currency.FeeCurrencyName);
                    else
                        Warning = string.Format(CultureInfo.InvariantCulture, AppResources.InsufficientFunds);

                    return;
                }
                else if (estimatedFeeAmount == null || feeAmount < estimatedFeeAmount.Value)
                    Warning = string.Format(CultureInfo.InvariantCulture, AppResources.LowFees);

                if (feeAmount > maxAvailableFee)
                    Warning = string.Format(CultureInfo.InvariantCulture, AppResources.InsufficientChainFunds, Currency.FeeCurrencyName);

                OnPropertyChanged(nameof(FeeString));
            }

            OnQuotesUpdatedEventHandler(App.QuotesProvider, EventArgs.Empty);
        }

        protected override void OnQuotesUpdatedEventHandler(object sender, EventArgs args)
        {
            if (!(sender is ICurrencyQuotesProvider quotesProvider))
                return;

            var quote = quotesProvider.GetQuote(CurrencyCode, BaseCurrencyCode);
            var xtzQuote = quotesProvider.GetQuote("XTZ", BaseCurrencyCode);

            AmountInBase = Amount * (quote?.Bid ?? 0m);
            FeeInBase = Fee * (xtzQuote?.Bid ?? 0m);
        }
    }
}

