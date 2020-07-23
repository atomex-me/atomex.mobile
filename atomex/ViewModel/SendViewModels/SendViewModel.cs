using System;
using System.Globalization;
using System.Threading.Tasks;
using atomex.Resources;
using Atomex;
using Atomex.Blockchain.Abstract;
using Atomex.Core;
using Atomex.MarketData.Abstract;
using Serilog;

namespace atomex.ViewModel.SendViewModels
{
    public class SendViewModel : BaseViewModel
    {
        protected IAtomexApp App { get; set; }

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
            set { UpdateAmount(value); }
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
            set { UpdateFee(value); }
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

        public virtual decimal FeePrice => Currency.GetDefaultFeePrice();

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
                    _lowFees = false;
                    OnPropertyChanged(nameof(LowFees));
                    _insufficientFunds = false;
                    OnPropertyChanged(nameof(InsufficientFunds));
                    Amount = _amount; // recalculate amount and fee using default fee
                }
            }
        }

        protected bool _insufficientFunds = false;
        public virtual bool InsufficientFunds
        {
            get => _insufficientFunds;
            set { _insufficientFunds = value; OnPropertyChanged(nameof(InsufficientFunds)); }
        }

        protected bool _lowFees = false;
        public virtual bool LowFees
        {
            get => _lowFees;
            set { _lowFees = value; OnPropertyChanged(nameof(LowFees)); }
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
            {
                return AppResources.EmptyAddressError;
            }

            if (!Currency.IsValidAddress(To))
            {
                return AppResources.InvalidAddressError;
            }

            if (Amount <= 0)
            {
                return AppResources.AmountLessThanZeroError;
            }

            if (Fee <= 0)
            {
                return AppResources.CommissionLessThanZeroError;
            }

            var isToken = Currency.FeeCurrencyName != Currency.Name;

            var feeAmount = !isToken ? Fee : 0;

            if (Amount + feeAmount > CurrencyViewModel.AvailableAmount)
            {
                return AppResources.AvailableFundsError;
            }

            return null;
        }

        public async Task<string> Send()
        {
            var account = App.Account;

            try
            {
                var error = await account
                    .SendAsync(Currency.Name, To, Amount, Fee, FeePrice, UseDefaultFee);

                if (error != null)
                {
                    return error.Description;
                }

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
            App = currencyViewModel.GetAtomexApp();

            CurrencyViewModel = currencyViewModel;
            Currency = currencyViewModel.Currency;

            UseDefaultFee = true; // use default fee by default

            SubscribeToServices();
        }
        private void SubscribeToServices()
        {
            if (App.HasQuotesProvider)
                App.QuotesProvider.QuotesUpdated += OnQuotesUpdatedEventHandler;
        }

        public virtual async Task UpdateAmount(decimal amount)
        {
            _amount = amount;

            if (UseDefaultFee)
            {
                var (maxAmount, maxFeeAmount, _) = await App.Account
                    .EstimateMaxAmountToSendAsync(Currency.Name, To, BlockchainTransactionType.Output, 0, 0, true);

                if (_amount > maxAmount)
                {
                    _insufficientFunds = true;
                    OnPropertyChanged(nameof(InsufficientFunds));
                    return;
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
                var (maxAmount, maxFeeAmount, _) = await App.Account
                    .EstimateMaxAmountToSendAsync(Currency.Name, To, BlockchainTransactionType.Output, 0, 0, false);

                var availableAmount = Currency is BitcoinBasedCurrency
                    ? CurrencyViewModel.AvailableAmount
                    : maxAmount + maxFeeAmount;

                var feeAmount = Currency.GetFeeAmount(_fee, Currency.GetDefaultFeePrice());

                if (_amount > maxAmount || _amount + feeAmount > availableAmount)
                {
                    _insufficientFunds = true;
                    OnPropertyChanged(nameof(InsufficientFunds));
                    return;
                }

                OnPropertyChanged(nameof(AmountString));

                Fee = _fee;
            }

            OnQuotesUpdatedEventHandler(App.QuotesProvider, EventArgs.Empty);
        }

        public virtual async Task UpdateFee(decimal fee)
        {
            _fee = Math.Min(fee, Currency.GetMaximumFee());

            if (_amount == 0)
            {
                if (Currency.GetFeeAmount(_fee, Currency.GetDefaultFeePrice()) > CurrencyViewModel.AvailableAmount)
                {
                    _insufficientFunds = true;
                    OnPropertyChanged(nameof(InsufficientFunds));
                }
                return;
            }

            if (!UseDefaultFee)
            {
                var estimatedFeeAmount = _amount != 0
                    ? await App.Account.EstimateFeeAsync(Currency.Name, To, _amount, BlockchainTransactionType.Output)
                    : 0;

                var (maxAmount, maxFeeAmount, _) = await App.Account
                        .EstimateMaxAmountToSendAsync(Currency.Name, To, BlockchainTransactionType.Output, 0, 0, false);

                var availableAmount = Currency is BitcoinBasedCurrency
                    ? CurrencyViewModel.AvailableAmount
                    : maxAmount + maxFeeAmount;

                var feeAmount = Currency.GetFeeAmount(_fee, Currency.GetDefaultFeePrice());

                if (_amount + feeAmount > availableAmount)
                {
                    _insufficientFunds = true;
                    OnPropertyChanged(nameof(InsufficientFunds));
                    return;
                }
                else if (estimatedFeeAmount == null || feeAmount < estimatedFeeAmount.Value)
                {
                    _lowFees = true;
                    OnPropertyChanged(nameof(LowFees));
                    return;
                }

                _insufficientFunds = false;
                OnPropertyChanged(nameof(InsufficientFunds));
                _lowFees = false;
                OnPropertyChanged(nameof(LowFees));
                OnPropertyChanged(nameof(FeeString));
            }

            OnQuotesUpdatedEventHandler(App.QuotesProvider, EventArgs.Empty);
        }

        public virtual async Task OnMaxClick()
        {
            if (CurrencyViewModel.AvailableAmount == 0)
                return;

            if (UseDefaultFee)
            {
                var (maxAmount, maxFeeAmount, _) = await App.Account
                    .EstimateMaxAmountToSendAsync(Currency.Name, To, BlockchainTransactionType.Output, 0, 0, true);

                if (maxAmount > 0)
                    _amount = maxAmount;

                OnPropertyChanged(nameof(AmountString));

                _fee = Currency.GetFeeFromFeeAmount(maxFeeAmount, Currency.GetDefaultFeePrice());
                OnPropertyChanged(nameof(FeeString));
            }
            else
            {
                var (maxAmount, maxFeeAmount, _) = await App.Account
                    .EstimateMaxAmountToSendAsync(Currency.Name, To, BlockchainTransactionType.Output, 0, 0, false);

                var availableAmount = Currency is BitcoinBasedCurrency
                    ? CurrencyViewModel.AvailableAmount
                    : maxAmount + maxFeeAmount;

                var feeAmount = Currency.GetFeeAmount(_fee, Currency.GetDefaultFeePrice());

                if (availableAmount - feeAmount > 0)
                {
                    _amount = availableAmount - feeAmount;

                    var estimatedFeeAmount = _amount != 0
                        ? await App.Account.EstimateFeeAsync(Currency.Name, To, _amount, BlockchainTransactionType.Output)
                        : 0;

                    if (estimatedFeeAmount == null || feeAmount < estimatedFeeAmount.Value)
                    {
                        _lowFees = true;
                        OnPropertyChanged(nameof(LowFees));
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
                    _insufficientFunds = true;
                    OnPropertyChanged(nameof(InsufficientFunds));
                }

                OnPropertyChanged(nameof(AmountString));

                OnPropertyChanged(nameof(FeeString));
            }

            _insufficientFunds = false;
            OnPropertyChanged(nameof(InsufficientFunds));

            OnQuotesUpdatedEventHandler(App.QuotesProvider, EventArgs.Empty);
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

