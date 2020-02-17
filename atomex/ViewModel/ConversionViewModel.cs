using System.Collections.Generic;
using System.Linq;
using Atomex;
using Atomex.Core;
using System.Threading.Tasks;
using Atomex.Common;
using System;
using Atomex.Blockchain.Abstract;
using Atomex.MarketData;
using Atomex.Subsystems.Abstract;
using Serilog;
using Xamarin.Forms;
using Atomex.MarketData.Abstract;
using Atomex.Subsystems;
using System.Globalization;

namespace atomex.ViewModel
{
    public class ConversionViewModel : BaseViewModel
    {
        private List<Currency> _coreCurrencies;

        private IAtomexApp _app;

        private string BASE_CURRENCY_CODE = "USD";

        private List<CurrencyViewModel> _currencyViewModels;

        private List<CurrencyViewModel> _fromCurrencies;
        public List<CurrencyViewModel> FromCurrencies
        {
            get => _fromCurrencies;
            private set { _fromCurrencies = value; OnPropertyChanged(nameof(FromCurrencies)); }
        }

        private List<CurrencyViewModel> _toCurrencies;
        public List<CurrencyViewModel> ToCurrencies
        {
            get => _toCurrencies;
            private set
            {
                _toCurrencies = value;
                OnPropertyChanged(nameof(ToCurrencies));
            }
        }

        private CurrencyViewModel _fromCurrency;
        public CurrencyViewModel FromCurrency
        {
            get { return _fromCurrency; }
            set
            {
                _fromCurrency = value;
                OnPropertyChanged(nameof(FromCurrency));

                if (_fromCurrency == null)
                    return;

                var oldToCurrency = ToCurrency;

                ToCurrencies = _currencyViewModels
                    .Where(c => _app.Account.Symbols.SymbolByCurrencies(c.Currency, _fromCurrency.Currency) != null)
                    .ToList();

                if (oldToCurrency != null && oldToCurrency != _fromCurrency &&
                    ToCurrencies.FirstOrDefault(c => c.Name == oldToCurrency.Name) != null)
                {
                    ToCurrency = oldToCurrency;
                }
                else
                {
                    ToCurrency = ToCurrencies.First();
                }

                //CurrencyCode = _fromCurrency?.Currency.Name;
                //BaseCurrencyCode = "USD";

                //var symbol = _app.Account.Symbols.SymbolByCurrencies(FromCurrency.Currency, ToCurrency.Currency);
                //if (symbol != null)
                //    PriceFormat = $"F{symbol.Quote.Digits}";

                Amount = 0;
            }
        }

        private CurrencyViewModel _toCurrency;
        public CurrencyViewModel ToCurrency
        {
            get { return _toCurrency; }
            set
            {
                _toCurrency = value;
                OnPropertyChanged(nameof(ToCurrency));
                //var symbol = _app.Account.Symbols.SymbolByCurrencies(FromCurrency.Currency, ToCurrency.Currency);
                //if (symbol != null)
                //    PriceFormat = $"F{symbol.Quote.Digits}";
                OnQuotesUpdatedEventHandler(_app.Terminal, null);
                OnBaseQuotesUpdatedEventHandler(_app.QuotesProvider, EventArgs.Empty);
            }
        }

        private decimal _amount;
        public decimal Amount
        {
            get => _amount;
            set
            {
                _amount = value;

                var (maxAmount, maxFee, _) = _app.Account
                    .EstimateMaxAmountToSendAsync(FromCurrency.Name, null, BlockchainTransactionType.SwapPayment)
                    .WaitForResult();

                var availableAmount = FromCurrency.Currency is BitcoinBasedCurrency
                    ? FromCurrency.AvailableAmount
                    : maxAmount + maxFee;

                var estimatedPaymentFee = _amount != 0
                    ? (_amount < availableAmount
                        ? _app.Account
                            .EstimateFeeAsync(FromCurrency.Name, null, _amount, BlockchainTransactionType.SwapPayment)
                            .WaitForResult()
                        : null)
                    : 0;

                if (estimatedPaymentFee != null)
                {
                    _estimatedPaymentFee = estimatedPaymentFee.Value;
                    _estimatedRedeemFee = ToCurrency.Currency.GetDefaultRedeemFee();

                    if (ToCurrency.AvailableAmount == 0 && !(ToCurrency.Currency is BitcoinBasedCurrency))
                    {
                        _estimatedRedeemFee *= 2;
                    }

                    if (_amount + _estimatedPaymentFee > availableAmount)
                        _amount = Math.Max(availableAmount - _estimatedPaymentFee, 0);

                    OnQuotesUpdatedEventHandler(_app.Terminal, null);
                    OnBaseQuotesUpdatedEventHandler(_app.QuotesProvider, EventArgs.Empty);
                }
                else
                {
                    _targetAmount = _estimatedPaymentFee = _estimatedPrice = 0;
                    OnPropertyChanged(nameof(TargetAmount));
                    OnPropertyChanged(nameof(EstimatedPrice));
                }

                OnPropertyChanged(nameof(Amount));
                OnPropertyChanged(nameof(EstimatedPaymentFee));
                OnPropertyChanged(nameof(EstimatedRedeemFee));
            }
        }
        private decimal _amountInBase;
        public decimal AmountInBase
        {
            get => _amountInBase;
            set { _amountInBase = value; OnPropertyChanged(nameof(AmountInBase)); }
        }

        private decimal _targetAmount;
        public decimal TargetAmount
        {
            get => _targetAmount; set { _targetAmount = value; OnPropertyChanged(nameof(TargetAmount)); }
        }
        private decimal _targetAmountInBase;
        public decimal TargetAmountInBase
        {
            get => _targetAmountInBase;
            set { _targetAmountInBase = value; OnPropertyChanged(nameof(TargetAmountInBase)); }
        }
        private decimal _estimatedPrice;
        public decimal EstimatedPrice
        {
            get => _estimatedPrice; set { _estimatedPrice = value; OnPropertyChanged(nameof(EstimatedPrice)); }
        }
        private decimal _estimatedPaymentFee;
        public decimal EstimatedPaymentFee
        {
            get => _estimatedPaymentFee; set { _estimatedPaymentFee = value; OnPropertyChanged(nameof(EstimatedPaymentFee)); }
        }
        private decimal _estimatedPaymentFeeInBase;
        public decimal EstimatedPaymentFeeInBase
        {
            get => _estimatedPaymentFeeInBase;
            set { _estimatedPaymentFeeInBase = value; OnPropertyChanged(nameof(EstimatedPaymentFeeInBase)); }
        }
        private decimal _estimatedRedeemFee;
        public decimal EstimatedRedeemFee
        {
            get => _estimatedRedeemFee;
            set { _estimatedRedeemFee = value; OnPropertyChanged(nameof(EstimatedRedeemFee)); }
        }
        private decimal _estimatedRedeemFeeInBase;
        public decimal EstimatedRedeemFeeInBase
        {
            get => _estimatedRedeemFeeInBase;
            set { _estimatedRedeemFeeInBase = value; OnPropertyChanged(nameof(EstimatedRedeemFeeInBase)); }
        }
        private decimal _estimatedMaxAmount;
        public decimal EstimatedMaxAmount
        {
            get => _estimatedMaxAmount;
            set { _estimatedMaxAmount = value; OnPropertyChanged(nameof(EstimatedMaxAmount)); }
        }
        private string _priceFormat;
        public string PriceFormat
        {
            get => _priceFormat;
            set { _priceFormat = value; OnPropertyChanged(nameof(PriceFormat)); }
        }




        public ConversionViewModel(IAtomexApp app)
        {
            _app = app;
            _coreCurrencies = app.Account.Currencies.ToList();
            FromCurrencies = ToCurrencies = _currencyViewModels = new List<CurrencyViewModel>();
            FillCurrenciesAsync().FireAndForget();
            SubscribeToServices();
        }

        private void SubscribeToServices()
        {
            _app.TerminalChanged += OnTerminalChangedEventHandler;

            if (_app.HasQuotesProvider)
                _app.QuotesProvider.QuotesUpdated += OnBaseQuotesUpdatedEventHandler;
        }

        private void OnTerminalChangedEventHandler(object sender, TerminalChangedEventArgs args)
        {
            var terminal = args.Terminal;

            if (terminal?.Account == null)
                return;

            terminal.QuotesUpdated += OnQuotesUpdatedEventHandler;

            // Get swaps
            //terminal.SwapUpdated += OnSwapEventHandler;
            //OnSwapEventHandler(this, null);
        }

        private async Task FillCurrenciesAsync()
        {
            await Task.WhenAll(_coreCurrencies.Select(async c =>
            {
                var balance = await _app.Account.GetBalanceAsync(c.Name);
                var address = await _app.Account.GetFreeExternalAddressAsync(c.Name);
                _currencyViewModels.Add(new CurrencyViewModel(_app)
                {
                    Currency = c,
                    AvailableAmount = balance.Available,
                    Name = c.Name,
                    FullName = c.Description,
                    Address = address.Address
                });
                FromCurrency = _currencyViewModels.FirstOrDefault();
            }));
        }

        public async Task<(decimal, decimal, decimal)> EstimateMaxAmount()
        {
            return await _app.Account
               .EstimateMaxAmountToSendAsync(FromCurrency.Name, null, BlockchainTransactionType.SwapPayment)
               .ConfigureAwait(false);
        }

        private void OnBaseQuotesUpdatedEventHandler(object sender, EventArgs args)
        {
            if (!(sender is ICurrencyQuotesProvider provider))
                return;

            if (FromCurrency == null || ToCurrency == null)
                return;

            var fromCurrencyPrice = provider.GetQuote(FromCurrency.Currency.Name, BASE_CURRENCY_CODE)?.Bid ?? 0m;
            AmountInBase = _amount * fromCurrencyPrice;
            EstimatedPaymentFeeInBase = _estimatedPaymentFee * fromCurrencyPrice;

            var toCurrencyPrice = provider.GetQuote(ToCurrency.Currency.Name, BASE_CURRENCY_CODE)?.Bid ?? 0m;
            EstimatedRedeemFeeInBase = _estimatedRedeemFee * toCurrencyPrice;

            UpdateTargetAmountInBase(provider);
        }

        private async void OnQuotesUpdatedEventHandler(object sender, MarketDataEventArgs args)
        {
            try
            {
                if (!(sender is ITerminal terminal))
                    return;

                if (ToCurrency == null)
                    return;

                var symbol = _app.Account.Symbols.SymbolByCurrencies(FromCurrency.Currency, ToCurrency.Currency);
                if (symbol == null)
                    return;

                var side = symbol.OrderSideForBuyCurrency(ToCurrency.Currency);
                var orderBook = terminal.GetOrderBook(symbol);

                if (orderBook == null)
                    return;

                _estimatedPrice = orderBook.EstimatedDealPrice(side, Amount);
                _estimatedMaxAmount = orderBook.EstimateMaxAmount(side);

                //_isNoLiquidity = Amount != 0 && _estimatedPrice == 0;

                if (symbol.IsBaseCurrency(ToCurrency.Currency))
                {
                    _targetAmount = _estimatedPrice != 0
                        ? Amount / _estimatedPrice
                        : 0m;
                }
                else if (symbol.IsQuoteCurrency(ToCurrency.Currency))
                {
                    _targetAmount = Amount * _estimatedPrice;
                }


                await Device.InvokeOnMainThreadAsync(() =>
                {
                    OnPropertyChanged(nameof(EstimatedPrice));
                    OnPropertyChanged(nameof(EstimatedMaxAmount));
                    OnPropertyChanged(nameof(TargetAmount));
                    OnPropertyChanged(nameof(PriceFormat));
                    UpdateTargetAmountInBase(_app.QuotesProvider);
                });
            }
            catch (Exception e)
            {
                Log.Error(e, "Quotes updated event handler error");
            }
        }
        private void UpdateTargetAmountInBase(ICurrencyQuotesProvider provider)
        {
            if (provider == null)
                throw new ArgumentNullException(nameof(provider));

            if (ToCurrency == null)
                return;

            var quote = provider.GetQuote(ToCurrency.Currency.Name, BASE_CURRENCY_CODE);

            TargetAmountInBase = _targetAmount * (quote?.Bid ?? 0m);
        }
    }
}
