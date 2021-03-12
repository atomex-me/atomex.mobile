using System;
using System.Globalization;
using System.Threading.Tasks;
using atomex.Resources;
using Atomex;
using Atomex.Blockchain.Abstract;
using Atomex.Common;
using Atomex.Core;
using Atomex.MarketData.Abstract;
using Serilog;

namespace atomex.ViewModel.SendViewModels
{
    public class SendViewModel : BaseViewModel
    {
        protected IAtomexApp AtomexApp { get; set; }

        protected Currency _currency;
        public virtual Currency Currency
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

            }
        }

        protected CurrencyViewModel _currencyViewModel;
        public virtual CurrencyViewModel CurrencyViewModel
        {
            get => _currencyViewModel;
            set
            {
                _currencyViewModel = value;
                CurrencyCode = _currencyViewModel?.CurrencyCode;
            }
        }

        protected string _to;
        public virtual string To
        {
            get => _to;
            set
            {
                _to = value;
                OnPropertyChanged(nameof(To));
                Warning = string.Empty;
            }
        }

        protected string CurrencyFormat { get; set; }
        protected string FeeCurrencyFormat { get; set; }

        private string _baseCurrencyFormat;
        public virtual string BaseCurrencyFormat
        {
            get => _baseCurrencyFormat;
            set { _baseCurrencyFormat = value; OnPropertyChanged(nameof(BaseCurrencyFormat)); }
        }

        protected decimal _amount;
        public decimal Amount
        {
            get => _amount;
            set { _ = UpdateAmount(value); }
        }

        public string AmountString
        {
            get => Amount.ToString(CurrencyFormat, CultureInfo.InvariantCulture);
            set
            {
                if (!decimal.TryParse(value, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var amount))
                    return;

                Amount = amount;
            }
        }

        protected decimal _fee;
        public decimal Fee
        {
            get => _fee;
            set { _ = UpdateFee(value); }
        }

        public virtual string FeeString
        {
            get => Fee.ToString(FeeCurrencyFormat, CultureInfo.InvariantCulture);
            set
            {
                if (!decimal.TryParse(value, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var fee))
                    return;

                Fee = fee;
            }
        }

        protected decimal _feePrice;
        public virtual decimal FeePrice
        {
            get => _feePrice;
            set { _feePrice = value; OnPropertyChanged(nameof(FeePrice)); }
        }

        protected bool _useDefaultFee;
        public virtual bool UseDefaultFee
        {
            get => _useDefaultFee;
            set
            {
                _useDefaultFee = value;
                OnPropertyChanged(nameof(UseDefaultFee));

                if (_useDefaultFee)
                {
                    Warning = string.Empty;
                    Amount = _amount; // recalculate amount and fee using default fee
                }
            }
        }

        protected string _warning = string.Empty;
        public string Warning
        {
            get => _warning;
            set { _warning = value; OnPropertyChanged(nameof(Warning)); }
        }

        protected decimal _amountInBase;
        public decimal AmountInBase
        {
            get => _amountInBase;
            set { _amountInBase = value; OnPropertyChanged(nameof(AmountInBase)); }
        }

        protected decimal _feeInBase;
        public decimal FeeInBase
        {
            get => _feeInBase;
            set { _feeInBase = value; OnPropertyChanged(nameof(FeeInBase)); }
        }

        protected string _currencyCode;
        public string CurrencyCode
        {
            get => _currencyCode;
            set { _currencyCode = value; OnPropertyChanged(nameof(CurrencyCode)); }
        }

        protected string _feeCurrencyCode;
        public string FeeCurrencyCode
        {
            get => _feeCurrencyCode;
            set { _feeCurrencyCode = value; OnPropertyChanged(nameof(FeeCurrencyCode)); }
        }

        protected string _baseCurrencyCode = "USD";
        public string BaseCurrencyCode
        {
            get => _baseCurrencyCode;
            set { _baseCurrencyCode = value; OnPropertyChanged(nameof(BaseCurrencyCode)); }
        }

        public virtual string OnNextCommand()
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

            var feeAmount = !isToken ? Fee : 0;

            if (Amount + feeAmount > CurrencyViewModel.AvailableAmount)
                return AppResources.AvailableFundsError;

            if (!string.IsNullOrEmpty(Warning))
                return AppResources.FailedToSend;

            return null;
        }

        public async Task<string> Send()
        {
            var account = AtomexApp.Account;

            try
            {
                var error = await account
                    .SendAsync(Currency.Name, To, Amount, Fee, FeePrice, UseDefaultFee);

                if (error != null)
                    return error.Description;

                return null;
            }
            catch (Exception e)
            {
                Log.Error(e, "Transaction send error.");
                return AppResources.SendingTransactionError;
            }
        }


        public SendViewModel(CurrencyViewModel currencyViewModel)
        {
            AtomexApp = currencyViewModel.GetAtomexApp() ?? throw new ArgumentNullException(nameof(AtomexApp));

            CurrencyViewModel = currencyViewModel;
            Currency = currencyViewModel.Currency;

            UseDefaultFee = true; // use default fee by default

            _ = UpdateFeePrice();

            SubscribeToServices();
        }

        public virtual async Task UpdateFeePrice()
        {
            try
            {
                FeePrice = await Currency.GetDefaultFeePriceAsync();
            }
            catch (Exception e)
            {
                Log.Error(e, "Update fee price error");
            }
        }

        private void SubscribeToServices()
        {
            if (AtomexApp.HasQuotesProvider)
                AtomexApp.QuotesProvider.QuotesUpdated += OnQuotesUpdatedEventHandler;
        }

        public virtual async Task UpdateAmount(decimal amount)
        {
            Warning = string.Empty;

            _amount = amount;

            var defaultFeePrice = await Currency.GetDefaultFeePriceAsync();

            if (UseDefaultFee)
            {
                var (maxAmount, maxFeeAmount, _) = await AtomexApp.Account
                    .EstimateMaxAmountToSendAsync(Currency.Name, To, BlockchainTransactionType.Output, 0, 0, true);

                if (_amount > maxAmount)
                {
                    Warning = string.Format(CultureInfo.InvariantCulture, AppResources.InsufficientFunds);
                    return;
                }

                var estimatedFeeAmount = _amount != 0
                        ? await AtomexApp.Account.EstimateFeeAsync(Currency.Name, To, _amount, BlockchainTransactionType.Output)
                        : 0;

                OnPropertyChanged(nameof(AmountString));

                _fee = Currency.GetFeeFromFeeAmount(estimatedFeeAmount ?? Currency.GetDefaultFee(), defaultFeePrice);
                OnPropertyChanged(nameof(FeeString));
            }
            else
            {
                var (maxAmount, maxFeeAmount, _) = await AtomexApp.Account
                    .EstimateMaxAmountToSendAsync(Currency.Name, To, BlockchainTransactionType.Output, 0, 0, false);

                var availableAmount = Currency is BitcoinBasedCurrency
                    ? CurrencyViewModel.AvailableAmount
                    : maxAmount + maxFeeAmount;

                var feeAmount = Currency.GetFeeAmount(_fee, defaultFeePrice);

                if (_amount > maxAmount || _amount + feeAmount > availableAmount)
                {
                    Warning = string.Format(CultureInfo.InvariantCulture, AppResources.InsufficientFunds);
                    return;
                }

                OnPropertyChanged(nameof(AmountString));

                Fee = _fee;
            }

            OnQuotesUpdatedEventHandler(AtomexApp.QuotesProvider, EventArgs.Empty);
        }

        public virtual async Task UpdateFee(decimal fee)
        {
            Warning = string.Empty;

            _fee = Math.Min(fee, Currency.GetMaximumFee());

            var defaultFeePrice = await Currency.GetDefaultFeePriceAsync();

            if (_amount == 0)
            {
                if (Currency.GetFeeAmount(_fee, defaultFeePrice) > CurrencyViewModel.AvailableAmount)
                    Warning = string.Format(CultureInfo.InvariantCulture, AppResources.InsufficientFunds);
                
                return;
            }

            if (!UseDefaultFee)
            {
                var estimatedFeeAmount = _amount != 0
                    ? await AtomexApp.Account.EstimateFeeAsync(Currency.Name, To, _amount, BlockchainTransactionType.Output)
                    : 0;

                var (maxAmount, maxFeeAmount, _) = await AtomexApp.Account
                        .EstimateMaxAmountToSendAsync(Currency.Name, To, BlockchainTransactionType.Output, 0, 0, false);

                var availableAmount = Currency is BitcoinBasedCurrency
                    ? CurrencyViewModel.AvailableAmount
                    : maxAmount + maxFeeAmount;

                var feeAmount = Currency.GetFeeAmount(_fee, defaultFeePrice);

                if (_amount + feeAmount > availableAmount)
                {
                    Warning = string.Format(CultureInfo.InvariantCulture, AppResources.InsufficientFunds);
                    return;
                }
                else if (estimatedFeeAmount == null || feeAmount < estimatedFeeAmount.Value)
                {
                    Warning = string.Format(CultureInfo.InvariantCulture, AppResources.LowFees);
                    return;
                }

                Warning = string.Empty;
                
                OnPropertyChanged(nameof(FeeString));
            }

            OnQuotesUpdatedEventHandler(AtomexApp.QuotesProvider, EventArgs.Empty);
        }

        public virtual async Task OnMaxClick()
        {
            Warning = string.Empty;

            if (CurrencyViewModel.AvailableAmount == 0)
                return;

            var defaultFeePrice = await Currency.GetDefaultFeePriceAsync();

            if (UseDefaultFee)
            {
                var (maxAmount, maxFeeAmount, _) = await AtomexApp.Account
                    .EstimateMaxAmountToSendAsync(Currency.Name, To, BlockchainTransactionType.Output, 0, 0, true);

                if (maxAmount > 0)
                    _amount = maxAmount;

                OnPropertyChanged(nameof(AmountString));

                _fee = Currency.GetFeeFromFeeAmount(maxFeeAmount, defaultFeePrice);
                OnPropertyChanged(nameof(FeeString));
            }
            else
            {
                var (maxAmount, maxFeeAmount, _) = await AtomexApp.Account
                    .EstimateMaxAmountToSendAsync(Currency.Name, To, BlockchainTransactionType.Output, 0, 0, false);

                var availableAmount = Currency is BitcoinBasedCurrency
                    ? CurrencyViewModel.AvailableAmount
                    : maxAmount + maxFeeAmount;

                var feeAmount = Currency.GetFeeAmount(_fee, defaultFeePrice);

                if (availableAmount - feeAmount > 0)
                {
                    _amount = availableAmount - feeAmount;

                    var estimatedFeeAmount = _amount != 0
                        ? await AtomexApp.Account.EstimateFeeAsync(Currency.Name, To, _amount, BlockchainTransactionType.Output)
                        : 0;

                    if (estimatedFeeAmount == null || feeAmount < estimatedFeeAmount.Value)
                    {
                        Warning = string.Format(CultureInfo.InvariantCulture, AppResources.LowFees);
                        if (_fee == 0)
                        {
                            _amount = 0;
                            OnPropertyChanged(nameof(AmountString));
                            return;
                        }
                    }
                }
                else
                {
                    _amount = 0;
                    Warning = string.Format(CultureInfo.InvariantCulture, AppResources.InsufficientFunds);
                }

                OnPropertyChanged(nameof(AmountString));

                OnPropertyChanged(nameof(FeeString));
            }

            OnQuotesUpdatedEventHandler(AtomexApp.QuotesProvider, EventArgs.Empty);
        }

        protected virtual void OnQuotesUpdatedEventHandler(object sender, EventArgs args)
        {
            if (!(sender is ICurrencyQuotesProvider quotesProvider))
                return;

            var quote = quotesProvider.GetQuote(CurrencyCode, BaseCurrencyCode);

            AmountInBase = Amount * (quote?.Bid ?? 0m);
            FeeInBase = Fee * (quote?.Bid ?? 0m);
        }
    }
}

