using System.Collections.Generic;
using System.Linq;
using Atomex;
using Atomex.Core;
using System.Threading.Tasks;
using Atomex.Common;
using System;
using Atomex.MarketData;
using Serilog;
using Xamarin.Forms;
using Atomex.MarketData.Abstract;
using System.Globalization;
using System.Collections.ObjectModel;
using Atomex.Swaps;
using Atomex.Abstract;
using atomex.Resources;
using Atomex.Swaps.Helpers;
using System.Windows.Input;
using atomex.Views.CreateSwap;
using Atomex.Services;
using Atomex.Services.Abstract;
using Atomex.Wallet.Abstract;
using atomex.ViewModel.CurrencyViewModels;

namespace atomex.ViewModel
{
    public class ConversionViewModel : BaseViewModel
    {

        private ISymbols Symbols
        {
            get
            {
                return AtomexApp.SymbolsProvider
                     .GetSymbols(AtomexApp.Account.Network); ;
            }
        }

        private ICurrencies Currencies
        {
            get
            {
                return AtomexApp.Account.Currencies;
            }
        }

        protected IAtomexApp AtomexApp { get; }
        private IAtomexClient Terminal { get; set;  }

        public INavigation Navigation { get; set; }

        private IFromSource FromSource { get; set; }
        private string To { get; set; }
        private string RedeemAddress { get; set; }

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

                CurrencyCode = _fromCurrencyViewModel?.CurrencyCode;
                FromFeeCurrencyCode = _fromCurrencyViewModel?.FeeCurrencyCode;
                BaseCurrencyCode = _fromCurrencyViewModel?.BaseCurrencyCode;

                var oldToCurrencyViewModel = ToCurrencyViewModel;

                ToCurrencies = _currencyViewModels
                    .Where(c => Symbols.SymbolByCurrencies(c.Currency, _fromCurrencyViewModel?.Currency) != null)
                    .ToList();

                if (oldToCurrencyViewModel != null &&
                    oldToCurrencyViewModel.Currency.Name != _fromCurrencyViewModel.Currency.Name &&
                    ToCurrencies.FirstOrDefault(c => c.Currency.Name == oldToCurrencyViewModel.Currency.Name) != null)
                {
                    ToCurrencyViewModel = ToCurrencies
                        .FirstOrDefault(c => c.Currency.Name == oldToCurrencyViewModel.Currency.Name);
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

                _ = Task.Run(async () =>
                {
                    await UpdateRedeemAndRewardFeesAsync();
                    OnQuotesUpdatedEventHandler(AtomexApp.Terminal, null);
                    OnBaseQuotesUpdatedEventHandler(AtomexApp.QuotesProvider, EventArgs.Empty);
                });
            }
        }

        protected decimal _amount;
        public decimal Amount
        {
            get => _amount;
            set { _ = UpdateAmountAsync(value); }
        }

        public string AmountString
        {
            get => Amount.ToString(CultureInfo.InvariantCulture);
            set
            {
                string temp = value.Replace(",", ".");
                if (!decimal.TryParse(temp, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var amount))
                {
                    ResetSwapValues(updateUi: false);
                    return;
                }

                _amount = amount;

                if (amount > long.MaxValue)
                    amount = long.MaxValue;

                _ = UpdateAmountAsync(amount, updateUi: false);
            }
        }

        private decimal _amountInBase;
        public decimal AmountInBase
        {
            get => _amountInBase;
            set { _amountInBase = value; OnPropertyChanged(nameof(AmountInBase)); }
        }

        private bool _isAmountUpdating;
        public bool IsAmountUpdating
        {
            get => _isAmountUpdating;
            set { _isAmountUpdating = value; OnPropertyChanged(nameof(IsAmountUpdating)); }
        }

        private bool _isAmountValid = true;
        public bool IsAmountValid
        {
            get => _isAmountValid;
            set { _isAmountValid = value; OnPropertyChanged(nameof(IsAmountValid)); }
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

        private decimal _estimatedOrderPrice;
        private decimal _estimatedPrice;
        public decimal EstimatedPrice
        {
            get => _estimatedPrice;
            set { _estimatedPrice = value; OnPropertyChanged(nameof(EstimatedPrice)); }
        }

        private decimal _estimatedMaxAmount;
        public decimal EstimatedMaxAmount
        {
            get => _estimatedMaxAmount;
            set { _estimatedMaxAmount = value; OnPropertyChanged(nameof(EstimatedMaxAmount)); }
        }

        private decimal _estimatedMakerNetworkFee;
        public decimal EstimatedMakerNetworkFee
        {
            get => _estimatedMakerNetworkFee;
            set { _estimatedMakerNetworkFee = value; OnPropertyChanged(nameof(EstimatedMakerNetworkFee)); }
        }

        private decimal _estimatedMakerNetworkFeeInBase;
        public decimal EstimatedMakerNetworkFeeInBase
        {
            get => _estimatedMakerNetworkFeeInBase;
            set { _estimatedMakerNetworkFeeInBase = value; OnPropertyChanged(nameof(EstimatedMakerNetworkFeeInBase)); }
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

        private decimal _estimatedTotalNetworkFeeInBase;
        public decimal EstimatedTotalNetworkFeeInBase
        {
            get => _estimatedTotalNetworkFeeInBase;
            set { _estimatedTotalNetworkFeeInBase = value; OnPropertyChanged(nameof(EstimatedTotalNetworkFeeInBase)); }
        }

        private decimal _rewardForRedeem;
        public decimal RewardForRedeem
        {
            get => _rewardForRedeem;
            set { _rewardForRedeem = value; OnPropertyChanged(nameof(RewardForRedeem)); }
        }

        private decimal _rewardForRedeemInBase;
        public decimal RewardForRedeemInBase
        {
            get => _rewardForRedeemInBase;
            set { _rewardForRedeemInBase = value; OnPropertyChanged(nameof(RewardForRedeemInBase)); }
        }

        private bool _hasRewardForRedeem;
        public bool HasRewardForRedeem
        {
            get => _hasRewardForRedeem;
            set { _hasRewardForRedeem = value; OnPropertyChanged(nameof(HasRewardForRedeem)); }
        }

        protected string _warning;
        public string Warning
        {
            get => _warning;
            set { _warning = value; OnPropertyChanged(nameof(Warning)); }
        }

        protected bool _isCriticalWarning;
        public bool IsCriticalWarning
        {
            get => _isCriticalWarning;
            set { _isCriticalWarning = value; OnPropertyChanged(nameof(IsCriticalWarning)); }
        }

        private bool _canConvert = true;
        public bool CanConvert
        {
            get => _canConvert;
            set { _canConvert = value; OnPropertyChanged(nameof(CanConvert)); }
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

        private Dictionary<long, SwapViewModel> _cachedSwaps;

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

        private float _opacity = 1f;
        public float Opacity
        {
            get => _opacity;
            set { _opacity = value; OnPropertyChanged(nameof(Opacity)); }
        }

        private bool _isLoading = false;
        public bool IsLoading
        {
            get => _isLoading;
            set
            {
                if (_isLoading == value)
                    return;

                _isLoading = value;

                if (_isLoading)
                    Opacity = 0.3f;
                else
                    Opacity = 1f;

                OnPropertyChanged(nameof(IsLoading));
            }
        }

        public void SetFromCurrency(string currencyCode)
        {
            try
            {
                //FromCurrencyViewModel = _currencyViewModels.
                //    Where(c => c.CurrencyCode == currencyCode).Single() ?? _currencyViewModels.First();

                var fromCurrencyVm = FromCurrencies
                    .FirstOrDefault(c => c.Currency?.Name == currencyCode);

                //FromCurrencyIndex = FromCurrencies.IndexOf(fromCurrencyVm);
            }
            catch(Exception e)
            {
                Log.Error(e, "ConversionViewModel SetFromCurrency error.");
            }
        }

        public ObservableCollection<Grouping<DateTime, SwapViewModel>> GroupedSwaps { get; set; }

        public string AmountEntryPlaceholderString => $"{AppResources.AmountEntryPlaceholder}, {CurrencyCode}";

        public ConversionViewModel(IAtomexApp app)
        {
            AtomexApp = app ?? throw new ArgumentNullException(nameof(AtomexApp));
            _fromCurrencies = new List<CurrencyViewModel>();
            _toCurrencies = new List<CurrencyViewModel>();
            _currencyViewModels = new List<CurrencyViewModel>();
            _cachedSwaps = new Dictionary<long, SwapViewModel>();

            SubscribeToServices();
            GetSwaps();
        }

        private void SubscribeToServices()
        {
            AtomexApp.AtomexClientChanged += OnTerminalChangedEventHandler;
            OnTerminalChangedEventHandler(this, new AtomexClientChangedEventArgs(AtomexApp.Terminal));

            if (AtomexApp.HasQuotesProvider)
                AtomexApp.QuotesProvider.QuotesUpdated += OnBaseQuotesUpdatedEventHandler;
        }

        private ICommand _nextCommand;
        public ICommand NextCommand => _nextCommand ??= new Command(OnNextButtonClick);

        private ICommand _maxAmountCommand;
        public ICommand MaxAmountCommand => _maxAmountCommand ??= new Command(async () =>
        {
            try
            {
                var swapParams = await Atomex.ViewModels.Helpers
                    .EstimateSwapPaymentParamsAsync(
                        from: FromSource,
                        to: To,
                        amount: EstimatedMaxAmount,
                        redeemFromAddress: RedeemAddress,
                        fromCurrency: FromCurrencyViewModel?.Currency,
                        toCurrency: ToCurrencyViewModel?.Currency,
                        account: AtomexApp.Account,
                        atomexClient: AtomexApp.Terminal,
                        symbolsProvider: AtomexApp.SymbolsProvider,
                        quotesProvider: AtomexApp.QuotesProvider);

                _amount = Math.Min(swapParams.Amount, EstimatedMaxAmount);
                _ = UpdateAmountAsync(_amount, updateUi: true);
            }
            catch (Exception e)
            {
                Log.Error(e, "Max amount command error.");
            }
        });

        private ICommand _swapCurrenciesCommand;
        public ICommand SwapCurrenciesCommand => _swapCurrenciesCommand ??= new Command(() =>
        {
            CurrencyViewModel temp = FromCurrencyViewModel;
            FromCurrencyViewModel = ToCurrencyViewModel;
            ToCurrencyViewModel = temp;
        });

        private ICommand _selectSwapCommand;
        public ICommand SelectSwapCommand => _selectSwapCommand ??= new Command<SwapViewModel>(async (value) => await OnSwapTapped(value));

        private ICommand _createNewSwapCommand;
        public ICommand CreateNewSwapCommand => _createNewSwapCommand ??= new Command(async () => await OnCreateSwapButtonClicked());

        private ICommand _amoutPageCommand;
        public ICommand AmoutPageCommand => _amoutPageCommand ??= new Command(async () => await ShowAmountPage());

        private ICommand _totalFeeCommand;
        public ICommand TotalFeeCommand => _totalFeeCommand ??= new Command(async () => await OnTotalFeeTapped());
        
        private async Task OnTotalFeeTapped()
        {
            string message = string.Format(
                   CultureInfo.InvariantCulture,
                   AppResources.TotalNetworkFeeDetail,
                   AppResources.PaymentFeeLabel,
                   FormattableString.Invariant($"{EstimatedPaymentFee} {FromCurrencyViewModel.FeeCurrencyCode}"),
                   FormattableString.Invariant($"{EstimatedPaymentFeeInBase:(0.00$)}"),
                   HasRewardForRedeem ?
                       AppResources.RewardForRedeemLabel :
                       AppResources.RedeemFeeLabel,
                   HasRewardForRedeem ?
                       FormattableString.Invariant($"{RewardForRedeem} {ToCurrencyViewModel.FeeCurrencyCode}") :
                       FormattableString.Invariant($"{EstimatedRedeemFee} {ToCurrencyViewModel.FeeCurrencyCode}"),
                   HasRewardForRedeem ?
                       FormattableString.Invariant($"{RewardForRedeemInBase:(0.00$)}") :
                       FormattableString.Invariant($"{EstimatedRedeemFeeInBase:(0.00$)}"),
                   AppResources.MakerFeeLabel,
                   FormattableString.Invariant($"{EstimatedMakerNetworkFee} {FromCurrencyViewModel.CurrencyCode}"),
                   FormattableString.Invariant($"{EstimatedMakerNetworkFeeInBase:(0.00$)}"),
                   AppResources.TotalNetworkFeeLabel,
                   FormattableString.Invariant($"{EstimatedTotalNetworkFeeInBase:0.00$}"));

            await Application.Current.MainPage.DisplayAlert(AppResources.NetworkFee, message, AppResources.AcceptButton);
        }

        private async Task ShowAmountPage()
        {
            Amount = 0;
            Warning = string.Empty;
            await Navigation.PushAsync(new AmountPage(this));
        }

        private async Task OnCreateSwapButtonClicked()
        {
            await Navigation.PushAsync(new CurrenciesPage(this));
        }

        private async Task OnSwapTapped(SwapViewModel swap)
        {
            if (swap != null)
                await Navigation.PushAsync(new SwapInfoPage(swap));
        }

        private void ResetSwapValues(bool updateUi = true)
        {
            _amount = 0;
            OnPropertyChanged(nameof(Amount));

            if (updateUi)
                OnPropertyChanged(nameof(AmountString));

            AmountInBase = 0;

            TargetAmount = 0;

            TargetAmountInBase = 0;

            EstimatedTotalNetworkFeeInBase = 0;

            EstimatedPaymentFee = 0;

            EstimatedPaymentFeeInBase = 0;

            EstimatedRedeemFee = 0;

            EstimatedRedeemFeeInBase = 0;

            EstimatedMakerNetworkFee = 0;

            EstimatedMakerNetworkFeeInBase = 0;
        }

        public virtual async Task UpdateAmountAsync(decimal value, bool updateUi = false)
        {
            Warning = string.Empty;

            try
            {
                IsAmountUpdating = true;

                if (value == 0)
                {
                    ResetSwapValues(updateUi: updateUi);
                    return;
                }

                // esitmate max payment amount and max fee
                //var swapParams = await Atomex.ViewModels.Helpers.
                //    EstimateSwapPaymentParamsAsync(
                //        amount: value,
                //        fromCurrency: FromCurrencyViewModel.Currency,
                //        toCurrency: ToCurrencyViewModel.Currency,
                //        account: AtomexApp.Account,
                //        atomexClient: AtomexApp.Terminal,
                //        symbolsProvider: AtomexApp.SymbolsProvider);

                //IsCriticalWarning = false;

                //if (swapParams.Error != null)
                //{
                //    Warning = swapParams.Error.Code switch
                //    {
                //        Errors.InsufficientFunds => AppResources.InsufficientFunds,
                //        Errors.InsufficientChainFunds => string.Format(CultureInfo.InvariantCulture, AppResources.InsufficientChainFunds, FromCurrencyViewModel.Currency.FeeCurrencyName),
                //        _ => AppResources.Error
                //    };
                //}
                //else
                //{
                //    Warning = string.Empty;
                //}

                //if (value > swapParams.Amount)
                //{
                //    Warning = AppResources.InsufficientFunds;
                //    ResetSwapValues(updateUi: false);
                //    return;
                //}

                //_estimatedPaymentFee = swapParams.PaymentFee;
                //_estimatedMakerNetworkFee = swapParams.MakerNetworkFee;
 
                //OnPropertyChanged(nameof(EstimatedPaymentFee));
                //OnPropertyChanged(nameof(EstimatedMakerNetworkFee));

                //IsAmountValid = _amount <= swapParams.Amount;

                //OnPropertyChanged(nameof(Amount));

                //if (updateUi)
                //{
                //    OnPropertyChanged(nameof(AmountString));
                //}

                //await UpdateRedeemAndRewardFeesAsync();

                //OnQuotesUpdatedEventHandler(AtomexApp.Terminal, null);
                //OnBaseQuotesUpdatedEventHandler(AtomexApp.QuotesProvider, EventArgs.Empty);
            }
            finally
            {
                IsAmountUpdating = false;
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

        private async Task UpdateRedeemAndRewardFeesAsync()
        {
            //var walletAddress = await AtomexApp.Account
            //    .GetCurrencyAccount(ToCurrencyViewModel.Currency.Name)
            //    .GetAddressAsync(RedeemAddress);

            //_estimatedRedeemFee = await ToCurrency
            //    .GetEstimatedRedeemFeeAsync(walletAddress, withRewardForRedeem: false);

            //_rewardForRedeem = await RewardForRedeemHelper
            //    .EstimateAsync(
            //        account: _app.Account,
            //        quotesProvider: _app.QuotesProvider,
            //        feeCurrencyQuotesProvider: symbol => _app.Terminal?.GetOrderBook(symbol)?.TopOfBook(),
            //        walletAddress: walletAddress);

            //_hasRewardForRedeem = _rewardForRedeem != 0;

            await Device.InvokeOnMainThreadAsync(() =>
            {
                OnPropertyChanged(nameof(EstimatedRedeemFee));
                OnPropertyChanged(nameof(RewardForRedeem));
                OnPropertyChanged(nameof(HasRewardForRedeem));
            });
        }

        private void OnTerminalChangedEventHandler(object sender, AtomexClientChangedEventArgs args)
        {
            var terminal = args.AtomexClient;

            if (terminal?.Account == null)
                return;

            if (Terminal != terminal && Terminal != null)
            {
                Terminal.QuotesUpdated -= OnQuotesUpdatedEventHandler;
                Terminal.SwapUpdated -= OnSwapEventHandler;
            }

            Terminal = terminal;

            Terminal.QuotesUpdated += OnQuotesUpdatedEventHandler;
            Terminal.SwapUpdated += OnSwapEventHandler;

            _currencyViewModels = terminal.Account.Currencies
                .Where(c => c.IsSwapAvailable)
                .Select(c => CurrencyViewModelCreator.CreateViewModel(AtomexApp, c, false))
                .ToList();

            FromCurrencies = _currencyViewModels.ToList();

            FromCurrencyViewModel = _currencyViewModels.FirstOrDefault();

            OnSwapEventHandler(this, null);
            OnQuotesUpdatedEventHandler(Terminal, null);
        }

        protected async void OnBaseQuotesUpdatedEventHandler(object sender, EventArgs args)
        {
            if (!(sender is ICurrencyQuotesProvider provider))
                return;

            if (_currencyCode == null || _targetCurrencyCode == null || _baseCurrencyCode == null)
                return;

            var fromCurrencyPrice = provider.GetQuote(_currencyCode, _baseCurrencyCode)?.Bid ?? 0m;
            _amountInBase = _amount * fromCurrencyPrice;

            var fromCurrencyFeePrice = provider.GetQuote(FromCurrencyViewModel?.Currency?.FeeCurrencyName, _baseCurrencyCode)?.Bid ?? 0m;
            _estimatedPaymentFeeInBase = _estimatedPaymentFee * fromCurrencyFeePrice;

            var toCurrencyFeePrice = provider.GetQuote(ToCurrencyViewModel?.Currency?.FeeCurrencyName, _baseCurrencyCode)?.Bid ?? 0m;
            _estimatedRedeemFeeInBase = _estimatedRedeemFee * toCurrencyFeePrice;

            var toCurrencyPrice = provider.GetQuote(TargetCurrencyCode, _baseCurrencyCode)?.Bid ?? 0m;
            _rewardForRedeemInBase = _rewardForRedeem * toCurrencyPrice;

            _estimatedMakerNetworkFeeInBase = _estimatedMakerNetworkFee * fromCurrencyPrice;

            _estimatedTotalNetworkFeeInBase =
                _estimatedPaymentFeeInBase +
                (!_hasRewardForRedeem ? _estimatedRedeemFeeInBase : 0) +
                _estimatedMakerNetworkFeeInBase +
                (_hasRewardForRedeem ? _rewardForRedeemInBase : 0);

            if (_amountInBase != 0 && _estimatedTotalNetworkFeeInBase / _amountInBase > 0.3m)
            {
                _isCriticalWarning = true;
                _warning = string.Format(
                    CultureInfo.InvariantCulture,
                    AppResources.TooHighNetworkFee,
                    FormattableString.Invariant($"{_estimatedTotalNetworkFeeInBase:0.00$}"),
                    FormattableString.Invariant($"{_estimatedTotalNetworkFeeInBase / _amountInBase:0.00%}"));
            }
            else if (_amountInBase != 0 && _estimatedTotalNetworkFeeInBase / _amountInBase > 0.1m)
            {
                IsCriticalWarning = false;
                Warning = string.Format(
                    CultureInfo.InvariantCulture,
                    AppResources.SufficientNetworkFee,
                    FormattableString.Invariant($"{_estimatedTotalNetworkFeeInBase:0.00$}"),
                    FormattableString.Invariant($"{_estimatedTotalNetworkFeeInBase / _amountInBase:0.00%}"));
            }

            _canConvert = _amountInBase == 0 || _estimatedTotalNetworkFeeInBase / _amountInBase <= 0.75m;

            await Device.InvokeOnMainThreadAsync(() =>
            {
                OnPropertyChanged(nameof(AmountInBase));
                OnPropertyChanged(nameof(EstimatedPaymentFeeInBase));
                OnPropertyChanged(nameof(EstimatedRedeemFeeInBase));
                OnPropertyChanged(nameof(RewardForRedeemInBase));

                OnPropertyChanged(nameof(EstimatedMakerNetworkFeeInBase));
                OnPropertyChanged(nameof(EstimatedTotalNetworkFeeInBase));

                OnPropertyChanged(nameof(IsCriticalWarning));
                OnPropertyChanged(nameof(Warning));
                OnPropertyChanged(nameof(CanConvert));

                UpdateTargetAmountInBase(provider);
            });
        }

        protected async void OnQuotesUpdatedEventHandler(object sender, MarketDataEventArgs args)
        {
            try
            {
                var swapPriceEstimation = await Atomex.ViewModels.Helpers.EstimateSwapPriceAsync(
                     amount: Amount,
                     fromCurrency: FromCurrencyViewModel.Currency,
                     toCurrency: ToCurrencyViewModel.Currency,
                     account: AtomexApp.Account,
                     atomexClient: AtomexApp.Terminal,
                     symbolsProvider: AtomexApp.SymbolsProvider);

                if (swapPriceEstimation == null)
                    return;

                _targetAmount = swapPriceEstimation.TargetAmount;
                _estimatedPrice = swapPriceEstimation.Price;
                _estimatedOrderPrice = swapPriceEstimation.OrderPrice;
                _estimatedMaxAmount = swapPriceEstimation.MaxAmount;
                _isNoLiquidity = swapPriceEstimation.IsNoLiquidity;

                await Device.InvokeOnMainThreadAsync(() =>
                {
                    OnPropertyChanged(nameof(EstimatedPrice));
                    OnPropertyChanged(nameof(EstimatedMaxAmount));
                    OnPropertyChanged(nameof(TargetAmount));
                    OnPropertyChanged(nameof(IsNoLiquidity));

                    UpdateTargetAmountInBase(AtomexApp.QuotesProvider);
                });
            }
            catch (Exception e)
            {
                Log.Error(e, "Quotes updated event handler error");
            }
        }

        private async void OnSwapEventHandler(object sender, SwapEventArgs args)
        {
            try
            {
                if (args == null)
                    return;

                await Device.InvokeOnMainThreadAsync(() =>
                {
                    if (_cachedSwaps.TryGetValue(args.Swap.Id, out SwapViewModel swap))
                    {
                        swap.UpdateSwap(args.Swap);
                    }
                    else
                    {
                        var swapViewModel = SwapViewModelFactory.CreateSwapViewModel(args.Swap, Currencies, AtomexApp.Account);
                        _cachedSwaps.Add(args.Swap.Id, swapViewModel);

                        Navigation.PushAsync(new SwapInfoPage(swapViewModel));

                        int pageNumber = Navigation.NavigationStack.Count;

                        for (int i = pageNumber - 2; i > 0; i--)
                        {
                            Navigation.RemovePage(Navigation.NavigationStack[i]);
                        }

                        Swaps.Add(swapViewModel);

                        var groups = Swaps
                            .GroupBy(p => p.LocalTime.Date)
                            .OrderByDescending(g => g.Key)
                            .Select(g => new Grouping<DateTime, SwapViewModel>(g.Key, new ObservableCollection<SwapViewModel>(g.OrderByDescending(g => g.LocalTime))));

                        GroupedSwaps = new ObservableCollection<Grouping<DateTime, SwapViewModel>>(groups);
                    }


                    OnPropertyChanged(nameof(Swaps));
                    OnPropertyChanged(nameof(GroupedSwaps));
                });
            }
            catch (Exception e)
            {
                Log.Error(e, "Swaps update error");
            }
        }

        private async void GetSwaps()
        {
            try
            {
                var swaps = await AtomexApp.Account
                    .GetSwapsAsync();

                await Device.InvokeOnMainThreadAsync(() =>
                {
                    Swaps = new ObservableCollection<SwapViewModel>();

                    if (swaps == null)
                        return;

                    foreach (var swap in swaps)
                    {
                        var swapViewModel = SwapViewModelFactory.CreateSwapViewModel(swap, Currencies, AtomexApp.Account);

                        long.TryParse(swapViewModel.Id, out long id);
                        _cachedSwaps.Add(id, swapViewModel);
                        Swaps.Add(swapViewModel);
                    }

                    var groups = Swaps
                        .GroupBy(p => p.LocalTime.Date)
                        .OrderByDescending(g => g.Key)
                        .Select(g => new Grouping<DateTime, SwapViewModel>(g.Key, new ObservableCollection<SwapViewModel>(g.OrderByDescending(g => g.LocalTime))));

                    GroupedSwaps = new ObservableCollection<Grouping<DateTime, SwapViewModel>>(groups);

                    OnPropertyChanged(nameof(Swaps));
                    OnPropertyChanged(nameof(GroupedSwaps));
                });
            }
            catch(Exception e)
            {
                Log.Error(e, "Get swaps error");
            }
        }

        private async void OnNextButtonClick()
        {
            if (String.IsNullOrWhiteSpace(Amount.ToString()))
            {
                await Application.Current.MainPage.DisplayAlert(AppResources.Warning, AppResources.EnterAmountLabel, AppResources.AcceptButton);
                return;
            }

            if (_amount <= 0)
            {
                await Application.Current.MainPage.DisplayAlert(AppResources.Error, AppResources.AmountLessThanZeroError, AppResources.AcceptButton);
                return;
            }

            if (!IsAmountValid)
            {
                await Application.Current.MainPage.DisplayAlert(AppResources.Error, AppResources.BigAmount, AppResources.AcceptButton);
                return;
            }

            if (EstimatedPrice == 0)
            {
                await Application.Current.MainPage.DisplayAlert(AppResources.Error, AppResources.NoLiquidityError, AppResources.AcceptButton);
                return;
            }

            if (!AtomexApp.Terminal.IsServiceConnected(TerminalService.All))
            {
                await Application.Current.MainPage.DisplayAlert(AppResources.Error, AppResources.ServicesUnavailable, AppResources.AcceptButton);
                return;
            }

            var symbol = Symbols.SymbolByCurrencies(FromCurrencyViewModel.Currency, ToCurrencyViewModel.Currency);
            if (symbol == null)
            {
                await Application.Current.MainPage.DisplayAlert(AppResources.Error, AppResources.NotSupportedSymbol, AppResources.AcceptButton);
                return;
            }

            var side = symbol.OrderSideForBuyCurrency(ToCurrencyViewModel.Currency);
            var price = EstimatedPrice;
            var baseCurrency = Currencies.GetByName(symbol.Base);
            var qty = AmountHelper.AmountToQty(side, _amount, price, baseCurrency.DigitsMultiplier);

            if (qty < symbol.MinimumQty)
            {
                var minimumAmount = AmountHelper.QtyToAmount(side, symbol.MinimumQty, price, FromCurrencyViewModel.Currency.DigitsMultiplier);
                var message = string.Format(CultureInfo.InvariantCulture, AppResources.MinimumAllowedQtyWarning, minimumAmount, FromCurrencyViewModel.Currency.Name);
                await Application.Current.MainPage.DisplayAlert(AppResources.Error, message, AppResources.AcceptButton);
                return;
            }

            var viewModel = new ConversionConfirmationViewModel(AtomexApp, Navigation)
            {
                FromCurrencyViewModel = FromCurrencyViewModel,
                ToCurrencyViewModel = ToCurrencyViewModel,

                CurrencyCode = CurrencyCode,
                TargetCurrencyCode = TargetCurrencyCode,
                BaseCurrencyCode = BaseCurrencyCode,

                FromFeeCurrencyCode = FromFeeCurrencyCode,
                TargetFeeCurrencyCode = TargetFeeCurrencyCode,

                Amount = _amount,
                AmountInBase = AmountInBase,
                TargetAmount = TargetAmount,
                TargetAmountInBase = TargetAmountInBase,

                EstimatedPrice = EstimatedPrice,
                EstimatedOrderPrice = _estimatedOrderPrice,
                EstimatedPaymentFee = EstimatedPaymentFee,
                EstimatedRedeemFee = EstimatedRedeemFee,
                EstimatedMakerNetworkFee = EstimatedMakerNetworkFee,

                EstimatedPaymentFeeInBase = EstimatedPaymentFeeInBase,
                EstimatedRedeemFeeInBase = EstimatedRedeemFeeInBase,
                EstimatedMakerNetworkFeeInBase = EstimatedMakerNetworkFeeInBase,
                EstimatedTotalNetworkFeeInBase = EstimatedTotalNetworkFeeInBase,

                RewardForRedeem = RewardForRedeem,
                RewardForRedeemInBase = RewardForRedeemInBase,
                HasRewardForRedeem = HasRewardForRedeem
            };

            viewModel.OnSuccess += OnSuccessConvertion;

            await Navigation.PushAsync(new ConfirmationPage(viewModel));
        }

        private void OnSuccessConvertion(object sender, EventArgs e)
        {
            _amount = Math.Min(_amount, EstimatedMaxAmount); // recalculate amount
            _ = UpdateAmountAsync(_amount, updateUi: true);
        }
    }
}
