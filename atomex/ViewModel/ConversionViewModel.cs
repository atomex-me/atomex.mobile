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
using atomex.Resources;

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

        protected IAtomexApp App { get; }
        private IAtomexClient Terminal { get; set;  }

        private static TimeSpan SWAP_TIMEOUT = TimeSpan.FromSeconds(60);

        private static TimeSpan SWAP_CHECK_INTERVAL = TimeSpan.FromSeconds(3);

        private decimal _estimatedOrderPrice;

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
            private set { _toCurrencies = value; OnPropertyChanged(nameof(ToCurrencies)); }
        }

        protected CurrencyViewModel _fromCurrencyViewModel;
        public virtual CurrencyViewModel FromCurrencyViewModel
        {
            get => _fromCurrencyViewModel;
            set
            {
                if (_fromCurrencyViewModel == value)
                    return;

                _fromCurrencyViewModel = value;

                if (_fromCurrencyViewModel == null)
                    return;

                OnPropertyChanged(nameof(FromCurrencyViewModel));

                var oldToCurrencyViewModel = ToCurrencyViewModel;

                ToCurrencies = _currencyViewModels
                    .Where(c => Symbols.SymbolByCurrencies(c.Currency, _fromCurrencyViewModel.Currency) != null)
                    .ToList();

                if (oldToCurrencyViewModel != null &&
                    oldToCurrencyViewModel.Currency.Name != _fromCurrencyViewModel.Currency.Name &&
                    ToCurrencies.FirstOrDefault(c => c.Currency.Name == oldToCurrencyViewModel.Currency.Name) != null)
                {
                    ToCurrencyViewModel = ToCurrencies.FirstOrDefault(c => c.Currency.Name == oldToCurrencyViewModel.Currency.Name);
                }
                else
                {
                    ToCurrencyViewModel = ToCurrencies.First();
                }

                CurrencyCode = _fromCurrencyViewModel?.CurrencyCode;

                FromFeeCurrencyCode = _fromCurrencyViewModel?.FeeCurrencyCode;

                BaseCurrencyCode = _fromCurrencyViewModel?.BaseCurrencyCode;

                Amount = 0;

                AmountInBase = 0;

                TargetAmount = 0;

                TargetAmountInBase = 0;
            }
        }

        private CurrencyViewModel _toCurrencyViewModel;
        public CurrencyViewModel ToCurrencyViewModel
        {
            get => _toCurrencyViewModel;
            set
            {
                if (_toCurrencyViewModel == value)
                    return;

                _toCurrencyViewModel = value;

                if (_toCurrencyViewModel == null)
                    return;

                OnPropertyChanged(nameof(ToCurrencyViewModel));

                TargetCurrencyCode = _toCurrencyViewModel?.CurrencyCode;

                TargetFeeCurrencyCode = _toCurrencyViewModel?.FeeCurrencyCode;

                OnQuotesUpdatedEventHandler(App.Terminal, null);
                OnBaseQuotesUpdatedEventHandler(App.QuotesProvider, EventArgs.Empty);

                //UpdateRedeemAndRewardFeesAsync();
            }
        }

        protected decimal _amount;
        public decimal Amount
        {
            get => _amount;
            set { UpdateAmount(value); }
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
            get => _estimatedPrice;
            set { _estimatedPrice = value; OnPropertyChanged(nameof(EstimatedPrice)); }
        }

        protected decimal _estimatedPaymentFee;
        public decimal EstimatedPaymentFee
        {
            get => _estimatedPaymentFee;
            set { _estimatedPaymentFee = value; OnPropertyChanged(nameof(EstimatedPaymentFee)); }
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

        private string _currencyCode;
        public string CurrencyCode
        {
            get => _currencyCode;
            set { _currencyCode = value; OnPropertyChanged(nameof(CurrencyCode)); }
        }

        private string _fromFeeCurrencyCode;
        public string FromFeeCurrencyCode
        {
            get => _fromFeeCurrencyCode;
            set { _fromFeeCurrencyCode = value; OnPropertyChanged(nameof(FromFeeCurrencyCode)); }
        }

        private string _targetCurrencyCode;
        public string TargetCurrencyCode
        {
            get => _targetCurrencyCode;
            set { _targetCurrencyCode = value; OnPropertyChanged(nameof(TargetCurrencyCode)); }
        }

        private string _targetFeeCurrencyCode;
        public string TargetFeeCurrencyCode
        {
            get => _targetFeeCurrencyCode;
            set { _targetFeeCurrencyCode = value; OnPropertyChanged(nameof(TargetFeeCurrencyCode)); }
        }

        private string _baseCurrencyCode;
        public string BaseCurrencyCode
        {
            get => _baseCurrencyCode;
            set { _baseCurrencyCode = value; OnPropertyChanged(nameof(BaseCurrencyCode)); }
        }

        //private decimal _rewardForRedeem;
        //public decimal RewardForRedeem
        //{
        //    get => _rewardForRedeem;
        //    set
        //    {
        //        _rewardForRedeem = value;
        //        OnPropertyChanged(nameof(RewardForRedeem));

        //        HasRewardForRedeem = _rewardForRedeem != 0;
        //    }
        //}

        //private decimal _rewardForRedeemInBase;
        //public decimal RewardForRedeemInBase
        //{
        //    get => _rewardForRedeemInBase;
        //    set { _rewardForRedeemInBase = value; OnPropertyChanged(nameof(RewardForRedeemInBase)); }
        //}

        //private bool _hasRewardForRedeem;
        //public bool HasRewardForRedeem
        //{
        //    get => _hasRewardForRedeem;
        //    set { _hasRewardForRedeem = value; OnPropertyChanged(nameof(HasRewardForRedeem)); }
        //}
        protected string _warning;
        public string Warning
        {
            get => _warning;
            set { _warning = value; OnPropertyChanged(nameof(Warning)); }
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

        public void SetFromCurrency(string currencyCode)
        {
            FromCurrencyViewModel = _currencyViewModels.
                Where(c => c.CurrencyCode == currencyCode).First();
        }

        public ObservableCollection<Grouping<DateTime, SwapViewModel>> GroupedSwaps { get; set; }

        public ConversionViewModel(IAtomexApp app)
        {
            App = app;
            _fromCurrencies = new List<CurrencyViewModel>();
            _toCurrencies = new List<CurrencyViewModel>();
            _currencyViewModels = new List<CurrencyViewModel>();

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

                _currencyViewModels.Add(new CurrencyViewModel(App)
                {
                    Currency = c,
                    TotalAmount = balance.Confirmed,
                    AvailableAmount = balance.Available,
                    UnconfirmedAmount = balance.UnconfirmedIncome + balance.UnconfirmedOutcome,
                });
            }));

            FromCurrencies = _currencyViewModels.ToList();
            FromCurrencyViewModel = _currencyViewModels.FirstOrDefault();
        }

        private async Task UpdateSwaps()
        {
            var swaps = await App.Account.GetSwapsAsync();

            var swapViewModels = swaps
                               .Select(s => SwapViewModelFactory.CreateSwapViewModel(s, Currencies))
                               .ToList()
                               .SortList((s1, s2) => s2.LocalTime.CompareTo(s1.LocalTime));
            Swaps = new ObservableCollection<SwapViewModel>(swapViewModels);

            var groups = Swaps.GroupBy(p => p.Time.Date).Select(g => new Grouping<DateTime, SwapViewModel>(g.Key, g));
            GroupedSwaps = new ObservableCollection<Grouping<DateTime, SwapViewModel>>(groups);

            await Device.InvokeOnMainThreadAsync(() =>
            {
                OnPropertyChanged(nameof(Swaps));
                OnPropertyChanged(nameof(GroupedSwaps));
            });
        }

        protected void OnBaseQuotesUpdatedEventHandler(object sender, EventArgs args)
        {
            if (!(sender is ICurrencyQuotesProvider provider))
                return;

            if (CurrencyCode == null || TargetCurrencyCode == null || BaseCurrencyCode == null)
                return;

            var fromCurrencyPrice = provider.GetQuote(CurrencyCode, BaseCurrencyCode)?.Bid ?? 0m;
            AmountInBase = _amount * fromCurrencyPrice;

            var fromCurrencyFeePrice = provider.GetQuote(FromCurrencyViewModel.Currency.FeeCurrencyName, BaseCurrencyCode)?.Bid ?? 0m;
            EstimatedPaymentFeeInBase = _estimatedPaymentFee * fromCurrencyFeePrice;

            var toCurrencyFeePrice = provider.GetQuote(ToCurrencyViewModel.Currency.FeeCurrencyName, BaseCurrencyCode)?.Bid ?? 0m;
            EstimatedRedeemFeeInBase = _estimatedRedeemFee * toCurrencyFeePrice;

            //var toCurrencyPrice = provider.GetQuote(TargetCurrencyCode, BaseCurrencyCode)?.Bid ?? 0m;
            //RewardForRedeemInBase = _rewardForRedeem * toCurrencyPrice;

            UpdateTargetAmountInBase(provider);
        }

        protected async void OnQuotesUpdatedEventHandler(object sender, MarketDataEventArgs args)
        {
            try
            {
                if (!(sender is IAtomexClient terminal))
                    return;

                if (ToCurrencyViewModel == null)
                    return;

                var symbol = Symbols.SymbolByCurrencies(FromCurrencyViewModel.Currency, ToCurrencyViewModel.Currency);
                if (symbol == null)
                    return;

                var side = symbol.OrderSideForBuyCurrency(ToCurrencyViewModel.Currency);
                var orderBook = terminal.GetOrderBook(symbol);

                if (orderBook == null)
                    return;

                var walletAddress = await App.Account
                    .GetRedeemAddressAsync(ToCurrencyViewModel.Currency.FeeCurrencyName);

                var baseCurrency = Currencies.GetByName(symbol.Base);

                (_estimatedOrderPrice, _estimatedPrice) = orderBook.EstimateOrderPrices(
                    side,
                    Amount,
                    FromCurrencyViewModel.Currency.DigitsMultiplier,
                    baseCurrency.DigitsMultiplier);

                _estimatedMaxAmount = orderBook.EstimateMaxAmount(side, FromCurrencyViewModel.Currency.DigitsMultiplier);
                EstimatedRedeemFee = ToCurrencyViewModel.Currency.GetRedeemFee(walletAddress);

                _isNoLiquidity = Amount != 0 && _estimatedOrderPrice == 0;

                if (symbol.IsBaseCurrency(ToCurrencyViewModel.Currency.Name))
                {
                    _targetAmount = _estimatedPrice != 0
                        ? AmountHelper.RoundDown(Amount / _estimatedPrice, ToCurrencyViewModel.Currency.DigitsMultiplier)
                        : 0m;
                }
                else if (symbol.IsQuoteCurrency(ToCurrencyViewModel.Currency.Name))
                {
                    _targetAmount = AmountHelper.RoundDown(Amount * _estimatedPrice, ToCurrencyViewModel.Currency.DigitsMultiplier);
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

            if (TargetCurrencyCode == null)
                return;

            if (BaseCurrencyCode == null)
                return;

            var quote = provider.GetQuote(TargetCurrencyCode, BaseCurrencyCode);

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
                        currency: FromCurrencyViewModel.Currency.Name,
                        amount: Amount,
                        fee: 0,
                        feePrice: 0,
                        feeUsagePolicy: FeeUsagePolicy.EstimatedFee,
                        addressUsagePolicy: AddressUsagePolicy.UseMinimalBalanceFirst,
                        transactionType: BlockchainTransactionType.SwapPayment))
                    .ToList();

                if (Amount == 0)
                    return new Error(Errors.SwapError, AppResources.AmountLessThanZeroError);

                if (Amount > 0 && !fromWallets.Any())
                    return new Error(Errors.SwapError, AppResources.InsufficientFunds);

                var symbol = App.Account.Symbols.SymbolByCurrencies(FromCurrencyViewModel.Currency, ToCurrencyViewModel.Currency);
                var baseCurrency = App.Account.Currencies.GetByName(symbol.Base);
                var side = symbol.OrderSideForBuyCurrency(ToCurrencyViewModel.Currency);
                var terminal = App.Terminal;
                var price = EstimatedPrice;
                var orderPrice = _estimatedOrderPrice;

                if (price == 0)
                    return new Error(Errors.NoLiquidity, AppResources.NoLiquidityError);

                var qty = AmountHelper.AmountToQty(side, Amount, price, baseCurrency.DigitsMultiplier);

                if (qty < symbol.MinimumQty)
                {
                    var minimumAmount = AmountHelper.QtyToAmount(side, symbol.MinimumQty, price, FromCurrencyViewModel.Currency.DigitsMultiplier);
                    var message = string.Format(CultureInfo.InvariantCulture, "The amount must be greater than or equal to the minimum allowed amount {0} {1}", minimumAmount, FromCurrencyViewModel.Currency.Name);

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
                        return new Error(Errors.PriceHasChanged, AppResources.PriceChangedError);

                    if (currentOrder.Status == OrderStatus.Rejected)
                        return new Error(Errors.OrderRejected, AppResources.OrderRejectedError);
                }

                return new Error(Errors.TimeoutReached, AppResources.TimeoutReachedError);
            }
            catch (Exception e)
            {
                Log.Error(e, "Conversion error");

                return new Error(Errors.SwapError, AppResources.ConversionError);
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

        public virtual async Task OnMaxClick()
        {
            var (maxAmount, maxFee, _) = await App.Account
                .EstimateMaxAmountToSendAsync(FromCurrencyViewModel.Currency.Name, null, BlockchainTransactionType.SwapPayment, 0, 0, true);

            _amount = maxAmount;
            OnPropertyChanged(nameof(Amount));

            if (maxAmount <= 0)
            {

                Warning = string.Format(CultureInfo.InvariantCulture, AppResources.InsufficientFunds);

                if (FromCurrencyViewModel.Currency.Name != FromCurrencyViewModel.Currency.FeeCurrencyName && FromCurrencyViewModel.AvailableAmount > 0)
                    Warning = string.Format(CultureInfo.InvariantCulture, AppResources.InsufficientChainFunds, FromCurrencyViewModel.Currency.FeeCurrencyName);
            }
            else
            {
                EstimatedPaymentFee = maxFee;

                UpdateRedeemAndRewardFeesAsync();

                OnQuotesUpdatedEventHandler(App.Terminal, null);
                OnBaseQuotesUpdatedEventHandler(App.QuotesProvider, EventArgs.Empty);
            }
        }

        protected virtual async void UpdateAmount(decimal value)
        {
            Warning = string.Empty;

            var previousAmount = _amount;
            _amount = value;

            var (maxAmount, maxFee, reserve) = await App.Account
                .EstimateMaxAmountToSendAsync(FromCurrencyViewModel.Currency.Name, null, BlockchainTransactionType.SwapPayment, 0, 0, true);

            var swaps = await App.Account
                .GetSwapsAsync();

            var usedAmount = swaps.Sum(s => (s.IsActive && s.SoldCurrency == FromCurrencyViewModel.Currency.Name && !s.StateFlags.HasFlag(SwapStateFlags.IsPaymentConfirmed))
                ? s.Symbol.IsBaseCurrency(FromCurrencyViewModel.Currency.Name)
                   ? s.Qty
                   : s.Qty * s.Price
                : 0);

            usedAmount = AmountHelper.RoundDown(usedAmount, FromCurrencyViewModel.Currency.DigitsMultiplier);

            maxAmount = Math.Max(maxAmount - usedAmount, 0);


            var includeFeeToAmount = FromCurrencyViewModel.Currency.FeeCurrencyName == FromCurrencyViewModel.Currency.Name;

            var availableAmount = FromCurrencyViewModel.Currency is BitcoinBasedCurrency
                ? FromCurrencyViewModel.AvailableAmount
                : maxAmount + (includeFeeToAmount ? maxFee : 0);

            var estimatedPaymentFee = _amount != 0
                ? (_amount < availableAmount
                    ? await App.Account
                        .EstimateFeeAsync(FromCurrencyViewModel.Currency.Name, null, _amount, BlockchainTransactionType.SwapPayment)
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
                    Warning = string.Format(CultureInfo.InvariantCulture, AppResources.InsufficientFunds);

                    if (FromCurrencyViewModel.Currency.Name != FromCurrencyViewModel.Currency.FeeCurrencyName && FromCurrencyViewModel.AvailableAmount > 0)
                        Warning = string.Format(CultureInfo.InvariantCulture, AppResources.InsufficientChainFunds, FromCurrencyViewModel.Currency.FeeCurrencyName);

                    return;
                }
            }

            EstimatedPaymentFee = estimatedPaymentFee.Value;

            if (_amount + (includeFeeToAmount ? _estimatedPaymentFee : 0) > availableAmount)
                _amount = Math.Max(availableAmount - (includeFeeToAmount ? _estimatedPaymentFee : 0), 0);

            //if (_amount <= 0)
            //{
            //    if (FromCurrencyViewModel.Currency.Name != FromCurrencyViewModel.Currency.FeeCurrencyName && FromCurrencyViewModel.AvailableAmount > 0)
            //    {
            //        Warning = string.Format(CultureInfo.InvariantCulture, AppResources.InsufficientChainFunds, FromCurrencyViewModel.Currency.FeeCurrencyName);
            //        return;
            //    }

            //    Warning = string.Format(CultureInfo.InvariantCulture, AppResources.AmountLessThanZeroError);
            //    return;
            //}

            if (_amount > maxAmount)
            {
                Warning = string.Format(CultureInfo.InvariantCulture, AppResources.InsufficientFunds);
                return;
            }

            OnPropertyChanged(nameof(Amount));

            UpdateRedeemAndRewardFeesAsync();

            OnQuotesUpdatedEventHandler(App.Terminal, null);
            OnBaseQuotesUpdatedEventHandler(App.QuotesProvider, EventArgs.Empty);
        }

        private async void UpdateRedeemAndRewardFeesAsync()
        {
            var walletAddress = await App.Account
                     .GetRedeemAddressAsync(ToCurrencyViewModel.Currency.FeeCurrencyName);

            EstimatedRedeemFee = ToCurrencyViewModel.Currency.GetRedeemFee(walletAddress);

            //RewardForRedeem = walletAddress.AvailableBalance() < EstimatedRedeemFee && !(ToCurrency is BitcoinBasedCurrency)
            //       ? ToCurrency.GetRewardForRedeem()
            //       : 0;
        }
    }
}
