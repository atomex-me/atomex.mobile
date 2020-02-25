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
using System.Collections.ObjectModel;
using Atomex.Swaps;

namespace atomex.ViewModel
{
    public class ConversionViewModel : BaseViewModel
    {
        private List<Currency> _coreCurrencies;

        private IAtomexApp _app;
        private ITerminal _terminal;

        private string BASE_CURRENCY_CODE = "USD";

        private static TimeSpan SWAP_TIMEOUT = TimeSpan.FromSeconds(60);

        private static TimeSpan SWAP_CHECK_INTERVAL = TimeSpan.FromSeconds(3);

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
                    _isNoLiquidity = true;
                    OnPropertyChanged(nameof(TargetAmount));
                    OnPropertyChanged(nameof(EstimatedPrice));
                    OnPropertyChanged(nameof(IsNoLiquidity));
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
        private bool _isNoLiquidity;
        public bool IsNoLiquidity
        {
            get => _isNoLiquidity;
            set { _isNoLiquidity = value; OnPropertyChanged(nameof(IsNoLiquidity)); }
        }
        private ObservableCollection<SwapViewModel> _swaps;
        public ObservableCollection<SwapViewModel> Swaps
        {
            get => _swaps;
            set { _swaps = value; OnPropertyChanged(nameof(Swaps)); }
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
            OnTerminalChangedEventHandler(this, new TerminalChangedEventArgs(_app.Terminal));

            if (_app.HasQuotesProvider)
                _app.QuotesProvider.QuotesUpdated += OnBaseQuotesUpdatedEventHandler;
        }

        private void OnTerminalChangedEventHandler(object sender, TerminalChangedEventArgs args)
        {
            var terminal = args.Terminal;

            if (_terminal != terminal && _terminal != null)
            {
                _terminal.QuotesUpdated -= OnQuotesUpdatedEventHandler;
                _terminal.SwapUpdated -= OnSwapEventHandler;
            }

            _terminal = terminal;

            if (_terminal?.Account == null)
                return;

            _terminal.QuotesUpdated += OnQuotesUpdatedEventHandler;
            _terminal.SwapUpdated += OnSwapEventHandler;

            OnSwapEventHandler(this, null);
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
            }));
            FromCurrency = _currencyViewModels.FirstOrDefault();
        }

        private async Task UpdateSwaps()
        {
            var swaps = await _app.Account.GetSwapsAsync();

            var swapViewModels = swaps
                               .Select(SwapViewModelFactory.CreateSwapViewModel)
                               .ToList()
                               .SortList((s1, s2) => s2.Time.ToUniversalTime()
                                   .CompareTo(s1.Time.ToUniversalTime()));
            Swaps = new ObservableCollection<SwapViewModel>(swapViewModels);

            await Device.InvokeOnMainThreadAsync(() =>
            {
                OnPropertyChanged(nameof(Swaps));
            });
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

                _isNoLiquidity = Amount != 0 && _estimatedPrice == 0;

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
                    OnPropertyChanged(nameof(IsNoLiquidity));
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
        public async Task<Error> ConvertAsync()
        {
            try
            {
                var account = _app.Account;

                var fromWallets = (await account
                    .GetUnspentAddressesAsync(
                        toAddress: null,
                        currency: FromCurrency.Name,
                        amount: Amount,
                        fee: 0,
                        feePrice: 0,
                        feeUsagePolicy: FeeUsagePolicy.EstimatedFee,
                        addressUsagePolicy: AddressUsagePolicy.UseMinimalBalanceFirst,
                        transactionType: BlockchainTransactionType.SwapPayment))
                    .ToList();

                if (Amount == 0)
                    return new Error(Errors.SwapError, "Amount to convert must be greater than zero");

                if (Amount > 0 && !fromWallets.Any())
                    return new Error(Errors.SwapError, "Insufficient funds");

                var symbol = _app.Account.Symbols.SymbolByCurrencies(FromCurrency.Currency, ToCurrency.Currency);
                var side = symbol.OrderSideForBuyCurrency(ToCurrency.Currency);
                var terminal = _app.Terminal;
                var price = EstimatedPrice;

                if (price == 0)
                    return new Error(Errors.NoLiquidity, "Not enough liquidity to convert a specified amount.");

                var qty = Math.Round(AmountHelper.AmountToQty(side, Amount, price), symbol.Base.Digits);

                if (qty < symbol.MinimumQty)
                {
                    var minimumAmount = Math.Round(AmountHelper.QtyToAmount(side, symbol.MinimumQty, price), FromCurrency.Currency.Digits);
                    var message = string.Format(CultureInfo.InvariantCulture, "The amount must be greater than or equal to the minimum allowed amount {0} {1}", minimumAmount, FromCurrency.Name);

                    return new Error(Errors.SwapError, message);
                }

                var order = new Order
                {
                    Symbol = symbol,
                    TimeStamp = DateTime.UtcNow,
                    Price = price,
                    Qty = qty,
                    Side = side,
                    Type = OrderType.FillOrKill,
                    FromWallets = fromWallets.ToList()
                };

                await order.CreateProofOfPossessionAsync(account);

                terminal.OrderSendAsync(order);

                // wait for swap confirmation
                var timeStamp = DateTime.UtcNow;

                while (DateTime.UtcNow < timeStamp + SWAP_TIMEOUT)
                {
                    await Task.Delay(SWAP_CHECK_INTERVAL);

                    var currentOrder = terminal.Account.GetOrderById(order.ClientOrderId);

                    if (currentOrder == null)
                        continue;

                    if (currentOrder.Status == OrderStatus.PartiallyFilled || currentOrder.Status == OrderStatus.Filled)
                        return null;

                    if (currentOrder.Status == OrderStatus.Canceled)
                        return new Error(Errors.PriceHasChanged, "Oops, the price has changed during the order sending. Please try again");

                    if (currentOrder.Status == OrderStatus.Rejected)
                        return new Error(Errors.OrderRejected, "Order rejected");
                }

                return new Error(Errors.TimeoutReached, "Atomex is not responding for a long time");
            }
            catch (Exception e)
            {
                Log.Error(e, "Conversion error");

                return new Error(Errors.SwapError, "Conversion error. Please contant technical support");
            }
        }

        private async void OnSwapEventHandler(object sender, SwapEventArgs args)
        {
            try
            {
                await UpdateSwaps();
            }
            catch (Exception e)
            {
                Log.Error(e, "Swaps update error");
            }
        }
    }
}
