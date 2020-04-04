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
using Atomex.Abstract;

namespace atomex.ViewModel
{
    public class ConversionViewModel : BaseViewModel
    {

        private ISymbols Symbols
        {
            get
            {
                return App.Account.Symbols;
            }
        }

        private ICurrencies Currencies
        {
            get
            {
                return App.Account.Currencies;
            }
        }

        private IAtomexApp App { get; }
        private IAtomexClient Terminal { get; set;  }

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
                    .Where(c => Symbols.SymbolByCurrencies(c.Currency, _fromCurrency.Currency) != null)
                    .ToList();

                if (oldToCurrency != null && oldToCurrency != _fromCurrency &&
                    ToCurrencies.FirstOrDefault(c => c.Currency.Name == oldToCurrency.Currency.Name) != null)
                {
                    ToCurrency = oldToCurrency;
                }
                else
                {
                    ToCurrency = ToCurrencies.First();
                }

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

                OnQuotesUpdatedEventHandler(App.Terminal, null);
                OnBaseQuotesUpdatedEventHandler(App.QuotesProvider, EventArgs.Empty);
            }
        }

        private decimal _amount;
        public decimal Amount
        {
            get => _amount;
            set { UpdateAmount(value); }
            //set
            //{
            //    _amount = value;

            //    var (maxAmount, maxFee, _) = _app.Account
            //        .EstimateMaxAmountToSendAsync(FromCurrency.Name, null, BlockchainTransactionType.SwapPayment)
            //        .WaitForResult();

            //    var availableAmount = FromCurrency.Currency is BitcoinBasedCurrency
            //        ? FromCurrency.AvailableAmount
            //        : maxAmount + maxFee;

            //    var estimatedPaymentFee = _amount != 0
            //        ? (_amount < availableAmount
            //            ? _app.Account
            //                .EstimateFeeAsync(FromCurrency.Name, null, _amount, BlockchainTransactionType.SwapPayment)
            //                .WaitForResult()
            //            : null)
            //        : 0;

            //    if (estimatedPaymentFee != null)
            //    {
            //        var redeemAddress = _app.Account
            //            .GetRedeemAddressAsync(ToCurrency.Name)
            //            .WaitForResult();

            //        _estimatedPaymentFee = estimatedPaymentFee.Value;
            //        _estimatedRedeemFee = ToCurrency.Currency.GetDefaultRedeemFee(redeemAddress);

            //        if (ToCurrency.AvailableAmount == 0 && !(ToCurrency.Currency is BitcoinBasedCurrency))
            //        {
            //            _estimatedRedeemFee *= 2;
            //        }

            //        if (_amount + _estimatedPaymentFee > availableAmount)
            //            _amount = Math.Max(availableAmount - _estimatedPaymentFee, 0);

            //        OnQuotesUpdatedEventHandler(_app.Terminal, null);
            //        OnBaseQuotesUpdatedEventHandler(_app.QuotesProvider, EventArgs.Empty);
            //    }
            //    else
            //    {
            //        _targetAmount = _estimatedPaymentFee = _estimatedPrice = 0;
            //        _isNoLiquidity = true;
            //        OnPropertyChanged(nameof(TargetAmount));
            //        OnPropertyChanged(nameof(EstimatedPrice));
            //        OnPropertyChanged(nameof(IsNoLiquidity));
            //    }

            //    OnPropertyChanged(nameof(Amount));
            //    OnPropertyChanged(nameof(EstimatedPaymentFee));
            //    OnPropertyChanged(nameof(EstimatedRedeemFee));
            //}
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

        private bool _useRewardForRedeem;

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

        private decimal _estimatedOrderPrice;
        public decimal EstimatedOrderPrice
        {
            get => _estimatedOrderPrice;
            set { _estimatedOrderPrice = value; OnPropertyChanged(nameof(EstimatedOrderPrice)); }
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

        public class Grouping<K, T> : ObservableCollection<T>
        {
            public K Date { get; private set; }
            public Grouping(K date, IEnumerable<T> items)
            {
                Date = date;
                foreach (T item in items)
                    Items.Add(item);
            }
        }

        public ObservableCollection<Grouping<DateTime, SwapViewModel>> GroupedSwaps { get; set; }


        public ConversionViewModel(IAtomexApp app)
        {
            App = app;
            FromCurrencies = ToCurrencies = _currencyViewModels = new List<CurrencyViewModel>();
            FillCurrenciesAsync().FireAndForget();
            SubscribeToServices();
        }

        private void SubscribeToServices()
        {
            App.TerminalChanged += OnTerminalChangedEventHandler;
            OnTerminalChangedEventHandler(this, new TerminalChangedEventArgs(App.Terminal));

            if (App.HasQuotesProvider)
                App.QuotesProvider.QuotesUpdated += OnBaseQuotesUpdatedEventHandler;
        }

        private void OnTerminalChangedEventHandler(object sender, TerminalChangedEventArgs args)
        {
            var terminal = args.Terminal;

            if (Terminal != terminal && Terminal != null)
            {
                Terminal.QuotesUpdated -= OnQuotesUpdatedEventHandler;
                Terminal.SwapUpdated -= OnSwapEventHandler;
            }

            Terminal = terminal;

            if (Terminal?.Account == null)
                return;

            Terminal.QuotesUpdated += OnQuotesUpdatedEventHandler;
            Terminal.SwapUpdated += OnSwapEventHandler;

            OnSwapEventHandler(this, null);
        }

        private async Task FillCurrenciesAsync()
        {
            await Task.WhenAll(Currencies.Select(async c =>
            {
                var balance = await App.Account.GetBalanceAsync(c.Name);
                var address = await App.Account.GetFreeExternalAddressAsync(c.Name);

                _currencyViewModels.Add(new CurrencyViewModel(App)
                {
                    Currency = c,
                    TotalAmount = balance.Confirmed,
                    AvailableAmount = balance.Available,
                    UnconfirmedAmount = balance.UnconfirmedIncome + balance.UnconfirmedOutcome,
                    FreeExternalAddress = address.Address
                });
            }));
            FromCurrency = _currencyViewModels.FirstOrDefault();
        }

        private async Task UpdateSwaps()
        {
            var swaps = await App.Account.GetSwapsAsync();

            var swapViewModels = swaps
                               .Select(s => SwapViewModelFactory.CreateSwapViewModel(s, Currencies))
                               .ToList()
                               .SortList((s1, s2) => s2.Time.ToUniversalTime()
                                   .CompareTo(s1.Time.ToUniversalTime()));
            Swaps = new ObservableCollection<SwapViewModel>(swapViewModels);

            var groups = Swaps.GroupBy(p => p.Time.Date).Select(g => new Grouping<DateTime, SwapViewModel>(g.Key, g));
            GroupedSwaps = new ObservableCollection<Grouping<DateTime, SwapViewModel>>(groups);

            await Device.InvokeOnMainThreadAsync(() =>
            {
                OnPropertyChanged(nameof(Swaps));
                OnPropertyChanged(nameof(GroupedSwaps));
            });
        }

        public async Task<(decimal, decimal, decimal)> EstimateMaxAmount()
        {
            return await App.Account
                .EstimateMaxAmountToSendAsync(FromCurrency.Currency.Name, null, BlockchainTransactionType.SwapPayment)
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
                if (!(sender is IAtomexClient terminal))
                    return;

                if (ToCurrency == null)
                    return;

                var symbol = Symbols.SymbolByCurrencies(FromCurrency.Currency, ToCurrency.Currency);
                if (symbol == null)
                    return;

                var side = symbol.OrderSideForBuyCurrency(ToCurrency.Currency);
                var orderBook = terminal.GetOrderBook(symbol);

                if (orderBook == null)
                    return;


                var walletAddress = await App.Account
                    .GetRedeemAddressAsync(ToCurrency.Currency.FeeCurrencyName);

                var baseCurrency = Currencies.GetByName(symbol.Base);

                (_estimatedOrderPrice, _estimatedPrice) = orderBook.EstimateOrderPrices(
                    side,
                    Amount,
                    FromCurrency.Currency.DigitsMultiplier,
                    baseCurrency.DigitsMultiplier);

                _estimatedMaxAmount = orderBook.EstimateMaxAmount(side, FromCurrency.Currency.DigitsMultiplier);
                EstimatedRedeemFee = ToCurrency.Currency.GetDefaultRedeemFee(walletAddress);

                _isNoLiquidity = Amount != 0 && _estimatedOrderPrice == 0;

                if (symbol.IsBaseCurrency(ToCurrency.Currency.Name))
                {
                    _targetAmount = _estimatedPrice != 0
                        ? AmountHelper.RoundDown(Amount / _estimatedPrice, ToCurrency.Currency.DigitsMultiplier)
                        : 0m;
                }
                else if (symbol.IsQuoteCurrency(ToCurrency.Currency.Name))
                {
                    _targetAmount = AmountHelper.RoundDown(Amount * _estimatedPrice, ToCurrency.Currency.DigitsMultiplier);
                }


                await Device.InvokeOnMainThreadAsync(() =>
                {
                    OnPropertyChanged(nameof(EstimatedPrice));
                    OnPropertyChanged(nameof(EstimatedMaxAmount));
                    OnPropertyChanged(nameof(TargetAmount));
                    OnPropertyChanged(nameof(IsNoLiquidity));
                    UpdateTargetAmountInBase(App.QuotesProvider);
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
                var account = App.Account;

                var fromWallets = (await account
                    .GetUnspentAddressesAsync(
                        toAddress: null,
                        currency: FromCurrency.Currency.Name,
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

                var symbol = App.Account.Symbols.SymbolByCurrencies(FromCurrency.Currency, ToCurrency.Currency);
                var baseCurrency = App.Account.Currencies.GetByName(symbol.Base);
                var side = symbol.OrderSideForBuyCurrency(ToCurrency.Currency);
                var terminal = App.Terminal;
                var price = EstimatedPrice;
                var orderPrice = EstimatedOrderPrice;

                if (price == 0)
                    return new Error(Errors.NoLiquidity, "Not enough liquidity to convert a specified amount.");

                var qty = AmountHelper.AmountToQty(side, Amount, price, baseCurrency.DigitsMultiplier);

                if (qty < symbol.MinimumQty)
                {
                    var minimumAmount = AmountHelper.QtyToAmount(side, symbol.MinimumQty, price, FromCurrency.Currency.DigitsMultiplier);
                    var message = string.Format(CultureInfo.InvariantCulture, "The amount must be greater than or equal to the minimum allowed amount {0} {1}", minimumAmount, FromCurrency.CurrencyCode);

                    return new Error(Errors.SwapError, message);
                }

                var order = new Order
                {
                    Symbol = symbol.Name,
                    TimeStamp = DateTime.UtcNow,
                    Price = orderPrice,
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

                    if (currentOrder.Status == OrderStatus.Pending)
                        continue;

                    if (currentOrder.Status == OrderStatus.PartiallyFilled || currentOrder.Status == OrderStatus.Filled)
                    {
                        var swap = (await terminal.Account
                            .GetSwapsAsync())
                            .FirstOrDefault(s => s.OrderId == currentOrder.Id);

                        if (swap == null)
                            continue;

                        return null;
                    }

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

        private async void UpdateAmount(decimal value)
        {
            var previousAmount = _amount;
            _amount = value;

            var (maxAmount, maxFee, reserve) = await App.Account
                .EstimateMaxAmountToSendAsync(FromCurrency.Currency.Name, null, BlockchainTransactionType.SwapPayment);

            var swaps = await App.Account
                .GetSwapsAsync();

            var usedAmount = swaps.Sum(s => (s.IsActive && s.SoldCurrency == FromCurrency.Currency.Name && !s.StateFlags.HasFlag(SwapStateFlags.IsPaymentConfirmed))
                ? s.Symbol.IsBaseCurrency(FromCurrency.Currency.Name)
                    ? s.Qty
                    : s.Qty * s.Price
                : 0);

            usedAmount = AmountHelper.RoundDown(usedAmount, FromCurrency.Currency.DigitsMultiplier);

            maxAmount = Math.Max(maxAmount - usedAmount, 0);

            var includeFeeToAmount = FromCurrency.Currency.FeeCurrencyName == FromCurrency.Currency.Name;

            var availableAmount = FromCurrency.Currency is BitcoinBasedCurrency
                ? FromCurrency.AvailableAmount
                : maxAmount + (includeFeeToAmount ? maxFee : 0);

            var estimatedPaymentFee = _amount != 0
                ? (_amount < availableAmount
                    ? await App.Account
                        .EstimateFeeAsync(FromCurrency.Currency.Name, null, _amount, BlockchainTransactionType.SwapPayment)
                    : null)
                : 0;

            if (estimatedPaymentFee == null)
            {
                if (maxAmount > 0)
                {
                    _amount = maxAmount;
                    estimatedPaymentFee = maxFee;
                }
                else
                {
                    _amount = 0; // previousAmount;
                    OnPropertyChanged(nameof(Amount));

                    return;
                    // todo: insufficient funds warning
                    // 
                }
            }

            var walletAddress = await App.Account
                .GetRedeemAddressAsync(ToCurrency.Currency.FeeCurrencyName);

            _estimatedPaymentFee = estimatedPaymentFee.Value;
            _estimatedRedeemFee = ToCurrency.Currency.GetDefaultRedeemFee(walletAddress);
            _useRewardForRedeem = false;

            if (walletAddress.AvailableBalance() < _estimatedRedeemFee && !(ToCurrency.Currency is BitcoinBasedCurrency))
            {
                _estimatedRedeemFee *= 2; // todo: show hint or tool tip
                _useRewardForRedeem = true;
            }

            if (_amount + (includeFeeToAmount ? _estimatedPaymentFee : 0) > availableAmount)
                _amount = Math.Max(availableAmount - (includeFeeToAmount ? _estimatedPaymentFee : 0), 0);


            OnPropertyChanged(nameof(Amount));
            OnPropertyChanged(nameof(EstimatedPaymentFee));
            OnPropertyChanged(nameof(EstimatedRedeemFee));

            OnQuotesUpdatedEventHandler(App.Terminal, null);
            OnBaseQuotesUpdatedEventHandler(App.QuotesProvider, EventArgs.Empty);
        }
    }
}
