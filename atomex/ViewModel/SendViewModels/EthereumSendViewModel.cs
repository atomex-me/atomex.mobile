using System;
using System.Globalization;
using Atomex;
using Atomex.Blockchain.Abstract;
using Atomex.Core;
using Atomex.MarketData.Abstract;
using Serilog;


namespace atomex.ViewModel.SendViewModels
{
    public class EthereumSendViewModel : SendViewModel
    {
        public override Currency Currency
        {
            get => _currency;
            set
            {

                _currency = value;
                OnPropertyChanged(nameof(Currency));

                _amount = 0;
                OnPropertyChanged(nameof(AmountString));

                _fee = 0;
                OnPropertyChanged(nameof(FeeString));

                _feePrice = 0;
                OnPropertyChanged(nameof(FeePriceString));

                OnPropertyChanged(nameof(TotalFeeString));

                FeePriceFormat = _currency.FeePriceFormat;
                FeePriceCode = _currency.FeePriceCode;

            }
        }

        protected string FeePriceFormat { get; set; }
        public virtual string TotalFeeCurrencyCode => CurrencyCode;

        public virtual string GasCode => "GAS";
        public virtual string GasFormat => "F0";

        public virtual string GasString
        {
            get => Fee.ToString(GasFormat, CultureInfo.InvariantCulture);
            set
            {
                if (!decimal.TryParse(value, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var fee))
                    return;

                //Fee = fee.TruncateByFormat(GasFormat);
                Fee = fee;
            }
        }


        protected decimal _feePrice;
        public virtual decimal FeePrice
        {
            get => _feePrice;
            set
            {
                _feePrice = value;

                if (!UseDefaultFee)
                {
                    var feeAmount = Currency.GetFeeAmount(_fee, _feePrice);

                    if (_amount + feeAmount > CurrencyViewModel.AvailableAmount)
                        feeAmount = Math.Max(CurrencyViewModel.AvailableAmount - _amount, 0);

                    _feePrice = Currency.GetFeePriceFromFeeAmount(feeAmount, _fee);

                    OnPropertyChanged(nameof(FeePriceString));

                    OnPropertyChanged(nameof(TotalFeeString));
                }

                OnQuotesUpdatedEventHandler(App.QuotesProvider, EventArgs.Empty);
            }
        }

        public virtual string FeePriceString
        {
            get => FeePrice.ToString(FeePriceFormat, CultureInfo.InvariantCulture);
            set
            {
                if (!decimal.TryParse(value, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var gasPrice))
                    return;

                //FeePrice = gasPrice.TruncateByFormat(FeePriceFormat);
                FeePrice = gasPrice;
            }
        }

        public virtual string TotalFeeString
        {
            get => Currency
                .GetFeeAmount(_fee, _feePrice)
                .ToString(FeeCurrencyFormat, CultureInfo.InvariantCulture);
        }

        protected string _feePriceCode;
        public string FeePriceCode
        {
            get => _feePriceCode;
            set { _feePriceCode = value; OnPropertyChanged(nameof(FeePriceCode)); }
        }


        public EthereumSendViewModel(CurrencyViewModel currencyViewModel) : base(currencyViewModel)
        {
        }

        public override async void UpdateAmount(decimal amount)
        {
            var previousAmount = _amount;
            _amount = amount;

            if (UseDefaultFee)
            {
                var (maxAmount, maxFeeAmount, _) = await App.Account
                    .EstimateMaxAmountToSendAsync(Currency.Name, To, BlockchainTransactionType.Output, true);

                var availableAmount = Currency is BitcoinBasedCurrency
                    ? CurrencyViewModel.AvailableAmount
                    : maxAmount + maxFeeAmount;

                var estimatedFeeAmount = _amount != 0
                    ? (_amount < availableAmount
                        ? await App.Account.EstimateFeeAsync(Currency.Name, To, _amount, BlockchainTransactionType.Output)
                        : null)
                    : 0;

                if (estimatedFeeAmount == null)
                {
                    if (maxAmount > 0)
                    {
                        _amount = maxAmount;
                        estimatedFeeAmount = maxFeeAmount;
                    }
                    else
                    {
                        _amount = previousAmount;
                        return;
                    }
                }

                if (_amount + estimatedFeeAmount.Value > availableAmount)
                    _amount = Math.Max(availableAmount - estimatedFeeAmount.Value, 0);

                if (_amount == 0)
                    estimatedFeeAmount = 0;

                OnPropertyChanged(nameof(AmountString));

                _fee = Currency.GetFeeFromFeeAmount(estimatedFeeAmount.Value, Currency.GetDefaultFeePrice());
                OnPropertyChanged(nameof(GasString));

                _feePrice = Currency.GetDefaultFeePrice();
                OnPropertyChanged(nameof(FeePriceString));

                OnPropertyChanged(nameof(TotalFeeString));
            }
            else
            {
                var (maxAmount, maxFeeAmount, _) = await App.Account
                    .EstimateMaxAmountToSendAsync(Currency.Name, To, BlockchainTransactionType.Output, false);

                var availableAmount = Currency is BitcoinBasedCurrency
                    ? CurrencyViewModel.AvailableAmount
                    : maxAmount + maxFeeAmount;

                var feeAmount = Math.Max(Currency.GetFeeAmount(_fee, _feePrice), maxFeeAmount);

                if (_amount + feeAmount > availableAmount)
                    _amount = Math.Max(availableAmount - feeAmount, 0);

                OnPropertyChanged(nameof(AmountString));

                if (_fee != 0)
                    Fee = _fee;

                OnPropertyChanged(nameof(TotalFeeString));

            }

            OnQuotesUpdatedEventHandler(App.QuotesProvider, EventArgs.Empty);
        }

        public override async void UpdateFee(decimal fee)
        {
            if (_amount == 0)
            {
                _fee = 0;
                return;
            }

            _fee = Math.Min(fee, Currency.GetMaximumFee());

            if (!UseDefaultFee)
            {
                var estimatedFeeAmount = _amount != 0
                    ? await App.Account.EstimateFeeAsync(Currency.Name, To, _amount, BlockchainTransactionType.Output)
                    : 0;

                var feeAmount = Currency.GetFeeAmount(_fee, _feePrice);

                if (feeAmount > estimatedFeeAmount.Value)
                {
                    var (maxAmount, maxFee, _) = await App.Account
                        .EstimateMaxAmountToSendAsync(Currency.Name, To, BlockchainTransactionType.Output, true);

                    var availableAmount = Currency is BitcoinBasedCurrency
                        ? CurrencyViewModel.AvailableAmount
                        : maxAmount + maxFee;

                    if (_amount + feeAmount > availableAmount)
                        _amount = Math.Max(availableAmount - feeAmount, 0);
                }
                else if (feeAmount < estimatedFeeAmount.Value)
                    _fee = Currency.GetFeeFromFeeAmount(estimatedFeeAmount.Value, Currency.GetDefaultFeePrice());

                if (_amount == 0)
                    _fee = 0;

                OnPropertyChanged(nameof(AmountString));
                OnPropertyChanged(nameof(GasString));
                OnPropertyChanged(nameof(TotalFeeString));

            }

            OnQuotesUpdatedEventHandler(App.QuotesProvider, EventArgs.Empty);
        }

        public override string OnNextCommand()
        {
            if (string.IsNullOrEmpty(To))
            {
                return "Resources.SvEmptyAddressError";
            }

            if (!Currency.IsValidAddress(To))
            {
                return "Resources.SvInvalidAddressError";
                
            }

            if (Amount <= 0)
            {
                return "Resources.SvAmountLessThanZeroError";
            }

            if (Fee <= 0)
            {
                return "Resources.SvCommissionLessThanZeroError";
            }

            var isToken = Currency.FeeCurrencyName != Currency.Name;

            var feeAmount = !isToken ? Currency.GetFeeAmount(Fee, FeePrice) : 0;

            if (Amount + feeAmount > CurrencyViewModel.AvailableAmount)
            {
                return "Resources.SvAvailableFundsError";
            }

            return null;
        }

        protected override void OnQuotesUpdatedEventHandler(object sender, EventArgs args)
        {
            if (!(sender is ICurrencyQuotesProvider quotesProvider))
                return;

            var quote = quotesProvider.GetQuote(CurrencyCode, BaseCurrencyCode);

            AmountInBase = Amount * (quote?.Bid ?? 0m);
            FeeInBase = Currency.GetFeeAmount(Fee, FeePrice) * (quote?.Bid ?? 0m);
        }
    }
}

