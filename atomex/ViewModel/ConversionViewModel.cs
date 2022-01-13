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
using System.Windows.Input;
using atomex.Views.CreateSwap;
using Atomex.Services;
using Atomex.Wallet.Abstract;
using atomex.ViewModel.CurrencyViewModels;
using ReactiveUI.Fody.Helpers;
using ReactiveUI;
using System.Reactive.Linq;
using Rg.Plugins.Popup.Services;
using Xamarin.Essentials;
using ZXing;

namespace atomex.ViewModel
{
    public class ConversionViewModel : BaseViewModel
    {
        private readonly IAtomexApp _app;

        private ISymbols Symbols
        {
            get
            {
                return _app.SymbolsProvider
                    .GetSymbols(_app.Account.Network);
            }
        }
        private ICurrencies Currencies
        {
            get
            {
                return _app.Account.Currencies;
            }
        }

        public INavigation Navigation { get; set; }

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

        private IFromSource FromSource { get; set; }

        [Reactive]
        public decimal FromBalance { get; set; }

        [Reactive]
        public string ToAddress { get; set; }

        [Reactive]
        public string RedeemAddress { get; set; }

        [Reactive]
        public List<CurrencyViewModel> FromCurrencies { get; set; }

        [Reactive]
        public List<CurrencyViewModel> ToCurrencies { get; set; }

        [Reactive]
        public CurrencyViewModel FromCurrencyViewModel { get; set; }

        [Reactive]
        public CurrencyViewModel ToCurrencyViewModel { get; set; }

        [Reactive]
        public decimal Amount { get; set; }
        
        public string AmountString
        {
            get => Amount.ToString(CultureInfo.InvariantCulture);
            set
            {
                string temp = value.Replace(",", ".");
                if (!decimal.TryParse(
                    s: temp,
                    style: NumberStyles.AllowDecimalPoint,
                    provider: CultureInfo.InvariantCulture,
                    result: out var amount))
                {
                    Amount = 0;
                }
                else
                {
                    Amount = amount;

                    if (Amount > long.MaxValue)
                        Amount = long.MaxValue;
                }

                this.RaisePropertyChanged(nameof(Amount));
            }
        }

        [Reactive]
        public decimal AmountInBase { get; set; }

        [Reactive]
        public bool IsSwapParamsEstimating { get; set; }

        [Reactive]
        public bool IsAmountValid { get; set; }

        [Reactive]
        public decimal TargetAmount { get; set; }

        [Reactive]
        public decimal TargetAmountInBase { get; set; }

        private decimal _estimatedOrderPrice;

        [Reactive]
        public decimal EstimatedPrice { get; set; }

        [Reactive]
        public decimal EstimatedMaxAmount { get; set; }

        [Reactive]
        public decimal EstimatedMakerNetworkFee { get; set; }

        [Reactive]
        public decimal EstimatedMakerNetworkFeeInBase { get; set; }

        [Reactive]
        public decimal EstimatedPaymentFee { get; set; }

        [Reactive]
        public decimal EstimatedPaymentFeeInBase { get; set; }

        [Reactive]
        public decimal EstimatedRedeemFee { get; set; }

        [Reactive]
        public decimal EstimatedRedeemFeeInBase { get; set; }

        [Reactive]
        public decimal EstimatedTotalNetworkFeeInBase { get; set; }

        [Reactive]
        public decimal RewardForRedeem { get; set; }

        [Reactive]
        public decimal RewardForRedeemInBase { get; set; }

        [Reactive]
        public bool HasRewardForRedeem { get; set; }

        [Reactive]
        public string Warning { get; set; }

        [Reactive]
        public bool IsCriticalWarning { get; set; }

        [Reactive]
        public bool CanConvert { get; set; }

        [Reactive]
        public ObservableCollection<SwapViewModel> Swaps { get; set; }

        [Reactive]
        public ObservableCollection<Grouping<DateTime, SwapViewModel>> GroupedSwaps { get; set; }

        private Dictionary<long, SwapViewModel> _cachedSwaps;

        [Reactive]
        public bool IsNoLiquidity { get; set; }

        public enum SelectionAddressType
        {
            From,
            To,
            Redeem
        }

        [Reactive]
        public SelectionAddressType CurrentSelection { get; set; }

        [Reactive]
        public string AddressPageTitle { get; set; }

        [Reactive]
        public Result ScanResult { get; set; }

        [Reactive]
        public bool IsScanning { get; set; }

        [Reactive]
        public bool IsAnalyzing { get; set; }

        [Reactive]
        public bool IsAllSwapsShowed { get; set; }

        private int _swapNumberPerPage = 3;

        public ConversionViewModel(IAtomexApp app)
        {
            _app = app ?? throw new ArgumentNullException(nameof(app));

            FromCurrencies = new List<CurrencyViewModel>();
            ToCurrencies = new List<CurrencyViewModel>();
            _cachedSwaps = new Dictionary<long, SwapViewModel>();

            IsAmountValid = true;
            CanConvert = true;
            IsAllSwapsShowed = false;

            // "From" currency changed => Update "To" currencies list
            this.WhenAnyValue(vm => vm.FromCurrencyViewModel)
                .WhereNotNull()
                .Subscribe(c =>
                {
                    ToCurrencies = FromCurrencies
                        .Where(fc => Symbols.SymbolByCurrencies(fc.Currency, c.Currency) != null)
                        .ToList();

                    //ToCurrencyViewModel = ToCurrencies.FirstOrDefault();
                });

            // Amoung, "From" currency or  "To" currency changed => estimate swap price and target amount
            this.WhenAnyValue(vm => vm.Amount, vm => vm.FromCurrencyViewModel, vm => vm.ToCurrencyViewModel)
                .Throttle(TimeSpan.FromMilliseconds(1))
                .Subscribe(a =>
                {
                    _ = EstimateSwapParamsAsync(amount: Amount);
                    OnQuotesUpdatedEventHandler(sender: this, args: null);
                });

            // Amount changed => update AmountInBase
            this.WhenAnyValue(vm => vm.Amount)
                .Subscribe(amount => UpdateAmountInBase());

            // TargetAmount changed => update TargetAmountInBase
            this.WhenAnyValue(vm => vm.TargetAmount)
                .Subscribe(amount => UpdateTargetAmountInBase());

            // EstimatedPaymentFee changed => update EstimatedPaymentFeeInBase
            this.WhenAnyValue(vm => vm.EstimatedPaymentFee)
                .Subscribe(amount => UpdateEstimatedPaymentFeeInBase());

            // EstimatedRedeemFee changed => update EstimatedRedeemFeeInBase
            this.WhenAnyValue(vm => vm.EstimatedRedeemFee)
                .Subscribe(amount => UpdateEstimatedRedeemFeeInBase());

            // RewardForRedeem changed => update RewardForRedeemInBase
            this.WhenAnyValue(vm => vm.RewardForRedeem)
                .Subscribe(amount => UpdateRewardForRedeemInBase());

            // EstimatedMakerNetworkFee changed => update EstimatedMakerNetworkFeeInBase
            this.WhenAnyValue(vm => vm.EstimatedMakerNetworkFee)
                .Subscribe(amount => UpdateEstimatedMakerNetworkFeeInBase());

            // If fees in base currency changed => update TotalNetworkFeeInBase
            this.WhenAnyValue(
                    vm => vm.EstimatedPaymentFeeInBase,
                    vm => vm.HasRewardForRedeem,
                    vm => vm.EstimatedRedeemFeeInBase,
                    vm => vm.EstimatedMakerNetworkFeeInBase,
                    vm => vm.RewardForRedeemInBase)
                .Throttle(TimeSpan.FromMilliseconds(1))
                .Subscribe(t => UpdateTotalNetworkFeeInBase());

            // AmountInBase or EstimatedTotalNetworkFeeInBase changed => check the ratio of the fee to the amount
            this.WhenAnyValue(vm => vm.AmountInBase, vm => vm.EstimatedTotalNetworkFeeInBase)
                .Subscribe(t => CheckAmountToFeeRatio());

            // CurrentSelection changed => set addresses list bindings and page title
            this.WhenAnyValue(vm => vm.CurrentSelection)
                .Subscribe(t => SetBindingsOnAddressesPage());

            
            this.WhenAnyValue(vm => vm.IsAllSwapsShowed)
                .Subscribe(t => GetSwaps());

            SubscribeToServices();
        }

        private ICommand _maxAmountCommand;
        public ICommand MaxAmountCommand => _maxAmountCommand ??= ReactiveCommand.Create(async () =>
        {
            try
            {
                if (FromCurrencyViewModel == null || ToCurrencyViewModel == null)
                    return;

                var swapParams = await Atomex.ViewModels.Helpers
                    .EstimateSwapParamsAsync(
                        from: FromSource,
                        amount: EstimatedMaxAmount,
                        redeemFromAddress: RedeemAddress,
                        fromCurrency: FromCurrencyViewModel.Currency,
                        toCurrency: ToCurrencyViewModel.Currency,
                        account: _app.Account,
                        atomexClient: _app.Terminal,
                        symbolsProvider: _app.SymbolsProvider,
                        quotesProvider: _app.QuotesProvider);

                //if (swapParams.Error != null) {
                //    TODO: warning?
                //}

                await Device.InvokeOnMainThreadAsync(() =>
                {
                    AmountString = Math.Min(swapParams.Amount, EstimatedMaxAmount).ToString();
                    this.RaisePropertyChanged(nameof(AmountString));
                });
            }
            catch (Exception e)
            {
                Log.Error(e, "Max amount command error.");
            }
        });

        private ICommand _swapCurrenciesCommand;
        public ICommand SwapCurrenciesCommand => _swapCurrenciesCommand ??= ReactiveCommand.Create(() =>
        {
            if (FromCurrencyViewModel == null || ToCurrencyViewModel == null)
                return;

            var previousFromCurrency = FromCurrencyViewModel;
            FromCurrencyViewModel = ToCurrencyViewModel;
            ToCurrencyViewModel = previousFromCurrency;
        });

        private void SubscribeToServices()
        {
            _app.AtomexClientChanged += OnTerminalChangedEventHandler;

            if (_app.HasQuotesProvider)
                _app.QuotesProvider.QuotesUpdated += OnBaseQuotesUpdatedEventHandler;

            OnTerminalChangedEventHandler(this, new AtomexClientChangedEventArgs(_app.Terminal));
        }

        protected virtual async Task EstimateSwapParamsAsync(decimal amount)
        {

            if (FromCurrencyViewModel == null || ToCurrencyViewModel == null)
                return;

            // estimate max payment amount and max fee
            var swapParams = await Atomex.ViewModels.Helpers
                .EstimateSwapParamsAsync(
                    from: FromSource,
                    amount: amount,
                    redeemFromAddress: RedeemAddress,
                    fromCurrency: FromCurrencyViewModel?.Currency,
                    toCurrency: ToCurrencyViewModel?.Currency,
                    account: _app.Account,
                    atomexClient: _app.Terminal,
                    symbolsProvider: _app.SymbolsProvider,
                    quotesProvider: _app.QuotesProvider);

            await Device.InvokeOnMainThreadAsync(() =>
            {
                Warning = string.Empty;
                IsCriticalWarning = false;

                if (swapParams.Error != null)
                {
                    Warning = swapParams.Error.Code switch
                    {
                        Errors.InsufficientFunds => AppResources.InsufficientFunds,
                        Errors.InsufficientChainFunds => string.Format(
                            CultureInfo.InvariantCulture,
                            AppResources.InsufficientChainFunds,
                            FromCurrencyViewModel?.Currency.FeeCurrencyName),
                        _ => AppResources.Error
                    };
                }
                else
                {
                    Warning = string.Empty;
                }

                EstimatedPaymentFee = swapParams.PaymentFee;
                EstimatedRedeemFee = swapParams.RedeemFee;
                RewardForRedeem = swapParams.RewardForRedeem;
                EstimatedMakerNetworkFee = swapParams.MakerNetworkFee;

                if (FromCurrencyViewModel != null)
                    IsAmountValid = Amount <= swapParams.Amount;
            });
        }

        private static decimal TryGetAmountInBase(
            decimal amount,
            string currency,
            string baseCurrency,
            ICurrencyQuotesProvider provider,
            decimal defaultAmountInBase = 0)
        {
            if (currency == null || baseCurrency == null || provider == null)
                return defaultAmountInBase;

            var quote = provider.GetQuote(currency, baseCurrency);
            return amount * (quote?.Bid ?? 0m);
        }

        private void UpdateAmountInBase() => AmountInBase = TryGetAmountInBase(
            amount: Amount,
            currency: FromCurrencyViewModel?.CurrencyCode,
            baseCurrency: FromCurrencyViewModel?.BaseCurrencyCode,
            provider: _app.QuotesProvider,
            defaultAmountInBase: AmountInBase);

        private void UpdateTargetAmountInBase() => TargetAmountInBase = TryGetAmountInBase(
            amount: TargetAmount,
            currency: ToCurrencyViewModel?.CurrencyCode,
            baseCurrency: FromCurrencyViewModel?.BaseCurrencyCode,
            provider: _app.QuotesProvider,
            defaultAmountInBase: TargetAmountInBase);

        private void UpdateEstimatedPaymentFeeInBase() => EstimatedPaymentFeeInBase = TryGetAmountInBase(
            amount: EstimatedPaymentFee,
            currency: FromCurrencyViewModel?.Currency?.FeeCurrencyName,
            baseCurrency: FromCurrencyViewModel?.BaseCurrencyCode,
            provider: _app.QuotesProvider,
            defaultAmountInBase: EstimatedPaymentFeeInBase);

        private void UpdateEstimatedRedeemFeeInBase() => EstimatedRedeemFeeInBase = TryGetAmountInBase(
            amount: EstimatedRedeemFee,
            currency: ToCurrencyViewModel?.Currency?.FeeCurrencyName,
            baseCurrency: FromCurrencyViewModel?.BaseCurrencyCode,
            provider: _app.QuotesProvider,
            defaultAmountInBase: EstimatedRedeemFeeInBase);

        private void UpdateRewardForRedeemInBase() => RewardForRedeemInBase = TryGetAmountInBase(
            amount: RewardForRedeem,
            currency: ToCurrencyViewModel?.CurrencyCode,
            baseCurrency: FromCurrencyViewModel?.BaseCurrencyCode,
            provider: _app.QuotesProvider,
            defaultAmountInBase: RewardForRedeemInBase);

        private void UpdateEstimatedMakerNetworkFeeInBase() => EstimatedMakerNetworkFeeInBase = TryGetAmountInBase(
            amount: EstimatedMakerNetworkFee,
            currency: FromCurrencyViewModel?.CurrencyCode,
            baseCurrency: FromCurrencyViewModel?.BaseCurrencyCode,
            provider: _app.QuotesProvider,
            defaultAmountInBase: EstimatedMakerNetworkFeeInBase);

        private void UpdateTotalNetworkFeeInBase() =>
            EstimatedTotalNetworkFeeInBase = EstimatedPaymentFeeInBase +
                (!HasRewardForRedeem ? EstimatedRedeemFeeInBase : 0) +
                EstimatedMakerNetworkFeeInBase +
                (HasRewardForRedeem ? RewardForRedeemInBase : 0);

        private void OnTerminalChangedEventHandler(object sender, AtomexClientChangedEventArgs args)
        {
            var terminal = args.AtomexClient;

            if (terminal?.Account == null)
                return;

            terminal.QuotesUpdated += OnQuotesUpdatedEventHandler;
            terminal.SwapUpdated += OnSwapEventHandler;

            FromCurrencies = terminal.Account.Currencies
                .Where(c => c.IsSwapAvailable)
                .Select(c => CurrencyViewModelCreator.CreateViewModel(_app, c, false))
                .ToList();

            //FromCurrencyViewModel = FromCurrencies.First(c => c.Currency.Name == "BTC");
            //ToCurrencyViewModel = ToCurrencies.First(c => c.Currency.Name == "LTC");

            OnSwapEventHandler(this, args: null);
            OnQuotesUpdatedEventHandler(this, args: null);
        }

        private void CheckAmountToFeeRatio()
        {
            if (AmountInBase != 0 && EstimatedTotalNetworkFeeInBase / AmountInBase > 0.3m)
            {
                IsCriticalWarning = true;
                Warning = string.Format(
                    CultureInfo.InvariantCulture,
                    AppResources.TooHighNetworkFee,
                    FormattableString.Invariant($"{EstimatedTotalNetworkFeeInBase:$0.00}"),
                    FormattableString.Invariant($"{EstimatedTotalNetworkFeeInBase / AmountInBase:0.00%}"));
            }
            else if (AmountInBase != 0 && EstimatedTotalNetworkFeeInBase / AmountInBase > 0.1m)
            {
                IsCriticalWarning = false;
                Warning = string.Format(
                    CultureInfo.InvariantCulture,
                    AppResources.SufficientNetworkFee,
                    FormattableString.Invariant($"{EstimatedTotalNetworkFeeInBase:$0.00}"),
                    FormattableString.Invariant($"{EstimatedTotalNetworkFeeInBase / AmountInBase:0.00%}"));
            }

            CanConvert = AmountInBase == 0 || EstimatedTotalNetworkFeeInBase / AmountInBase <= 0.75m;
        }

        private async void SetBindingsOnAddressesPage()
        {
            await Device.InvokeOnMainThreadAsync(() =>
            {
                if (CurrentSelection == SelectionAddressType.From)
                {
                    //FoundAddress = FromAddresses
                    AddressPageTitle = AppResources.SendFrom;
                    return;
                }
                if (CurrentSelection == SelectionAddressType.To)
                {
                    //FoundAddress = MyAddresses
                    AddressPageTitle = AppResources.ChooseAnAddress;
                    return;
                }
                if (CurrentSelection == SelectionAddressType.Redeem)
                {
                    //FoundAddress = MyAddresses ?
                    AddressPageTitle = AppResources.ChangeRedeemAddress;
                    return;
                }
            });
        }

        protected async void OnBaseQuotesUpdatedEventHandler(object sender, EventArgs args)
        {
            await Device.InvokeOnMainThreadAsync(() =>
            {
                UpdateAmountInBase();
                UpdateEstimatedPaymentFeeInBase();
                UpdateEstimatedRedeemFeeInBase();
                UpdateRewardForRedeemInBase();
                UpdateEstimatedMakerNetworkFeeInBase();
                UpdateTotalNetworkFeeInBase();
            });
        }

        protected async void OnQuotesUpdatedEventHandler(object sender, MarketDataEventArgs args)
        {
            try
            {
                var swapPriceEstimation = await Atomex.ViewModels.Helpers.EstimateSwapPriceAsync(
                    amount: Amount,
                    fromCurrency: FromCurrencyViewModel?.Currency,
                    toCurrency: ToCurrencyViewModel?.Currency,
                    account: _app.Account,
                    atomexClient: _app.Terminal,
                    symbolsProvider: _app.SymbolsProvider);

                if (swapPriceEstimation == null)
                    return;

                await Device.InvokeOnMainThreadAsync(() =>
                {
                    _estimatedOrderPrice = swapPriceEstimation.OrderPrice;

                    TargetAmount = swapPriceEstimation.TargetAmount;
                    EstimatedPrice = swapPriceEstimation.Price;
                    EstimatedMaxAmount = swapPriceEstimation.MaxAmount;
                    IsNoLiquidity = swapPriceEstimation.IsNoLiquidity;

                });
            }
            catch (Exception e)
            {
                Log.Error(e, "Quotes updated event handler error");
            }
        }

        public void SetFromCurrency(string currencyCode)
        {
            try
            {
                FromCurrencyViewModel = FromCurrencies.FirstOrDefault(vm => vm.Currency?.Name == currencyCode);
            }
            catch(Exception e)
            {
                Log.Error(e, "SetFromCurrency error");
            }
        }

        private ICommand _nextCommand;
        public ICommand NextCommand => _nextCommand ??= new Command(OnNextButtonClick);

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


        //private ICommand _amoutPageCommand;
        //public ICommand AmoutPageCommand => _amoutPageCommand ??= new Command(async () => await ShowAmountPage());

        //private async Task ShowAmountPage()
        //{
        //    if (FromCurrencyViewModel == null || ToCurrencyViewModel == null)
        //    {
        //        Warning = "Select 'From' and 'To' currencies";
        //        return;
        //    }
        //    Warning = string.Empty;
        //    await Navigation.PushAsync(new AmountPage(this));
        //}

        private ICommand _createNewSwapCommand;
        public ICommand CreateNewSwapCommand => _createNewSwapCommand ??= new Command(async () => await OnCreateSwapButtonClicked());

        private async Task OnCreateSwapButtonClicked()
        {
            await Navigation.PushAsync(new ExchangePage(this));
        }

        private ICommand _showFromCurrenciesCommand;
        public ICommand ShowFromCurrenciesCommand => _showFromCurrenciesCommand ??= new Command(async () => await OnFromCurrencyTapped());

        private async Task OnFromCurrencyTapped()
        {
            await Navigation.PushAsync(new FromCurrenciesPage(this));
        }

        private ICommand _showToCurrenciesCommand;
        public ICommand ShowToCurrenciesCommand => _showToCurrenciesCommand ??= new Command(async () => await OnToCurrencyTapped());

        private async Task OnToCurrencyTapped()
        {
            if (FromCurrencyViewModel == null)
            {
                await Application.Current.MainPage.DisplayAlert(AppResources.Warning, "Choose the From currency first", AppResources.AcceptButton);
                return;
            }

            await Navigation.PushAsync(new ToCurrenciesPage(this));
        }

        private ICommand _selectFromCurrencyCommand;
        public ICommand SelectFromCurrencyCommand => _selectFromCurrencyCommand ??= new Command<CurrencyViewModel>(async (value) => await OnFromCurrencyTapped(value));

        private async Task OnFromCurrencyTapped(CurrencyViewModel currency)
        {
            if (currency == null)
                return;

            FromCurrencyViewModel = currency;

            await Navigation.PopAsync();
        }

        private ICommand _selectToCurrencyCommand;
        public ICommand SelectToCurrencyCommand => _selectToCurrencyCommand ??= new Command<CurrencyViewModel>(async (value) => await OnToCurrencyTapped(value));

        private async Task OnToCurrencyTapped(CurrencyViewModel currency)
        {
            if (currency == null)
                return;

            ToCurrencyViewModel = currency;

            await Navigation.PopAsync();
        }

        private ICommand _searchAddressCommand;
        public ICommand SearchAddressCommand => _searchAddressCommand ??= new Command<string>((value) => OnSearchEntryTextChanged(value));

        private void OnSearchEntryTextChanged(string value)
        {
            try
            {
                Console.WriteLine("Search");
                //if (CurrentSelection == SelectionAddressType.From)
                //    FoundAddressesList = FromAddresses;
                //else if (CurrentSelection == SelectionAddressType.To)
                //    FoundAddressesList = ToAddresses;
                //else (CurrentSelection == SelectionAddressType.Redeem)
                //    FoundAddressesList = ??;
                
                //if (string.IsNullOrEmpty(value))
                //    FoundAddressesList = AddressesList;
                //else
                //    FoundAddressesList = AddressesList.Where(x => x.Name.ToLower().Contains(value.ToLower())).ToList();
            }
            catch (Exception e)
            {
                Log.Error(e.Message);
            }
        }

        private ICommand _showFromAddressesCommand;
        public ICommand ShowFromAddressesCommand => _showFromAddressesCommand ??= new Command(async () => await ShowFromAddresses());


        private async Task ShowFromAddresses()
        {
            if (FromCurrencyViewModel == null)
                return;

            CurrentSelection = SelectionAddressType.From;

            if (FromCurrencyViewModel.Currency is BitcoinBasedConfig)
            {
                await Navigation.PushAsync(new BitcoinBasedAddressesPage(this));
                return;
            }

            await Navigation.PushAsync(new Views.CreateSwap.AddressesPage(this));
        }

        private ICommand _showToAddressesCommand;
        public ICommand ShowToAddressesCommand => _showToAddressesCommand ??= new Command(async () => await ShowToAddresses());

        private async Task ShowToAddresses()
        {
            CurrentSelection = SelectionAddressType.To;
            await PopupNavigation.Instance.PopAsync();
            await Navigation.PushAsync(new Views.CreateSwap.AddressesPage(this));
        }

        private ICommand _showRedeemAddressesCommand;
        public ICommand ShowRedeemAddressesCommand => _showRedeemAddressesCommand ??= new Command(async () => await ShowRedeemAddresses());

        private async Task ShowRedeemAddresses()
        {
            CurrentSelection = SelectionAddressType.Redeem;
            await Navigation.PushAsync(new Views.CreateSwap.AddressesPage(this));
        }

        private ICommand _editToAddressCommand;
        public ICommand EditToAddressCommand => _editToAddressCommand ??= new Command(async () => await EditToAddresses());

        private async Task EditToAddresses()
        {
            await PopupNavigation.Instance.PushAsync(new AddressesBottomSheet(this));
        }

        private ICommand _externalAddressCommand;
        public ICommand ExternalAddressCommand => _externalAddressCommand ??= new Command(async () => await EnterExternalAddress());

        private async Task EnterExternalAddress()
        {
            await PopupNavigation.Instance.PopAsync();
            await Navigation.PushAsync(new ExternalAddressPage(this));
        }

        private ICommand _scanAddressCommand;
        public ICommand ScanAddressCommand => _scanAddressCommand ??= new Command(async () => await OnScanButtonClicked());

        private ICommand _scanResultCommand;
        public ICommand ScanResultCommand => _scanResultCommand ??= new Command(async () => await OnScanResult());

        private async Task OnScanButtonClicked()
        {
            Console.WriteLine("Scan");

            PermissionStatus permissions = await Permissions.CheckStatusAsync<Permissions.Camera>();

            if (permissions != PermissionStatus.Granted)
                permissions = await Permissions.RequestAsync<Permissions.Camera>();
            if (permissions != PermissionStatus.Granted)
                return;

            IsScanning = IsAnalyzing = true;

            this.RaisePropertyChanged(nameof(IsScanning));
            this.RaisePropertyChanged(nameof(IsAnalyzing));

            await Navigation.PushAsync(new ScanningQrPage(this));
        }

        private async Task OnScanResult()
        {
            IsScanning = IsAnalyzing = false;
            this.RaisePropertyChanged(nameof(IsScanning));
            this.RaisePropertyChanged(nameof(IsAnalyzing));

            if (ScanResult == null)
            {
                await Application.Current.MainPage.DisplayAlert(AppResources.Error, "Incorrect QR code format", AppResources.AcceptButton);

                Device.BeginInvokeOnMainThread(async () =>
                {
                    await Navigation.PopAsync();
                });
                return;
            }

            Device.BeginInvokeOnMainThread(async () =>
            {
                int indexOfChar = ScanResult.Text.IndexOf(':');
                //if (indexOfChar == -1)
                //    To = ScanResult.Text;
                //else
                //    To = ScanResult.Text.Substring(indexOfChar + 1);
                await Navigation.PopAsync();
            });
        }

        private ICommand _showAllSwapsCommand;
        public ICommand ShowAllSwapsCommand => _showAllSwapsCommand ??= new Command(ShowAllSwaps);

        private void ShowAllSwaps()
        {
            IsAllSwapsShowed = true;
        }

        private ICommand _selectSwapCommand;
        public ICommand SelectSwapCommand => _selectSwapCommand ??= new Command<SwapViewModel>(async (value) => await OnSwapTapped(value));

        private async Task OnSwapTapped(SwapViewModel swap)
        {
            if (swap != null)
                await Navigation.PushAsync(new SwapInfoPage(swap));
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
                        var swapViewModel = SwapViewModelFactory.CreateSwapViewModel(args.Swap, Currencies, _app.Account);
                        _cachedSwaps.Add(args.Swap.Id, swapViewModel);

                        Navigation.PushAsync(new SwapInfoPage(swapViewModel));

                        int pageNumber = Navigation.NavigationStack.Count;

                        for (int i = pageNumber - 2; i > 0; i--)
                        {
                            Navigation.RemovePage(Navigation.NavigationStack[i]);
                        }

                        Swaps.Add(swapViewModel);

                        var groups = !IsAllSwapsShowed
                            ? Swaps
                               .OrderByDescending(p => p.LocalTime.Date)
                               .Take(_swapNumberPerPage)
                               .GroupBy(p => p.LocalTime.Date)
                               .Select(g => new Grouping<DateTime, SwapViewModel>(g.Key, new ObservableCollection<SwapViewModel>(g.OrderByDescending(g => g.LocalTime))))
                            : Swaps
                                .GroupBy(p => p.LocalTime.Date)
                                .OrderByDescending(g => g.Key)
                                .Select(g => new Grouping<DateTime, SwapViewModel>(g.Key, new ObservableCollection<SwapViewModel>(g.OrderByDescending(g => g.LocalTime))));

                        GroupedSwaps = new ObservableCollection<Grouping<DateTime, SwapViewModel>>(groups);

                        this.RaisePropertyChanged(nameof(Swaps));
                        this.RaisePropertyChanged(nameof(GroupedSwaps));
                    }
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
                var swaps = await _app.Account
                    .GetSwapsAsync();

                await Device.InvokeOnMainThreadAsync(() =>
                {
                    Swaps = new ObservableCollection<SwapViewModel>();

                    if (swaps == null)
                        return;

                    _cachedSwaps.Clear();

                    foreach (var swap in swaps)
                    {
                        var swapViewModel = SwapViewModelFactory.CreateSwapViewModel(swap, Currencies, _app.Account);

                        long.TryParse(swapViewModel.Id, out long id);
                        _cachedSwaps.Add(id, swapViewModel);
                        Swaps.Add(swapViewModel);
                    }

                    var groups = !IsAllSwapsShowed
                        ? Swaps
                           .OrderByDescending(p => p.LocalTime.Date)
                           .Take(_swapNumberPerPage)
                           .GroupBy(p => p.LocalTime.Date)
                           .Select(g => new Grouping<DateTime, SwapViewModel>(g.Key, new ObservableCollection<SwapViewModel>(g.OrderByDescending(g => g.LocalTime))))
                        : Swaps
                            .GroupBy(p => p.LocalTime.Date)
                            .OrderByDescending(g => g.Key)
                            .Select(g => new Grouping<DateTime, SwapViewModel>(g.Key, new ObservableCollection<SwapViewModel>(g.OrderByDescending(g => g.LocalTime))));

                    GroupedSwaps = new ObservableCollection<Grouping<DateTime, SwapViewModel>>(groups);

                    this.RaisePropertyChanged(nameof(Swaps));
                    this.RaisePropertyChanged(nameof(GroupedSwaps));
                });
            }
            catch(Exception e)
            {
                Log.Error(e, "Get swaps error");
            }
        }

        private async void OnNextButtonClick()
        {
            //if (String.IsNullOrWhiteSpace(Amount.ToString()))
            //{
            //    await Application.Current.MainPage.DisplayAlert(
            //        AppResources.Warning,
            //        AppResources.EnterAmountLabel,
            //        AppResources.AcceptButton);
            //    return;
            //}

            if (Amount <= 0)
            {
                await Application.Current.MainPage.DisplayAlert(
                    AppResources.Error,
                    AppResources.AmountLessThanZeroError,
                    AppResources.AcceptButton);
                return;
            }

            if (!IsAmountValid)
            {
                await Application.Current.MainPage.DisplayAlert(
                    AppResources.Error,
                    AppResources.BigAmount,
                    AppResources.AcceptButton);
                return;
            }

            if (EstimatedPrice == 0)
            {
                await Application.Current.MainPage.DisplayAlert(
                    AppResources.Error,
                    AppResources.NoLiquidityError,
                    AppResources.AcceptButton);
                return;
            }

            if (!_app.Terminal.IsServiceConnected(TerminalService.All))
            {
                await Application.Current.MainPage.DisplayAlert(
                    AppResources.Error,
                    AppResources.ServicesUnavailable,
                    AppResources.AcceptButton);
                return;
            }

            var symbol = Symbols.SymbolByCurrencies(FromCurrencyViewModel.Currency, ToCurrencyViewModel.Currency);
            if (symbol == null)
            {
                await Application.Current.MainPage.DisplayAlert(
                    AppResources.Error,
                    AppResources.NotSupportedSymbol,
                    AppResources.AcceptButton);
                return;
            }

            var side = symbol.OrderSideForBuyCurrency(ToCurrencyViewModel.Currency);
            var price = EstimatedPrice;
            var baseCurrency = Currencies.GetByName(symbol.Base);
            var qty = AmountHelper.AmountToQty(
                side: side,
                amount: Amount,
                price: price,
                digitsMultiplier: baseCurrency.DigitsMultiplier);

            if (qty < symbol.MinimumQty)
            {
                var minimumAmount = AmountHelper.QtyToAmount(
                    side: side,
                    qty: symbol.MinimumQty,
                    price: price,
                    digitsMultiplier: FromCurrencyViewModel.Currency.DigitsMultiplier);
                var message = string.Format(
                    CultureInfo.InvariantCulture,
                    AppResources.MinimumAllowedQtyWarning,
                    minimumAmount,
                    FromCurrencyViewModel.Currency.Name);
                await Application.Current.MainPage.DisplayAlert(
                    AppResources.Error,
                    message,
                    AppResources.AcceptButton);
                return;
            }

            var viewModel = new ConversionConfirmationViewModel(_app, Navigation)
            {
                FromCurrencyViewModel = FromCurrencyViewModel,
                ToCurrencyViewModel = ToCurrencyViewModel,

                Amount = Amount,
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

            //await Navigation.PushAsync(new ConfirmationPage(viewModel));
        }

        private void OnSuccessConvertion(object sender, EventArgs e)
        {
            Amount = Math.Min(Amount, EstimatedMaxAmount); // recalculate amount
            _ = EstimateSwapParamsAsync(Amount);
        }
    }
}
