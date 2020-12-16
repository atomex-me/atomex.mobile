﻿using System;
using System.Globalization;
using System.Threading.Tasks;
using atomex.Resources;
using Atomex.Blockchain.Abstract;
using Atomex.Core;
using Atomex.MarketData.Abstract;


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


        //protected decimal _feePrice;
        public override decimal FeePrice
        {
            get => _feePrice;
            set { _ = UpdateFeePrice(value); }
            
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

        protected decimal _totalFee;

        private bool _isTotalFeeUpdating;
        public bool IsTotalFeeUpdating
        {
            get => _isTotalFeeUpdating;
            set { _isTotalFeeUpdating = value; OnPropertyChanged(nameof(IsTotalFeeUpdating)); }
        }

        public virtual string TotalFeeString
        {
            get => _totalFee
               .ToString(FeeCurrencyFormat, CultureInfo.InvariantCulture);
            set { UpdateTotalFeeString(); }
        }

        public override bool UseDefaultFee
        {
            get => _useDefaultFee;
            set
            {
                Warning = string.Empty;

                _useDefaultFee = value;
                OnPropertyChanged(nameof(UseDefaultFee));

                if (_useDefaultFee)
                    Amount = _amount; // recalculate amount and fee using default fee
            }
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

        public override async Task UpdateAmount(decimal amount)
        {
            Warning = string.Empty;

            _amount = amount;

            if (UseDefaultFee)
            {
                var (maxAmount, maxFeeAmount, _) = await AtomexApp.Account
                    .EstimateMaxAmountToSendAsync(Currency.Name, To, BlockchainTransactionType.Output, 0, 0, false);

                _fee = Currency.GetDefaultFee();
                OnPropertyChanged(nameof(GasString));

                _feePrice = await Currency.GetDefaultFeePriceAsync();
                OnPropertyChanged(nameof(FeePriceString));

                if (_amount > maxAmount)
                {
                    Warning = string.Format(CultureInfo.InvariantCulture, AppResources.InsufficientFunds);
                    return;
                }

                UpdateTotalFeeString();
                OnPropertyChanged(nameof(TotalFeeString));
            }
            else
            {
                var (maxAmount, maxFeeAmount, _) = await AtomexApp.Account
                      .EstimateMaxAmountToSendAsync(Currency.Name, To, BlockchainTransactionType.Output, _fee, _feePrice, false);

                if (_amount > maxAmount)
                {
                    Warning = string.Format(CultureInfo.InvariantCulture, AppResources.InsufficientFunds);
                    return;
                }

                OnPropertyChanged(nameof(AmountString));

                if (_fee < Currency.GetDefaultFee() || _feePrice == 0)
                    Warning = string.Format(CultureInfo.InvariantCulture, AppResources.LowFees);
            }

            OnQuotesUpdatedEventHandler(AtomexApp.QuotesProvider, EventArgs.Empty);
        }

        public virtual async Task UpdateFeePrice(decimal value)
        {
            Warning = string.Empty;

            _feePrice = value;

            if (_amount == 0)
            {
                if (Currency.GetFeeAmount(_fee, _feePrice) > CurrencyViewModel.AvailableAmount)
                    Warning = string.Format(CultureInfo.InvariantCulture, AppResources.InsufficientFunds);
                return;
            }

            if (value == 0)
            {
                Warning = string.Format(CultureInfo.InvariantCulture, AppResources.LowFees);
                UpdateTotalFeeString();
                OnPropertyChanged(nameof(TotalFeeString));
                return;
            }

            if (!UseDefaultFee)
            {
                var (maxAmount, maxFee, _) = await AtomexApp.Account
                    .EstimateMaxAmountToSendAsync(Currency.Name, To, BlockchainTransactionType.Output, _fee, _feePrice, false);

                if (_amount > maxAmount)
                {
                    Warning = string.Format(CultureInfo.InvariantCulture, AppResources.InsufficientFunds);
                    return;
                }

                OnPropertyChanged(nameof(FeePrice));
                OnPropertyChanged(nameof(FeePriceString));
                UpdateTotalFeeString();
                OnPropertyChanged(nameof(TotalFeeString));
            }

            OnQuotesUpdatedEventHandler(AtomexApp.QuotesProvider, EventArgs.Empty);
        }

        public override async Task UpdateFee(decimal fee)
        {
            Warning = string.Empty;

            _fee = Math.Min(fee, Currency.GetMaximumFee());

            if (_amount == 0)
            {
                if (Currency.GetFeeAmount(_fee, _feePrice) > CurrencyViewModel.AvailableAmount)
                    Warning = string.Format(CultureInfo.InvariantCulture, AppResources.InsufficientFunds);
                return;
            }

            if (_fee < Currency.GetDefaultFee())
            {
                Warning = string.Format(CultureInfo.InvariantCulture, AppResources.LowFees);
                if (fee == 0)
                {
                    UpdateTotalFeeString();
                    OnPropertyChanged(nameof(TotalFeeString));
                    return;
                }
            }

            if (!UseDefaultFee)
            {
                var (maxAmount, maxFee, _) = await AtomexApp.Account
                        .EstimateMaxAmountToSendAsync(Currency.Name, To, BlockchainTransactionType.Output, _fee, _feePrice, false);

                if (_amount > maxAmount)
                {
                    Warning = string.Format(CultureInfo.InvariantCulture, AppResources.InsufficientFunds);
                    return;
                }

                UpdateTotalFeeString();
                OnPropertyChanged(nameof(TotalFeeString));

                OnPropertyChanged(nameof(GasString));
            }

            OnQuotesUpdatedEventHandler(AtomexApp.QuotesProvider, EventArgs.Empty);
        }

        protected async void UpdateTotalFeeString(decimal totalFeeAmount = 0)
        {
            IsTotalFeeUpdating = true;

            try
            {
                var feeAmount = totalFeeAmount > 0
                    ? totalFeeAmount
                    : Currency.GetFeeAmount(_fee, _feePrice) > 0
                        ? await AtomexApp.Account.EstimateFeeAsync(Currency.Name, To, _amount, BlockchainTransactionType.Output, _fee, _feePrice)
                        : 0;

                if (feeAmount != null)
                    _totalFee = feeAmount.Value;
            }
            finally
            {
                IsTotalFeeUpdating = false;
            }
        }

        public override async Task OnMaxClick()
        {
            Warning = string.Empty;

            var availableAmount = CurrencyViewModel.AvailableAmount;
            if (availableAmount == 0)
                return;

            if (UseDefaultFee)
            {
                var (maxAmount, maxFeeAmount, _) = await AtomexApp.Account
                    .EstimateMaxAmountToSendAsync(Currency.Name, To, BlockchainTransactionType.Output, 0, 0, false);

                if (maxAmount > 0)
                    _amount = maxAmount;

                OnPropertyChanged(nameof(AmountString));

                _fee = Currency.GetDefaultFee();
                OnPropertyChanged(nameof(GasString));

                _feePrice = await Currency.GetDefaultFeePriceAsync();
                OnPropertyChanged(nameof(FeePriceString));

                UpdateTotalFeeString(maxFeeAmount);
                OnPropertyChanged(nameof(TotalFeeString));
            }
            else
            {
                if (_fee < Currency.GetDefaultFee() || _feePrice == 0)
                {
                    Warning = string.Format(CultureInfo.InvariantCulture, AppResources.LowFees);
                    if (_fee == 0 || _feePrice == 0)
                    {
                        _amount = 0;
                        OnPropertyChanged(nameof(AmountString));
                        return;
                    }
                }

                var (maxAmount, maxFeeAmount, _) = await AtomexApp.Account
                    .EstimateMaxAmountToSendAsync(Currency.Name, To, BlockchainTransactionType.Output, _fee, _feePrice, false);

                _amount = maxAmount;

                if (maxAmount == 0 && availableAmount > 0)
                    Warning = string.Format(CultureInfo.InvariantCulture, AppResources.InsufficientFunds);

                OnPropertyChanged(nameof(AmountString));
                UpdateTotalFeeString(maxFeeAmount);
                OnPropertyChanged(nameof(TotalFeeString));
            }

            OnQuotesUpdatedEventHandler(AtomexApp.QuotesProvider, EventArgs.Empty);
        }

        public override string OnNextCommand()
        {
            if (string.IsNullOrEmpty(To))
                return AppResources.EmptyAddressError;

            if (!Currency.IsValidAddress(To))
                return AppResources.InvalidAddressError;

            if (Amount <= 0)
                return AppResources.AmountLessThanZeroError;

            if (Fee <= 0)
                return AppResources.CommissionLessThanZeroError;

            var isToken = Currency.FeeCurrencyName != Currency.Name;

            var feeAmount = !isToken ? Currency.GetFeeAmount(Fee, FeePrice) : 0;

            if (Amount + feeAmount > CurrencyViewModel.AvailableAmount)
                return AppResources.AvailableFundsError;

            if (!string.IsNullOrEmpty(Warning))
                return AppResources.FailedToSend;

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

