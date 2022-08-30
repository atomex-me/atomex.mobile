using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using atomex.Common;
using atomex.Resources;
using atomex.Views;
using atomex.Views.TezosTokens;
using Atomex;
using Atomex.Blockchain.Tezos;
using Atomex.Common;
using Atomex.MarketData.Abstract;
using Atomex.TezosTokens;
using atomex.ViewModels.SendViewModels;
using atomex.ViewModels.TransactionViewModels;
using atomex.Views.Popup;
using Atomex.Wallet;
using Atomex.Wallet.Abstract;
using Atomex.Wallet.Tezos;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Serilog;
using Xamarin.Essentials;
using Xamarin.Forms;
using static atomex.Models.SnackbarMessage;

namespace atomex.ViewModels.CurrencyViewModels
{
    public class TezosTokenViewModel : BaseViewModel
    {
        protected IAtomexApp _app;
        protected IAccount _account;
        protected INavigationService _navigationService;

        public TezosConfig TezosConfig { get; set; }
        public TokenBalance TokenBalance { get; set; }
        public TokenContract Contract { get; set; }
        public static string BaseCurrencyCode => "USD";
        public string CurrencyCode => TokenBalance?.Symbol ?? "TOKENS";
        public string Description => TokenBalance?.Name ?? TokenBalance?.Symbol;

        public bool IsConvertable => _app?.Account?.Currencies
            .Any(c => c is Fa12Config fa12 && fa12?.TokenContractAddress == Contract.Address) ?? false;

        [Reactive] public decimal TotalAmount { get; set; }
        [Reactive] public decimal TotalAmountInBase { get; set; }
        [Reactive] public decimal CurrentQuote { get; set; }

        [Reactive] public ObservableCollection<TransactionViewModel> Transactions { get; set; }

        [Reactive]
        public ObservableCollection<Grouping<TransactionViewModel>> GroupedTransactions { get; set; }

        [Reactive] public TransactionViewModel SelectedTransaction { get; set; }

        [Reactive] public AddressesViewModel AddressesViewModel { get; set; }
        [Reactive] public ObservableCollection<AddressViewModel> Addresses { get; set; }

        [Reactive] public bool IsTransfersLoading { get; set; }
        [Reactive] public bool CanShowMoreTxs { get; set; }
        [Reactive] public bool IsAllTxsShowed { get; set; }
        [Reactive] public bool CanShowMoreAddresses { get; set; }

        public int TxsNumberPerPage = 3;
        public int AddressesNumberPerPage = 3;

        public const double DefaultTxGroupHeight = 36;
        public double DefaultTxRowHeight = 76;
        public const double DefaultAddressRowHeight = 64;
        public const double ListViewFooterHeight = 72;
        [Reactive] public double TxListViewHeight { get; set; }
        [Reactive] public double AddressListViewHeight { get; set; }

        private CancellationTokenSource _cancellationTokenSource;

        public class Grouping<T> : ObservableCollection<T>
        {
            public double GroupHeight { get; set; } = DefaultTxGroupHeight;
            public DateTime Date { get; private set; }

            public Grouping(DateTime date, IEnumerable<T> items)
            {
                Date = date;

                foreach (T item in items)
                    Items.Add(item);
            }

            public string DateString => Date.ToString(AppResources.Culture.DateTimeFormat.MonthDayPattern, AppResources.Culture);
        }

        [Reactive] public bool IsRefreshing { get; set; }
        [Reactive] public CurrencyTab SelectedTab { get; set; }

        private ThumbsApi ThumbsApi => new(
            new ThumbsApiSettings
            {
                ThumbsApiUri = TezosConfig.ThumbsApiUri,
                IpfsGatewayUri = TezosConfig.IpfsGatewayUri,
                CatavaApiUri = TezosConfig.CatavaApiUri
            });

        [Reactive] public ImageSource TokenPreview { get; set; }

        protected ImageSource GetTokenPreview(string url)
        {
            var hasImageInCache = CacheHelper
                .HasCacheAsync(new Uri(url))
                .WaitForResult();

            if (hasImageInCache)
            {
                return new UriImageSource
                {
                    Uri = new Uri(url),
                    CachingEnabled = true,
                    CacheValidity = new TimeSpan(365, 0, 0, 0)
                };
            }

            var downloaded = CacheHelper
                .SaveToCacheAsync(new Uri(url))
                .WaitForResult();

            if (downloaded)
            {
                return new UriImageSource
                {
                    Uri = new Uri(url),
                    CachingEnabled = true,
                    CacheValidity = new TimeSpan(365, 0, 0, 0)
                };
            }

            return null;
        }

        protected void InitTokenPreview()
        {
            if (TokenBalance == null) return;

            var url = ThumbsApi.GetTokenPreviewUrl(Contract.Address, TokenBalance.TokenId);
            TokenPreview = GetTokenPreview(url);
        }

        public TezosTokenViewModel(
            IAtomexApp app,
            INavigationService navigationService,
            TokenContract contract,
            TokenBalance tokenBalance)
        {
            Transactions = new ObservableCollection<TransactionViewModel>();
            GroupedTransactions = new ObservableCollection<Grouping<TransactionViewModel>>();

            _app = app ?? throw new ArgumentNullException(nameof(app));
            _account = app.Account ?? throw new ArgumentNullException(nameof(app.Account));
            _navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));
            Contract = contract;
            TokenBalance = tokenBalance;

            TezosConfig = _app.Account
                .Currencies
                .Get<TezosConfig>(TezosConfig.Xtz);

            this.WhenAnyValue(vm => vm.TotalAmount)
                .WhereNotNull()
                .Where(_ => _app != null)
                .SubscribeInMainThread(_ => UpdateQuotesInBaseCurrency(_app.QuotesProvider));

            this.WhenAnyValue(vm => vm.SelectedTransaction)
                .WhereNotNull()
                .SubscribeInMainThread(t =>
                {
                    _navigationService?.ShowPage(new TransactionInfoPage(t), TabNavigation.Portfolio);
                    SelectedTransaction = null;
                });

            this.WhenAnyValue(vm => vm.GroupedTransactions)
                .WhereNotNull()
                .SubscribeInMainThread(_ =>
                {
                    CanShowMoreTxs = Transactions.Count > TxsNumberPerPage;

                    TxListViewHeight = IsAllTxsShowed
                        ? Transactions.Count * DefaultTxRowHeight +
                          GroupedTransactions.Count * DefaultTxGroupHeight +
                          ListViewFooterHeight
                        : TxsNumberPerPage * DefaultTxRowHeight +
                          (GroupedTransactions.Count + 1) * DefaultTxGroupHeight;
                });

            this.WhenAnyValue(vm => vm.Addresses)
                .WhereNotNull()
                .SubscribeInMainThread(_ =>
                {
                    CanShowMoreAddresses = AddressesViewModel?.Addresses?.Count > AddressesNumberPerPage;

                    AddressListViewHeight = AddressesNumberPerPage * DefaultAddressRowHeight +
                                            ListViewFooterHeight;
                });

            this.WhenAnyValue(vm => vm.AddressesViewModel)
                .WhereNotNull()
                .SubscribeInMainThread(_ =>
                {
                    AddressesViewModel.AddressesChanged += OnAddresesChangedEventHandler;
                    OnAddresesChangedEventHandler();
                });

            _ = UpdateBalanceAsync();

            IsRefreshing = false;
            IsAllTxsShowed = false;

            SelectedTab = tokenBalance != null && tokenBalance.IsNft
                ? CurrencyTab.Details
                : CurrencyTab.Activity;

            InitTokenPreview();
        }

        protected void OnAddresesChangedEventHandler()
        {
            try
            {
                Device.InvokeOnMainThreadAsync(() =>
                {
                    Addresses = AddressesViewModel != null
                        ? new ObservableCollection<AddressViewModel>(
                            AddressesViewModel.Addresses
                                .OrderByDescending(a => a?.TokenBalance?.ParsedBalance)
                                .Take(AddressesNumberPerPage))
                        : new ObservableCollection<AddressViewModel>();
                });
            }
            catch (Exception e)
            {
                Log.Error(e, $"Error for token {CurrencyCode}");
            }
        }

        public void LoadAddresses()
        {
            try
            {
                AddressesViewModel?.Dispose();

                AddressesViewModel = new AddressesViewModel(
                    app: _app,
                    currency: TezosConfig,
                    navigationService: _navigationService,
                    tokenContract: Contract.Address,
                    tokenId: TokenBalance.TokenId);
            }
            catch (Exception e)
            {
                Log.Error(e, $"LoadAddresses error for contract {Contract?.Name} ({Contract?.Address})");
            }
        }

        public async Task LoadTransfers()
        {
            try
            {
                IsTransfersLoading = true;

                var tezosAccount = _app.Account
                    .GetCurrencyAccount<TezosAccount>(TezosConfig.Xtz);

                await Device.InvokeOnMainThreadAsync(async () =>
                {
                    Transactions = new ObservableCollection<TransactionViewModel>((await tezosAccount
                            .DataRepository
                            .GetTezosTokenTransfersAsync(
                                Contract.Address,
                                offset: 0,
                                limit: int.MaxValue))
                        .Where(token => token.Token.TokenId == TokenBalance.TokenId)
                        .Select(t => new TezosTokenTransferViewModel(t, TezosConfig))
                        .ToList()
                        .ForEachDo(t =>
                        {
                            t.CopyAddress = CopyAddress;
                            t.CopyTxId = CopyTxId;
                        }));

                    var groups = !IsAllTxsShowed
                        ? Transactions
                            .OrderByDescending(p => p.LocalTime.Date)
                            .Take(TxsNumberPerPage)
                            .GroupBy(p => p.LocalTime.Date)
                            .Select(g => new Grouping<TransactionViewModel>(g.Key,
                                new ObservableCollection<TransactionViewModel>(g.OrderByDescending(g => g.LocalTime))))
                        : Transactions
                            .GroupBy(p => p.LocalTime.Date)
                            .OrderByDescending(g => g.Key)
                            .Select(g => new Grouping<TransactionViewModel>(g.Key,
                                new ObservableCollection<TransactionViewModel>(g.OrderByDescending(g => g.LocalTime))));

                    GroupedTransactions = new ObservableCollection<Grouping<TransactionViewModel>>(groups);
                });
            }
            catch (OperationCanceledException)
            {
                Log.Debug($"LoadTransfers for {Contract} canceled");
            }
            catch (Exception e)
            {
                Log.Error(e, $"LoadTransfers error for contract {Contract}");
            }
            finally
            {
                IsTransfersLoading = false;
                Log.Debug($"Token transfers loaded for contract {Contract}");
            }
        }

        private async void OnBalanceUpdatedEventHandler(object sender, CurrencyEventArgs args)
        {
            try
            {
                if (!args.IsTokenUpdate ||
                    args.TokenContract != null && (args.TokenContract != TokenBalance.Contract ||
                                                   args.TokenId != TokenBalance.TokenId))
                {
                    return;
                }

                await Device.InvokeOnMainThreadAsync(async () => { await UpdateBalanceAsync(); });
            }
            catch (Exception e)
            {
                Log.Error(e, $"Balance updated event handler error for tezos token {CurrencyCode}");
            }
        }

        public void SubscribeToUpdates()
        {
            _app.Account.BalanceUpdated += OnBalanceUpdatedEventHandler;
            _app.QuotesProvider.QuotesUpdated += OnQuotesUpdatedEventHandler;
        }

        private void OnQuotesUpdatedEventHandler(object sender, EventArgs args)
        {
            if (sender is not IQuotesProvider quotesProvider)
                return;

            UpdateQuotesInBaseCurrency(quotesProvider);
        }

        public void UpdateQuotesInBaseCurrency(IQuotesProvider quotesProvider)
        {
            try
            {
                var tokenQuote = quotesProvider.GetQuote(TokenBalance.Symbol, BaseCurrencyCode);
                var xtzQuote = quotesProvider.GetQuote(TezosConfig.Xtz, BaseCurrencyCode);
                if (tokenQuote == null || xtzQuote == null) return;

                CurrentQuote = tokenQuote.Bid.SafeMultiply(xtzQuote.Bid);
                TotalAmountInBase = TotalAmount.SafeMultiply(CurrentQuote);
            }
            catch (Exception e)
            {
                Log.Error(e, $"Update quotes error on tezos token {CurrencyCode}");
            }
        }

        private ReactiveCommand<Unit, Unit> _tokenActionSheetCommand;

        public ReactiveCommand<Unit, Unit> TokenActionSheetCommand => _tokenActionSheetCommand ??=
            ReactiveCommand.Create(() =>
                _navigationService?.ShowBottomSheet(new TokenActionBottomSheet(this)));

        private ReactiveCommand<Unit, Unit> _receiveCommand;
        public ReactiveCommand<Unit, Unit> ReceiveCommand => _receiveCommand ??= ReactiveCommand.Create(OnReceiveClick);

        private ReactiveCommand<Unit, Unit> _sendCommand;
        public ReactiveCommand<Unit, Unit> SendCommand => _sendCommand ??= ReactiveCommand.Create(OnSendClick);

        protected ReactiveCommand<Unit, Unit> _convertCurrencyCommand;

        public ReactiveCommand<Unit, Unit> ConvertCurrencyCommand => _convertCurrencyCommand ??= ReactiveCommand.Create(
            () =>
            {
                if (IsConvertable)
                {
                    var currency = _app.Account.Currencies
                        .FirstOrDefault(c => c is Fa12Config fa12 && fa12.TokenContractAddress == Contract.Address);

                    if (currency == null)
                        return; // TODO: msg to user

                    _navigationService?.CloseBottomSheet();
                    _navigationService?.SetInitiatedPage(TabNavigation.Exchange);
                    _navigationService?.GoToExchange(currency);
                }
            });

        private ICommand _refreshCommand;
        public ICommand RefreshCommand => _refreshCommand ??= new Command(async () => await ScanCurrency());

        private ReactiveCommand<Unit, Unit> _cancelUpdateCommand;

        public ReactiveCommand<Unit, Unit> CancelUpdateCommand => _cancelUpdateCommand ??= ReactiveCommand.Create(() =>
        {
            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource?.Dispose();
        });

        private async Task UpdateBalanceAsync()
        {
            var tezosAccount = _app.Account.GetCurrencyAccount<TezosAccount>(TezosConfig.Xtz);

            var tokenWalletAddresses = await tezosAccount
                .DataRepository
                .GetTezosTokenAddressesByContractAsync(Contract.Address);

            var addresses = tokenWalletAddresses
                .Where(walletAddress => walletAddress.TokenBalance.TokenId == TokenBalance.TokenId)
                .ToList();

            var tokenBalance = 0m;
            addresses.ForEach(a => { tokenBalance += a.TokenBalance.GetTokenBalance(); });

            await Device.InvokeOnMainThreadAsync(() => { TotalAmount = tokenBalance; });
        }


        public async Task ScanCurrency()
        {
            _cancellationTokenSource = new CancellationTokenSource();
            IsRefreshing = true;

            try
            {
                var tezosAccount = _app.Account
                    .GetCurrencyAccount<TezosAccount>(TezosConfig.Xtz);

                var tezosTokensScanner = new TezosTokensScanner(tezosAccount);

                await tezosTokensScanner.UpdateBalanceAsync(
                    tokenContract: Contract.Address,
                    tokenId: (int)TokenBalance.TokenId,
                    cancellationToken: _cancellationTokenSource.Token);

                await Device.InvokeOnMainThreadAsync(() =>
                {
                    _navigationService?.DisplaySnackBar(MessageType.Regular,
                        CurrencyCode + " " + AppResources.HasBeenUpdated);
                });
            }
            catch (OperationCanceledException)
            {
                // nothing to do...
            }
            catch (Exception e)
            {
                Log.Error(e, $"Tezos tokens scanner error for {CurrencyCode}");
            }
            finally
            {
                IsRefreshing = false;
            }
        }

        protected void OnReceiveClick()
        {
            _navigationService?.CloseBottomSheet();
            var receiveViewModel = new ReceiveViewModel(
                app: _app,
                navigationService: _navigationService,
                currency: TezosConfig,
                tokenContract: Contract.Address,
                tokenType: Contract.GetContractType(),
                tokenId: (int)TokenBalance.TokenId);
            _navigationService?.ShowBottomSheet(new ReceiveBottomSheet(receiveViewModel));
        }

        protected void OnSendClick()
        {
            if (TotalAmount <= 0) return;
            _navigationService?.CloseBottomSheet();

            _navigationService?.SetInitiatedPage(TabNavigation.Portfolio);
            var sendViewModel = TokenBalance.IsNft
                ? new NftSendViewModel(
                    app: _app,
                    navigationService: _navigationService,
                    tokenContract: Contract.Address,
                    tokenId: (int)TokenBalance.TokenId,
                    tokenType: Contract.GetContractType(),
                    tokenPreview: TokenPreview)
                : new TezosTokensSendViewModel(
                    app: _app,
                    navigationService: _navigationService,
                    tokenContract: Contract.Address,
                    tokenId: (int)TokenBalance.TokenId,
                    tokenType: Contract.GetContractType(),
                    tokenPreview: TokenPreview);

            _navigationService?.ShowPage(new SelectAddressPage(sendViewModel.SelectFromViewModel),
                TabNavigation.Portfolio);
        }

        private ICommand _closeActionSheetCommand;

        public ICommand CloseActionSheetCommand => _closeActionSheetCommand ??= new Command(() =>
            _navigationService?.CloseBottomSheet());

        private ReactiveCommand<string, Unit> _changeCurrencyTabCommand;

        public ReactiveCommand<string, Unit> ChangeCurrencyTabCommand => _changeCurrencyTabCommand ??=
            ReactiveCommand.Create<string>((value) =>
            {
                Enum.TryParse(value, out CurrencyTab selectedTab);
                SelectedTab = selectedTab;
            });

        private ReactiveCommand<Unit, Unit> _openInBrowser;

        public ReactiveCommand<Unit, Unit> OpenInBrowser => _openInBrowser ??= ReactiveCommand.Create(() =>
        {
            var assetUrl = AssetUrl;

            if (assetUrl != null && Uri.TryCreate(assetUrl, UriKind.Absolute, out var uri))
                Launcher.OpenAsync(new Uri(uri.ToString()));
            else
                Log.Error("Invalid uri for ipfs asset");
        });

        protected void CopyAddress(string value)
        {
            Device.InvokeOnMainThreadAsync(() =>
            {
                if (value != null)
                {
                    _ = Clipboard.SetTextAsync(value);
                    _navigationService?.DisplaySnackBar(MessageType.Regular, AppResources.AddressCopied);
                }
                else
                {
                    _navigationService?.ShowAlert(AppResources.Error, AppResources.CopyError,
                        AppResources.AcceptButton);
                }
            });
        }

        protected void CopyTxId(string value)
        {
            Device.InvokeOnMainThreadAsync(() =>
            {
                if (value != null)
                {
                    _ = Clipboard.SetTextAsync(value);
                    _navigationService?.DisplaySnackBar(MessageType.Regular, AppResources.TransactionIdCopied);
                }
                else
                {
                    _navigationService?.ShowAlert(AppResources.Error, AppResources.CopyError,
                        AppResources.AcceptButton);
                }
            });
        }

        private ICommand _copyAddressCommand;

        public ICommand CopyAddressCommand =>
            _copyAddressCommand ??= new Command<string>((value) => CopyAddress(value));

        private ReactiveCommand<string, Unit> _showTokenInExplorerCommand;

        public ReactiveCommand<string, Unit> ShowTokenInExplorerCommand => _showTokenInExplorerCommand ??=
            ReactiveCommand.CreateFromTask<string>((value) =>
                Launcher.OpenAsync(
                    new Uri($"{TezosConfig.AddressExplorerUri}{Contract?.Address}/tokens/{TokenBalance?.TokenId}")));

        public bool IsIpfsAsset =>
            TokenBalance.ArtifactUri != null && ThumbsApi.HasIpfsPrefix(TokenBalance.ArtifactUri);

        public string AssetUrl => IsIpfsAsset
            ? $"http://ipfs.io/ipfs/{ThumbsApi.RemoveIpfsPrefix(TokenBalance.ArtifactUri)}"
            : null;

        private ReactiveCommand<Unit, Unit> _showAllTxsCommand;

        public ReactiveCommand<Unit, Unit> ShowAllTxsCommand => _showAllTxsCommand ??= ReactiveCommand.Create(() =>
        {
            IsAllTxsShowed = true;
            CanShowMoreTxs = false;
            TxsNumberPerPage = Transactions.Count;

            var groups = Transactions
                .GroupBy(p => p.LocalTime.Date)
                .OrderByDescending(g => g.Key)
                .Select(g => new Grouping<TransactionViewModel>(g.Key,
                    new ObservableCollection<TransactionViewModel>(g.OrderByDescending(g => g.LocalTime))));

            GroupedTransactions = new ObservableCollection<Grouping<TransactionViewModel>>(groups);
        });

        private ReactiveCommand<Unit, Unit> _showDescriptionCommand;

        public ReactiveCommand<Unit, Unit> ShowDescriptionCommand => _showDescriptionCommand ??=
            ReactiveCommand.Create(() =>
                _navigationService?.ShowBottomSheet(new NftDescriptionPopup(this)));

        private ICommand _closeBottomSheetCommand;

        public ICommand CloseBottomSheetCommand => _closeBottomSheetCommand ??= new Command(() =>
            _navigationService?.CloseBottomSheet());

        #region IDisposable Support

        private bool _disposedValue;

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    if (_account != null)
                        _account.BalanceUpdated -= OnBalanceUpdatedEventHandler;

                    if (_app.QuotesProvider != null)
                        _app.QuotesProvider.QuotesUpdated -= OnQuotesUpdatedEventHandler;

                    if (AddressesViewModel != null)
                        AddressesViewModel.AddressesChanged -= OnAddresesChangedEventHandler;
                }

                _account = null;
                AddressesViewModel = null;

                _disposedValue = true;
            }
        }

        ~TezosTokenViewModel()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion
    }
}