using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using atomex.Resources;
using atomex.Services;
using atomex.ViewModel.SendViewModels;
using atomex.ViewModel.TransactionViewModels;
using atomex.Views.TezosTokens;
using Atomex;
using Atomex.Blockchain.Tezos;
using Atomex.Common;
using Atomex.Services;
using Atomex.TezosTokens;
using Atomex.Wallet;
using Atomex.Wallet.Tezos;
using Serilog;
using Xamarin.Forms;

namespace atomex.ViewModel.CurrencyViewModels
{
    public class TezosTokensViewModel : BaseViewModel
    {
        private const int MaxAmountDecimals = 9;
        private const string Fa12 = "FA12";

        public INavigation Navigation { get; set; }

        public INavigationService NavigationService { get; set; }

        private IToastService ToastService { get; set; }

        public ObservableCollection<TezosTokenContractViewModel> TokensContracts { get; set; }
        public ObservableCollection<TezosTokenViewModel> Tokens { get; set; }
        public ObservableCollection<TezosTokenTransferViewModel> Transfers { get; set; }

        public CurrencyViewModel TezosViewModel { get; set; }

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

        public ObservableCollection<Grouping<DateTime, TezosTokenTransferViewModel>> GroupedTransfers { get; set; }

        private TezosTokenContractViewModel _tokenContract;
        public TezosTokenContractViewModel TokenContract
        {
            get => _tokenContract;
            set
            {
                _tokenContract = value;
                OnPropertyChanged(nameof(TokenContract));
                OnPropertyChanged(nameof(HasTokenContract));
                OnPropertyChanged(nameof(IsFa12));
                OnPropertyChanged(nameof(IsFa2));
                OnPropertyChanged(nameof(TokenContractAddress));
                OnPropertyChanged(nameof(TokenContractName));
                OnPropertyChanged(nameof(TokenContractIconUrl));
                OnPropertyChanged(nameof(IsConvertable));

                TokenContractChanged(TokenContract);
            }
        }

        public bool HasTokenContract => TokenContract != null;
        public bool IsFa12 => TokenContract?.IsFa12 ?? false;
        public bool IsFa2 => TokenContract?.IsFa2 ?? false;
        public string TokenContractAddress => TokenContract?.Contract?.Address ?? "";
        public string TokenContractName => TokenContract?.Name ?? "";
        public string TokenContractIconUrl => TokenContract?.IconUrl;
        public bool IsConvertable => _app.Account.Currencies
            .Any(c => c is Fa12Config fa12 && fa12.TokenContractAddress == TokenContractAddress);

        public decimal Balance { get; set; }
        public string BalanceFormat { get; set; }
        public string BalanceCurrencyCode { get; set; }

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

        private CancellationTokenSource _cancellation;

        private readonly IAtomexApp _app;

        public TezosTokensViewModel(IAtomexApp app)
        {
            _app = app ?? throw new ArgumentNullException(nameof(app));
            ToastService = DependencyService.Get<IToastService>();

            SubscribeToUpdates();

            _ = LoadAsync();

            //DesignerMode();
        }

        private void SubscribeToUpdates()
        {
            _app.AtomexClientChanged += OnAtomexClientChanged;
            _app.Account.BalanceUpdated += OnBalanceUpdatedEventHandler;
        }

        private void OnAtomexClientChanged(object sender, AtomexClientChangedEventArgs e)
        {
            Tokens?.Clear();
            Transfers?.Clear();
            TokensContracts?.Clear();
            TokenContract = null;
        }

        protected virtual async void OnBalanceUpdatedEventHandler(object sender, CurrencyEventArgs args)
        {
            try
            {
                if (Currencies.IsTezosToken(args.Currency))
                {
                    await Device.InvokeOnMainThreadAsync(async () =>
                    {
                        await ReloadTokenContractsAsync();

                    });
                }
            }
            catch (Exception e)
            {
                Log.Error(e, "Account balance updated event handler error");
            }
        }

        private async Task ReloadTokenContractsAsync()
        {
            var tokensContractsViewModels = (await _app.Account
                .GetCurrencyAccount<TezosAccount>(TezosConfig.Xtz)
                .DataRepository
                .GetTezosTokenContractsAsync())
                .Select(c => new TezosTokenContractViewModel { Contract = c });

            if (TokensContracts != null)
            {
                // add new token contracts if exists
                var newTokenContracts = tokensContractsViewModels.Except(
                    second: TokensContracts,
                    comparer: new Atomex.Common.EqualityComparer<TezosTokenContractViewModel>(
                        (x, y) => x.Contract.Address.Equals(y.Contract.Address),
                        x => x.Contract.Address.GetHashCode()));

                if (newTokenContracts.Any())
                {
                    foreach (var newTokenContract in newTokenContracts)
                        TokensContracts.Add(newTokenContract);
                }
                else
                {
                    // update current token contract
                    if (TokenContract != null)
                        TokenContractChanged(TokenContract);
                }
            }
            else
            {
                TokensContracts = new ObservableCollection<TezosTokenContractViewModel>(tokensContractsViewModels);
                OnPropertyChanged(nameof(TokensContracts));
            }
        }

        private async Task LoadAsync()
        {
            var tokensContractsViewModels = (await _app.Account
                .GetCurrencyAccount<TezosAccount>(TezosConfig.Xtz)
                .DataRepository
                .GetTezosTokenContractsAsync())
                .Select(c => new TezosTokenContractViewModel { Contract = c });

            TokensContracts = new ObservableCollection<TezosTokenContractViewModel>(tokensContractsViewModels);
            OnPropertyChanged(nameof(TokensContracts));
        }

        private async void TokenContractChanged(TezosTokenContractViewModel tokenContract)
        {
            if (tokenContract == null)
            {
                Tokens = new ObservableCollection<TezosTokenViewModel>();
                Transfers = new ObservableCollection<TezosTokenTransferViewModel>();
                GroupedTransfers = new ObservableCollection<Grouping<DateTime, TezosTokenTransferViewModel>>();

                OnPropertyChanged(nameof(Tokens));
                OnPropertyChanged(nameof(Transfers));
                OnPropertyChanged(nameof(GroupedTransfers));

                return;
            }

            var tezosConfig = _app.Account
                .Currencies
                .Get<TezosConfig>(TezosConfig.Xtz);

            if (tokenContract.IsFa12)
            {
                var tokenAccount = _app.Account.GetTezosTokenAccount<Fa12Account>(
                    currency: Fa12,
                    tokenContract: tokenContract.Contract.Address,
                    tokenId: 0);

                var tokenAddresses = await tokenAccount
                    .DataRepository
                    .GetTezosTokenAddressesByContractAsync(tokenContract.Contract.Address);

                var tokenAddress = tokenAddresses.FirstOrDefault();

                var tezosTokenConfig = _app.Account.Currencies
                    .FirstOrDefault(c => Currencies.IsTezosToken(c.Name) &&
                        c is Fa12Config fa12Config &&
                        fa12Config.TokenContractAddress == tokenContract.Contract.Address);

                Balance = tokenAccount
                    .GetBalance()
                    .Available;

                BalanceFormat = tokenAddress?.TokenBalance != null && tokenAddress.TokenBalance.Decimals != 0
                    ? $"F{Math.Min(tokenAddress.TokenBalance.Decimals, MaxAmountDecimals)}"
                    : $"F{MaxAmountDecimals}";

                BalanceCurrencyCode = tokenAddress?.TokenBalance != null && tokenAddress.TokenBalance.Symbol != null
                    ? tokenAddress.TokenBalance.Symbol
                    : tezosTokenConfig?.Name ?? "TOKENS";

                OnPropertyChanged(nameof(Balance));
                OnPropertyChanged(nameof(BalanceFormat));
                OnPropertyChanged(nameof(BalanceCurrencyCode));

                await Device.InvokeOnMainThreadAsync(async () =>
                {
                    Transfers = new ObservableCollection<TezosTokenTransferViewModel>((await tokenAccount
                        .DataRepository
                        .GetTezosTokenTransfersAsync(tokenContract.Contract.Address))
                        .Select(t => new TezosTokenTransferViewModel(t, tezosConfig))
                        .ToList()
                        .SortList((t1, t2) => t2.LocalTime.CompareTo(t1.LocalTime)));

                    var groups = Transfers.GroupBy(p => p.LocalTime.Date).Select(g => new Grouping<DateTime, TezosTokenTransferViewModel>(g.Key, g));
                    GroupedTransfers = new ObservableCollection<Grouping<DateTime, TezosTokenTransferViewModel>>(groups);
                    OnPropertyChanged(nameof(GroupedTransfers));
                });

                Tokens = new ObservableCollection<TezosTokenViewModel>();
            }
            else if (tokenContract.IsFa2)
            {
                var tezosAccount = _app.Account
                    .GetCurrencyAccount<TezosAccount>(TezosConfig.Xtz);

                var tokenAddresses = await tezosAccount
                    .DataRepository
                    .GetTezosTokenAddressesByContractAsync(tokenContract.Contract.Address);

                await Device.InvokeOnMainThreadAsync(async () =>
                {
                    Transfers = new ObservableCollection<TezosTokenTransferViewModel>((await tezosAccount
                        .DataRepository
                        .GetTezosTokenTransfersAsync(tokenContract.Contract.Address))
                        .Select(t => new TezosTokenTransferViewModel(t, tezosConfig))
                        .ToList()
                        .SortList((t1, t2) => t2.LocalTime.CompareTo(t1.LocalTime)));

                    var groups = Transfers.GroupBy(p => p.LocalTime.Date).Select(g => new Grouping<DateTime, TezosTokenTransferViewModel>(g.Key, g));
                    GroupedTransfers = new ObservableCollection<Grouping<DateTime, TezosTokenTransferViewModel>>(groups);
                    OnPropertyChanged(nameof(GroupedTransfers));
                });

                Tokens = new ObservableCollection<TezosTokenViewModel>(tokenAddresses
                    .Select(a => new TezosTokenViewModel
                        {
                            TokenBalance = a.TokenBalance,
                            Address = a.Address
                    }));
            }

            OnPropertyChanged(nameof(Tokens));
            OnPropertyChanged(nameof(Transfers));
        }

        private ICommand _selectTezosCurrencyCommand;
        public ICommand SelectTezosCurrencyCommand => _selectTezosCurrencyCommand ??= new Command(async (value) => await OnTezosTapped());

        private ICommand _selectTransferCommand;
        public ICommand SelectTransferCommand => _selectTransferCommand ??= new Command<TezosTokenTransferViewModel>(async (value) => await OnTransferTapped(value));

        private ICommand _selectTokenContractCommand;
        public ICommand SelectTokenContractCommand => _selectTokenContractCommand ??= new Command<TezosTokenContractViewModel>((value) => OnTokenContractTapped(value));

        private ICommand _sendPageCommand;
        public ICommand SendPageCommand => _sendPageCommand ??= new Command(async () => await OnSendButtonClicked());

        private ICommand _receivePageCommand;
        public ICommand ReceivePageCommand => _receivePageCommand ??= new Command(async () => await OnReceiveButtonClicked());

        private ICommand _convertPageCommand;
        public ICommand ConvertPageCommand => _convertPageCommand ??= new Command(async () => await OnConvertButtonClicked());

        protected ICommand _addressesPageCommand;
        public ICommand AddressesPageCommand => _addressesPageCommand ??= new Command(async () => await OnAddressesButtonClicked());

        private ICommand _updateTokensCommand;
        public ICommand UpdateTokensCommand => _updateTokensCommand ??= new Command(async () => await UpdateTokens());

        private async Task UpdateTokens()
        {
            _cancellation = new CancellationTokenSource();

            try
            {
                IsLoading = true;

                var tezosAccount = _app.Account
                    .GetCurrencyAccount<TezosAccount>(TezosConfig.Xtz);

                var tezosTokensScanner = new TezosTokensScanner(tezosAccount);

                await tezosTokensScanner.ScanAsync(
                    skipUsed: false,
                    cancellationToken: _cancellation.Token);

                // reload balances for all tezos tokens account
                foreach (var currency in _app.Account.Currencies)
                    if (Currencies.IsTezosToken(currency.Name))
                        _app.Account
                            .GetCurrencyAccount<TezosTokenAccount>(currency.Name)
                            .ReloadBalances();

                IsLoading = false;
                ToastService?.Show("Tokens" + " " + AppResources.HasBeenUpdated, ToastPosition.Top, Application.Current.RequestedTheme.ToString());
            }
            catch (OperationCanceledException)
            {
                Log.Debug("Wallet update operation canceled");
                IsLoading = false;
            }
            catch (Exception e)
            {
                Log.Error(e, "WalletViewModel.OnUpdateClick");
                IsLoading = false;
                // todo: message to user!?
            }
        }

        private async Task OnAddressesButtonClicked()
        {
            var tezosConfig = _app.Account
                .Currencies
                .Get<TezosConfig>(TezosConfig.Xtz);

            var addressesViewModel = new AddressesViewModel(
                app: _app,
                currency: tezosConfig,
                navigation: Navigation,
                tokenContract: TokenContract?.Contract?.Address);

            await Navigation.PushAsync(new AddressesPage(addressesViewModel));
        }

        private async Task OnTezosTapped()
        {
            await Navigation.PushAsync(new CurrencyPage(TezosViewModel));
        }

        private ICommand _selectTokenCommand;
        public ICommand SelectTokenCommand => _selectTokenCommand ??= new Command<TezosTokenViewModel>(async (value) => await OnTokenTapped(value));

        private async Task OnTokenTapped(TezosTokenViewModel token)
        {
            if (token == null)
                return;

            await Navigation.PushAsync(new TokenInfoPage(token));
        }

        private async Task OnTransferTapped(TezosTokenTransferViewModel transfer)
        {
            if (transfer == null)
                return;

            await Navigation.PushAsync(new TransactionInfoPage(transfer));
        }

        private async void OnTokenContractTapped(TezosTokenContractViewModel token)
        {
            if (token == null)
                return;

            TokenContract = token;

            await Navigation.PushAsync(new TokenPage(this));
        }

        private async Task OnSendButtonClicked()
        {
            var sendViewModel = new TezosTokensSendViewModel(
                app: _app,
                navigation: Navigation,
                from: null,
                tokenContract: TokenContract?.Contract?.Address,
                tokenId: 0);

            //await Navigation.PushAsync(new SendPage(new ReceiveViewModel(_app, tezosConfig, Navigation, TokenContract?.Contract?.Address)));
        }

        private async Task OnReceiveButtonClicked()
        {
            var tezosConfig = _app.Account
                .Currencies
                .GetByName(TezosConfig.Xtz);

            await Navigation.PushAsync(new ReceivePage(new ReceiveViewModel(_app, tezosConfig, Navigation, TokenContract?.Contract?.Address)));
        }

        private async Task OnConvertButtonClicked()
        {
            var currencyCode = _app.Account.Currencies
               .FirstOrDefault(c => c is Fa12Config fa12 && fa12.TokenContractAddress == TokenContractAddress)?.Name;

            if (currencyCode == null)
                return; // msg to user

            await NavigationService.ConvertCurrency(currencyCode);
        }

        protected void DesignerMode()
        {
            TokensContracts = new ObservableCollection<TezosTokenContractViewModel>
            {
                new TezosTokenContractViewModel
                {
                    Contract = new TokenContract
                    {
                        Address     = "KT1K9gCRgaLRFKTErYt1wVxA3Frb9FjasjTV",
                        Network     = "mainnet",
                        Name        = "kUSD",
                        Description = "FA1.2 Implementation of kUSD",
                        Interfaces  = new List<string> { "TZIP-007-2021-01-29" }
                    }
                },
                new TezosTokenContractViewModel
                {
                    Contract = new TokenContract
                    {
                        Address     = "KT1PWx2mnDueood7fEmfbBDKx1D9BAnnXitn",
                        //Address     = "KT1E953Kx9UfbyQeEQUgbcuzRQGyK2mpEmPw",
                        Network     = "mainnet",
                        Name        = "tzBTC",
                        Description = "Wrapped Bitcon",
                        Interfaces  = new List<string> { "TZIP-7", "TZIP-16", "TZIP-20" }
                    }
                },
                new TezosTokenContractViewModel
                {
                    Contract = new TokenContract
                    {
                        Address     = "KT1G1cCRNBgQ48mVDjopHjEmTN5Sbtar8nn9",
                        Network     = "mainnet",
                        Name        = "Hedgehoge",
                        Description = "such cute, much hedge!",
                        Interfaces  = new List<string> { "TZIP-007", "TZIP-016" }
                    }
                },
                new TezosTokenContractViewModel
                {
                    Contract = new TokenContract
                    {
                        Address     = "KT1RJ6PbjHpwc3M5rw5s2Nbmefwbuwbdxton",
                        Network     = "mainent",
                        Name        = "hic et nunc NFTs",
                        Description = "NFT token for digital assets.",
                        Interfaces  = new List<string> { "TZIP-12" }
                    }
                }
            };

            //TokenContract = TokensContracts.First();

            //var bcdApi = new BcdApi(new BcdApiSettings
            //{
            //    MaxSize = 10,
            //    Network = "mainnet",
            //    Uri = "https://api.better-call.dev/v1/"
            //});

            //var tokensBalances = bcdApi
            //    .GetTokenBalancesAsync(
            //        address: "tz1YS2CmS5o24bDz9XNr84DSczBXuq4oGHxr",
            //        count: 36)
            //    .WaitForResult();

            //Tokens = new ObservableCollection<TezosTokenViewModel>(
            //    tokensBalances.Value.Select(tb => new TezosTokenViewModel { TokenBalance = tb }));


            // test
            var bcdApi = new BcdApi(new BcdApiSettings
            {
                MaxSize = 10,
                Network = "mainnet",
                Uri = "https://api.better-call.dev/v1/"
            });

            var transfers = bcdApi
                .GetTokenTransfers(
                    address: "tz1YS2CmS5o24bDz9XNr84DSczBXuq4oGHxr",
                    contract: "KT1RJ6PbjHpwc3M5rw5s2Nbmefwbuwbdxton")
                .WaitForResult()
                .Value;

            var tezosConfig = _app.Account.Currencies.Get<TezosConfig>(TezosConfig.Xtz);

            Transfers = new ObservableCollection<TezosTokenTransferViewModel>(transfers
                .Select(t => new TezosTokenTransferViewModel(t, tezosConfig))
                .ToList()
                .SortList((t1, t2) => t2.LocalTime.CompareTo(t1.LocalTime)));

            var groups = Transfers.GroupBy(p => p.LocalTime.Date).Select(g => new Grouping<DateTime, TezosTokenTransferViewModel>(g.Key, g));
            GroupedTransfers = new ObservableCollection<Grouping<DateTime, TezosTokenTransferViewModel>>(groups);
            OnPropertyChanged(nameof(GroupedTransfers));


            Tokens = new ObservableCollection<TezosTokenViewModel>
            {
                new TezosTokenViewModel
                {
                    TokenBalance = new TokenBalance
                    {
                        Contract     = "KT1RJ6PbjHpwc3M5rw5s2Nbmefwbuwbdxton",
                        TokenId      = 155458,
                        Symbol       = "OBJKT",
                        Name         = "Enter VR Mode",
                        Description  = "VR Mode Collection 1/6",
                        ArtifactUri  = "ipfs://QmcxKgcESGphkb6S9k2Mh8jto6kapKtYN52mH1dBSFT6X5",
                        DisplayUri   = "ipfs://QmQRqbdfz8xGzobjcczGmz31cMHcs2okMw2oFgpjtvggoF",
                        ThumbnailUri = "ipfs://QmNrhZHUaEqxhyLfqoq1mtHSipkWHeT31LNHb1QEbDHgnc",
                        Balance      = "1"
                    }
                },
                new TezosTokenViewModel
                {
                    TokenBalance = new TokenBalance
                    {
                        Contract     = "KT1RJ6PbjHpwc3M5rw5s2Nbmefwbuwbdxton",
                        TokenId      = 155265,
                        Symbol       = "OBJKT",
                        Name         = "Rooted.",
                        Description  = "A high hillside with a rooted main character.",
                        //ArtifactUri  = "ipfs://QmapL8cQqVfVfmKNMbzH82kTZVW28qMoagoFCgDXRZmKgU",
                        DisplayUri   = "ipfs://QmeTWEdhg9gDCavV8tS25fyBfYtXftzjcAFMcBbxaYyyLH",
                        ThumbnailUri = "ipfs://QmNrhZHUaEqxhyLfqoq1mtHSipkWHeT31LNHb1QEbDHgnc",
                        Balance      = "1000"
                    }
                },
                new TezosTokenViewModel
                {
                    TokenBalance = new TokenBalance
                    {
                        Contract     = "KT1RJ6PbjHpwc3M5rw5s2Nbmefwbuwbdxton",
                        TokenId      = 154986,
                        Symbol       = "OBJKT",
                        Name         = "⭕️ MLNDR Founders Collection: 003/Greg",
                        Description  = "Meet 12 year old Greg, the family’s firstborn. During his short life, he has gone through so much, yet he stays positive and acts as a pillar to keep his parents and little sister happy and motivated.  Set in a dystopian future where natural resources are scarce and pollution is a global problem, the MLNDR Family Series will portray how I see our future as a species if our hunger for non-renewable natural resources continues to grow at the current pace.   The Founders collection will be comprised of 6 characters who will play as leading actors in my MLNDR Family series.   Holders of this token participate for a chance to win one of 10 NFTs from my props collection.",
                        ArtifactUri  = "ipfs://Qmd6rNTbeviB4tGruY27z47wAs27yRGHTDyp7b2qhxLHtU",
                        DisplayUri   = "ipfs://QmaJXJJBpfyMMXA5RvU1du4zGfvytt7VJLHvgCNeHzWEWA",
                        ThumbnailUri = "ipfs://QmNrhZHUaEqxhyLfqoq1mtHSipkWHeT31LNHb1QEbDHgnc",
                        Balance      = "10"
                    }
                }
            };
        }
    }
}
