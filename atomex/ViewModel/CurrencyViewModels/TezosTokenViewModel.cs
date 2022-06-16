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
using atomex.ViewModel.SendViewModels;
using atomex.ViewModel.TransactionViewModels;
using atomex.Views;
using atomex.Views.TezosTokens;
using Atomex;
using Atomex.Blockchain.Tezos;
using Atomex.Common;
using Atomex.MarketData.Abstract;
using Atomex.TezosTokens;
using Atomex.Wallet;
using Atomex.Wallet.Abstract;
using Atomex.Wallet.Tezos;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Serilog;
using Xamarin.Essentials;
using Xamarin.Forms;
using static atomex.Models.SnackbarMessage;

namespace atomex.ViewModel.CurrencyViewModels
{
    public class TezosTokenViewModel : BaseViewModel
    {
        protected IAtomexApp _app { get; set; }
        protected IAccount _account { get; set; }
        protected INavigationService _navigationService { get; set; }

        public const string Fa12 = "FA12";
        public const string Fa2 = "FA2";
        public TezosConfig TezosConfig { get; set; }
        public TokenBalance TokenBalance { get; set; }
        public TokenContract Contract { get; set; }
        public bool IsFa12 => Contract.GetContractType() == Fa12;
        public bool IsFa2 => Contract.GetContractType() == Fa2;
        public static string BaseCurrencyCode => "USD";
        public string CurrencyCode => TokenBalance.Symbol;
        public string Address { get; set; }
        public bool IsConvertable => _app?.Account?.Currencies
            .Any(c => c is Fa12Config fa12 && fa12?.TokenContractAddress == Contract.Address) ?? false;
        [Reactive] public decimal TotalAmount { get; set; }
        [Reactive] public decimal TotalAmountInBase { get; set; }
        [Reactive] public decimal CurrentQuote { get; set; }

        [Reactive] public ObservableCollection<TransactionViewModel> Transactions { get; set; }
        [Reactive] public ObservableCollection<Grouping<DateTime, TransactionViewModel>> GroupedTransactions { get; set; }
        [Reactive] public TransactionViewModel SelectedTransaction { get; set; }

        public AddressesViewModel AddressesViewModel { get; set; }
        [Reactive] public ObservableCollection<AddressViewModel> Addresses { get; set; }

        [Reactive] public bool CanShowMoreTxs { get; set; }
        [Reactive] public bool IsAllTxsShowed { get; set; }
        [Reactive] public bool CanShowMoreAddresses { get; set; }
        public int TxsNumberPerPage = 3;
        public int AddressesNumberPerPage = 3;
        public const double DefaultGroupHeight = 36;

        private CancellationTokenSource _cancellationTokenSource;

        public class Grouping<K, T> : ObservableCollection<T>
        {
            public double GroupHeight { get; set; } = DefaultGroupHeight;
            public K Date { get; private set; }
            public Grouping(K date, IEnumerable<T> items)
            {
                Date = date;
                foreach (T item in items)
                    Items.Add(item);
            }
        }

        [Reactive] public bool IsRefreshing { get; set; }
        [Reactive] public CurrencyTab SelectedTab { get; set; }

        private ThumbsApi ThumbsApi => new ThumbsApi(
            new ThumbsApiSettings
            {
                ThumbsApiUri = TezosConfig.ThumbsApiUri,
                IpfsGatewayUri = TezosConfig.IpfsGatewayUri,
                CatavaApiUri = TezosConfig.CatavaApiUri
            });

        public UriImageSource TokenPreview
        {
            get
            {
                foreach (var url in ThumbsApi.GetTokenPreviewUrls(TokenBalance.Contract, TokenBalance.ThumbnailUri,
                             TokenBalance.DisplayUri ?? TokenBalance.ArtifactUri))
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
                            CacheValidity = new TimeSpan(5, 0, 0, 0)
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
                            CacheValidity = new TimeSpan(5, 0, 0, 0)
                        };
                    }
                }
                return null;
            }
        }

        public TezosTokenViewModel(
            IAtomexApp app,
            INavigationService navigationService,
            TokenContract contract,
            TokenBalance tokenBalance,
            string address)
        {
            _app = app ?? throw new ArgumentNullException(nameof(_app));
            _account = app.Account ?? throw new ArgumentNullException(nameof(_account));
            _navigationService = navigationService ?? throw new ArgumentNullException(nameof(_navigationService));
            Contract = contract;
            TokenBalance = tokenBalance;
            Address = address;

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
                    CanShowMoreTxs = Transactions.Count > TxsNumberPerPage);

            this.WhenAnyValue(vm => vm.Addresses)
                .WhereNotNull()
                .SubscribeInMainThread(_ =>
                    CanShowMoreAddresses = AddressesViewModel?.Addresses?.Count > AddressesNumberPerPage);

            this.WhenAnyValue(vm => vm.AddressesViewModel)
                .WhereNotNull()
                .SubscribeInMainThread(_ =>
                {
                    AddressesViewModel.AddressesChanged += OnAddresesChangedEventHandler;
                    OnAddresesChangedEventHandler();
                });

            LoadAddresses();
            _ = LoadTransfers();
            _ = UpdateAsync();

            IsRefreshing = false;
            IsAllTxsShowed = false;
            SelectedTab = CurrencyTab.Activity;

            SubscribeToUpdates();
        }

        protected virtual void OnAddresesChangedEventHandler()
        {
            try
            {
                Addresses = new ObservableCollection<AddressViewModel>(
                    AddressesViewModel?.Addresses
                        .OrderByDescending(a => a.Balance)
                        .Take(AddressesNumberPerPage));
            }
            catch (Exception e)
            {
                Log.Error(e, $"Error for token {CurrencyCode}");
            }
        }

        private void LoadAddresses()
        {
            AddressesViewModel?.Dispose();

            AddressesViewModel = new AddressesViewModel(
                app: _app,
                currency: TezosConfig,
                navigationService: _navigationService,
                tokenContract: Contract.Address,
                tokenId: TokenBalance.TokenId);
        }

        private async Task LoadTransfers()
        {
            try
            {
                if (IsFa12)
                {
                    var tokenAccount = _app.Account.GetTezosTokenAccount<Fa12Account>(
                        currency: Fa12,
                        tokenContract: Contract.Address,
                        tokenId: 0);

                    var selectedTransactionId = SelectedTransaction?.Id;

                    await Device.InvokeOnMainThreadAsync(async () =>
                    {
                        Transactions = new ObservableCollection<TransactionViewModel>((await tokenAccount
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
                               .Select(g => new Grouping<DateTime, TransactionViewModel>(g.Key, new ObservableCollection<TransactionViewModel>(g.OrderByDescending(g => g.LocalTime))))
                            : Transactions
                                .GroupBy(p => p.LocalTime.Date)
                                .OrderByDescending(g => g.Key)
                                .Select(g => new Grouping<DateTime, TransactionViewModel>(g.Key, new ObservableCollection<TransactionViewModel>(g.OrderByDescending(g => g.LocalTime))));

                        GroupedTransactions = new ObservableCollection<Grouping<DateTime, TransactionViewModel>>(groups);
                    });
                }
                else if (IsFa2)
                {
                    var tezosAccount = _app.Account
                        .GetCurrencyAccount<TezosAccount>(TezosConfig.Xtz);

                    var selectedTransactionId = SelectedTransaction?.Id;

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
                               .Select(g => new Grouping<DateTime, TransactionViewModel>(g.Key, new ObservableCollection<TransactionViewModel>(g.OrderByDescending(g => g.LocalTime))))
                            : Transactions
                                .GroupBy(p => p.LocalTime.Date)
                                .OrderByDescending(g => g.Key)
                                .Select(g => new Grouping<DateTime, TransactionViewModel>(g.Key, new ObservableCollection<TransactionViewModel>(g.OrderByDescending(g => g.LocalTime))));

                        GroupedTransactions = new ObservableCollection<Grouping<DateTime, TransactionViewModel>>(groups);
                    });
                }
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
                Log.Debug($"Token transfers loaded for contract {Contract}");
            }
        }

        private async void OnBalanceUpdatedEventHandler(object sender, CurrencyEventArgs args)
        {
            try
            {
                if (!Currencies.IsTezosToken(args?.Currency)) return;

                await Device.InvokeOnMainThreadAsync(async () =>
                {
                    await LoadTransfers();
                    await UpdateAsync();
                });
            }
            catch (Exception e)
            {
                Log.Error(e, $"Balance updated event handler error for tezos token {CurrencyCode}");
            }
        }

        private void SubscribeToUpdates()
        {
            _app.Account.BalanceUpdated += OnBalanceUpdatedEventHandler;
            _app.QuotesProvider.QuotesUpdated += OnQuotesUpdatedEventHandler;
        }

        private void OnQuotesUpdatedEventHandler(object sender, EventArgs args)
        {
            if (sender is not ICurrencyQuotesProvider quotesProvider)
                return;

            UpdateQuotesInBaseCurrency(quotesProvider);
        }

        private void UpdateQuotesInBaseCurrency(ICurrencyQuotesProvider quotesProvider)
        {
            var quote = quotesProvider.GetQuote(CurrencyCode, BaseCurrencyCode);
            if (quote == null) return;

            CurrentQuote = quote.Bid;
            TotalAmountInBase = TotalAmount.SafeMultiply(quote.Bid);
        }

        private ReactiveCommand<Unit, Unit> _tokenActionSheetCommand;
        public ReactiveCommand<Unit, Unit> TokenActionSheetCommand => _tokenActionSheetCommand ??= ReactiveCommand.Create(() =>
            _navigationService?.ShowBottomSheet(new TokenActionBottomSheet(this)));

        private ReactiveCommand<Unit, Unit> _receiveCommand;
        public ReactiveCommand<Unit, Unit> ReceiveCommand => _receiveCommand ??= ReactiveCommand.Create(OnReceiveClick);

        private ReactiveCommand<Unit, Unit> _sendCommand;
        public ReactiveCommand<Unit, Unit> SendCommand => _sendCommand ??= ReactiveCommand.Create(OnSendClick);

        protected ReactiveCommand<Unit, Unit> _convertCurrencyCommand;
        public ReactiveCommand<Unit, Unit> ConvertCurrencyCommand => _convertCurrencyCommand ??= ReactiveCommand.Create(() =>
        {
            if (IsConvertable)
            {
                var currency = _app.Account.Currencies
                    .FirstOrDefault(c => c is Fa12Config fa12 && fa12.TokenContractAddress == Contract.Address);

                if (currency == null)
                    return; // TODO: msg to user

                _navigationService?.CloseBottomSheet();
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

        private async Task UpdateAsync()
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

            await Device.InvokeOnMainThreadAsync(() =>
            {
                TotalAmount = tokenBalance;
            });
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

                await tezosTokensScanner.ScanContractAsync(
                    contractAddress: Contract.Address,
                    cancellationToken: _cancellationTokenSource.Token);

                var tokenAccount = _app.Account
                    .GetTezosTokenAccount<TezosTokenAccount>(
                        currency: IsFa12 ? Fa12 : Fa2,
                        tokenContract: Contract.Address,
                        tokenId: TokenBalance.TokenId);

                tokenAccount.ReloadBalances();

                await Device.InvokeOnMainThreadAsync(() =>
                {
                    _navigationService?.DisplaySnackBar(MessageType.Regular, CurrencyCode + " " + AppResources.HasBeenUpdated);
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
                tokenType: Contract.GetContractType());
            _navigationService?.ShowBottomSheet(new ReceiveBottomSheet(receiveViewModel));
        }

        protected void OnSendClick()
        {
            if (TotalAmount <= 0) return;
            _navigationService?.CloseBottomSheet();

            _navigationService?.SetInitiatedPage(TabNavigation.Portfolio);
            var sendViewModel = new TezosTokensSendViewModel(
                app: _app,
                navigationService: _navigationService,
                tokenContract: Contract.Address,
                tokenId: TokenBalance.TokenId,
                tokenType: Contract.GetContractType(),
                tokenPreview: TokenPreview);

            _navigationService?.ShowPage(new SelectAddressPage(sendViewModel.SelectFromViewModel), TabNavigation.Portfolio);
        }

        private ICommand _closeActionSheetCommand;
        public ICommand CloseActionSheetCommand => _closeActionSheetCommand ??= new Command(() =>
            _navigationService?.CloseBottomSheet());

        private ReactiveCommand<string, Unit> _changeCurrencyTabCommand;
        public ReactiveCommand<string, Unit> ChangeCurrencyTabCommand => _changeCurrencyTabCommand ??= ReactiveCommand.Create<string>((value) =>
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
                    _navigationService?.ShowAlert(AppResources.Error, AppResources.CopyError, AppResources.AcceptButton);
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
                    _navigationService?.ShowAlert(AppResources.Error, AppResources.CopyError, AppResources.AcceptButton);
                }
            });
        }

        public bool IsIpfsAsset =>
            TokenBalance.ArtifactUri != null && ThumbsApi.HasIpfsPrefix(TokenBalance.ArtifactUri);

        public string AssetUrl => IsIpfsAsset
            ? $"http://ipfs.io/ipfs/{ThumbsApi.RemoveIpfsPrefix(TokenBalance.ArtifactUri)}"
            : null;

        public void ShowAllTxs()
        {
            IsAllTxsShowed = true;
            CanShowMoreTxs = false;
            TxsNumberPerPage = Transactions.Count;

            var groups = Transactions
                .GroupBy(p => p.LocalTime.Date)
                .OrderByDescending(g => g.Key)
                .Select(g => new Grouping<DateTime, TransactionViewModel>(g.Key, new ObservableCollection<TransactionViewModel>(g.OrderByDescending(g => g.LocalTime))));

            GroupedTransactions = new ObservableCollection<Grouping<DateTime, TransactionViewModel>>(groups);
        }

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
