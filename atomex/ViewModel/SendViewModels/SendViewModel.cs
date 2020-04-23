using System;
using System.Globalization;
using System.Threading.Tasks;
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

        private bool _useDefaultFee;
        public bool UseDefaultFee
        {
            get => _useDefaultFee;
            set
            {
                _useDefaultFee = value;
                OnPropertyChanged(nameof(UseDefaultFee));

                if (_useDefaultFee)
                {
                    Amount = _amount; // recalculate amount and fee using default fee
                }
            }
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
                return "Address does not empty";
            }

            if (!Currency.IsValidAddress(To))
            {
                return "Invalid Address";
            }

            if (Amount <= 0)
            {
                return "Amount less than zero";
            }

            if (Fee <= 0)
            {
                return "Commission less than zero";
            }

            var isToken = Currency.FeeCurrencyName != Currency.Name;

            var feeAmount = !isToken ? Fee : 0;

            if (Amount + feeAmount > CurrencyViewModel.AvailableAmount)
            {
                return "Available funds error";
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
                //return "An error has occurred while sending transaction.",
                Log.Error(e, "Transaction send error.");
                return "Transaction send error.";
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

        public virtual async void UpdateAmount(decimal amount)
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
                OnPropertyChanged(nameof(FeeString));
            }
            else
            {
                var (maxAmount, maxFeeAmount, _) = await App.Account
                    .EstimateMaxAmountToSendAsync(Currency.Name, To, BlockchainTransactionType.Output, false);

                var availableAmount = Currency is BitcoinBasedCurrency
                    ? CurrencyViewModel.AvailableAmount
                    : maxAmount + maxFeeAmount;

                var feeAmount = Math.Max(Currency.GetFeeAmount(_fee, Currency.GetDefaultFeePrice()), maxFeeAmount);

                if (_amount + feeAmount > availableAmount)
                    _amount = Math.Max(availableAmount - feeAmount, 0);

                OnPropertyChanged(nameof(AmountString));

                if (_fee != 0)
                    Fee = _fee;
            }

            OnQuotesUpdatedEventHandler(App.QuotesProvider, EventArgs.Empty);
        }

        public virtual async void EstimateMaxAmountAndFee()
        { 
            var (maxAmount, maxFee, _) = await App.Account
                .EstimateMaxAmountToSendAsync(Currency.Name, To, BlockchainTransactionType.Output, true);
            UpdateAmount(maxAmount);
            UpdateFee(maxFee);
            OnPropertyChanged(nameof(FeeString));
        }

        public virtual async void UpdateFee(decimal fee)
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

                var feeAmount = _fee;

                if (feeAmount > estimatedFeeAmount.Value)
                {
                    var (maxAmount, maxFee, _) = await App.Account
                        .EstimateMaxAmountToSendAsync(Currency.Name, To, BlockchainTransactionType.Output, true);

                    var availableAmount = Currency is BitcoinBasedCurrency
                        ? CurrencyViewModel.AvailableAmount
                        : maxAmount + maxFee;

                    if (_amount + feeAmount > availableAmount)
                        _fee = maxFee;
                    //if (_amount + feeAmount > availableAmount)
                    //    _amount = Math.Max(availableAmount - feeAmount, 0);
                }
                else if (feeAmount < estimatedFeeAmount.Value)
                    _fee = estimatedFeeAmount.Value;

                if (_amount == 0)
                    _fee = 0;

                OnPropertyChanged(nameof(AmountString));

                OnPropertyChanged(nameof(FeeString));
            }

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

