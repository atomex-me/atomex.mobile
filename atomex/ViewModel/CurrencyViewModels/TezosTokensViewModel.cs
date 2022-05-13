using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using atomex.Resources;
using atomex.ViewModel.SendViewModels;
using atomex.ViewModel.TransactionViewModels;
using atomex.Views;
using Atomex;
using Atomex.Common;
using Atomex.Core;
using Atomex.Services;
using Atomex.TezosTokens;
using Atomex.Wallet;
using Atomex.Wallet.Tezos;
using Serilog;
using Xamarin.Forms;
using static atomex.Models.SnackbarMessage;

namespace atomex.ViewModel.CurrencyViewModels
{
    public class TezosTokensViewModel : CurrencyViewModel
    {
        private const int MaxAmountDecimals = 9;
        private const string Fa12 = "FA12";

        public ObservableCollection<TezosTokenContractViewModel> TokensContracts { get; set; }
        public ObservableCollection<TezosTokenViewModel> Tokens { get; set; }
        public ObservableCollection<TezosTokenTransferViewModel> Transfers { get; set; }

        public CurrencyViewModel TezosViewModel { get; set; }

        public ObservableCollection<Grouping<DateTime, TezosTokenTransferViewModel>> GroupedTransfers { get; set; }

        private TezosTokenContractViewModel _tokenContract;
        public TezosTokenContractViewModel TokenContract
        {
            get => _tokenContract;
            set
            {
                if (value == null) return;

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
                //Navigation.PushAsync(new TokenPage(this));
                TokenContract = null;
            }
        }

        private TezosTokenTransferViewModel _tokenTransfer;
        public TezosTokenTransferViewModel TokenTransfer
        {
            get => _tokenTransfer;
            set
            {
                if (value == null) return;

                _tokenTransfer = value;
                OnPropertyChanged(nameof(TokenTransfer));

                //Navigation.PushAsync(new TransactionInfoPage(TokenTransfer));
                TokenTransfer = null;
            }
        }

        private TezosTokenViewModel _token;
        public TezosTokenViewModel Token
        {
            get => _token;
            set
            {
                if (value == null) return;

                _token = value;
                OnPropertyChanged(nameof(Token));

                //Navigation.PushAsync(new TokenInfoPage(Token));
                Token = null;
            }
        }

        public bool HasTokenContract => TokenContract != null;
        public bool IsFa12 => TokenContract?.IsFa12 ?? false;
        public bool IsFa2 => TokenContract?.IsFa2 ?? false;
        public string TokenContractAddress => TokenContract?.Contract?.Address ?? "";
        public string TokenContractName => TokenContract?.Name ?? "";
        public string TokenContractIconUrl => TokenContract?.IconUrl;
        public bool IsConvertable => _app?.Account?.Currencies
            .Any(c => c is Fa12Config fa12 && fa12?.TokenContractAddress == TokenContractAddress) ?? false;

        public decimal Balance { get; set; }
        public string BalanceFormat { get; set; }
        public string BalanceCurrencyCode { get; set; }

        private CancellationTokenSource _cancellation;


        public TezosTokensViewModel(
           IAtomexApp app,
           CurrencyConfig currency,
           INavigationService navigationService,
           bool loadTransaction = true)
           : base(app, currency, navigationService, loadTransaction)
        {
            _ = ReloadTokenContractsAsync();
        }

        protected override void SubscribeToServices()
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

        public override async Task ScanCurrency()
        {
            await UpdateTokens();
        }

        protected override async void OnBalanceUpdatedEventHandler(object sender, CurrencyEventArgs args)
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
                    comparer: new EqualityComparer<TezosTokenContractViewModel>(
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
                        .GetTezosTokenTransfersAsync(tokenContract.Contract.Address, offset: 0, limit: int.MaxValue))
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
                        .GetTezosTokenTransfersAsync(tokenContract.Contract.Address, offset: 0, limit: int.MaxValue))
                        .Select(t => new TezosTokenTransferViewModel(t, tezosConfig))
                        .ToList()
                        .SortList((t1, t2) => t2.LocalTime.CompareTo(t1.LocalTime)));

                    var groups = Transfers.GroupBy(p => p.LocalTime.Date).Select(g => new Grouping<DateTime, TezosTokenTransferViewModel>(g.Key, g));
                    GroupedTransfers = new ObservableCollection<Grouping<DateTime, TezosTokenTransferViewModel>>(groups);
                    OnPropertyChanged(nameof(GroupedTransfers));
                });

                Tokens = new ObservableCollection<TezosTokenViewModel>(tokenAddresses
                    .Where(a => a.Balance != 0)
                    .Select(a => new TezosTokenViewModel
                    {
                        TezosConfig = tezosConfig,
                        TokenBalance = a.TokenBalance,
                        Address = a.Address
                    })
                    .OrderBy(a => a.TokenBalance.TokenId));
            }

            OnPropertyChanged(nameof(Tokens));
            OnPropertyChanged(nameof(Transfers));
        }

        //private ICommand _selectTezosCurrencyCommand;
        //public ICommand SelectTezosCurrencyCommand => _selectTezosCurrencyCommand ??= new Command((value) => Navigation.PushAsync(new CurrencyPage(TezosViewModel)));

        //private ICommand _sendPageCommand;
        //public ICommand SendPageCommand => _sendPageCommand ??= new Command(async () => await OnSendButtonClicked());

        //private ICommand _sendFa2PageCommand;
        //public ICommand SendFa2PageCommand => _sendFa2PageCommand ??= new Command(async (value) => await OnSendButtonClicked(value));

        //private ICommand _receivePageCommand;
        //public ICommand ReceivePageCommand => _receivePageCommand ??= ReactiveCommand.Create(OnReceiveClick);

        //private ICommand _convertPageCommand;
        //public ICommand ConvertPageCommand => _convertPageCommand ??= new Command(OnConvertButtonClicked);

        //protected ICommand _addressesPageCommand;
        //public ICommand AddressesPageCommand => _addressesPageCommand ??= new Command(async () => await OnAddressesButtonClicked());

        //private ICommand _updateTokensCommand;
        //public ICommand UpdateTokensCommand => _updateTokensCommand ??= new Command(async () => await UpdateTokens());

        private async Task UpdateTokens()
        {
            if (IsRefreshing) return;

            _cancellation = new CancellationTokenSource();
            IsRefreshing = true;

            try
            {
                var scanner = new HdWalletScanner(_app.Account);
                await scanner.ScanAsync(
                    currency: TezosConfig.Xtz,
                    skipUsed: true,
                    cancellationToken: _cancellation.Token);
                await TezosViewModel?.LoadTransactionsAsync();

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

                _navigationService?.DisplaySnackBar(MessageType.Success, AppResources.Tokens + " " + AppResources.HasBeenUpdated);
            }
            catch (OperationCanceledException)
            {
                Log.Debug("Wallet update operation canceled");
            }
            catch (Exception e)
            {
                Log.Error(e, "WalletViewModel.OnUpdateClick");
                // todo: message to user!?
            }

            IsRefreshing = false;
        }

        protected override void LoadAddresses()
        {
            var tezosConfig = _app.Account
                .Currencies
                .Get<TezosConfig>(TezosConfig.Xtz);

            var addressesViewModel = new AddressesViewModel(
                app: _app,
                navigationService: _navigationService,
                currency: tezosConfig,
                tokenContract: TokenContract?.Contract?.Address);
        }

        //private async Task OnSendButtonClicked()
        //{
        //    if (Balance <= 0) return;

        //    string tokenPreviewUrl = HasTokenContract ? TokenContractIconUrl : string.Empty;

        //    var tokenPreview = new UriImageSource
        //    {
        //        Uri = new Uri(tokenPreviewUrl),
        //        CachingEnabled = true,
        //        CacheValidity = new TimeSpan(5, 0, 0, 0)
        //    };

            //var sendViewModel = new TezosTokensSendViewModel(
            //    app: _app,
            //    navigationService: Navigation,
            //    tokenType: TokenContract.Contract.GetContractType(),
            //    tokenPreview: tokenPreview,
            //    from: null,
            //    tokenContract: TokenContract?.Contract?.Address,
            //    tokenId: 0);

            //await Navigation.PushAsync(new SelectAddressPage(sendViewModel.SelectFromViewModel));
        //}

        //private async Task OnSendButtonClicked(object obj)
        //{
        //    var token = obj as TezosTokenViewModel;

        //    if (token == null)
        //        return;

            //var sendViewModel = new TezosTokensSendViewModel(
            //    app: _app,
            //    navigation: Navigation,
            //    tokenType: TokenContract.Contract.GetContractType(),
            //    tokenPreview: token.TokenPreview,
            //    from: token.Address,
            //    tokenContract: TokenContract?.Contract?.Address,
            //    tokenId: token.TokenBalance.TokenId);

            //await Navigation.PushAsync(new SelectAddressPage(sendViewModel.SelectToViewModel));
        //}

        protected override void OnReceiveClick()
        {

            var tezosConfig = _app.Account
                .Currencies
                .GetByName(TezosConfig.Xtz);

            var receiveViewModel = new ReceiveViewModel(
                app: _app,
                currency: tezosConfig,
                navigationService: _navigationService,
                tokenContract: TokenContract?.Contract?.Address,
                tokenType: TokenContract?.Contract?.GetContractType());
            _navigationService?.ShowBottomSheet(new ReceiveBottomSheet(receiveViewModel));
        }

        protected override void OnSendClick()
        {
            if (TokenContract?.Contract?.Address == null)
                return;

            string tokenPreviewUrl = HasTokenContract ? TokenContractIconUrl : string.Empty;

            var tokenPreview = new UriImageSource
            {
                Uri = new Uri(tokenPreviewUrl),
                CachingEnabled = true,
                CacheValidity = new TimeSpan(5, 0, 0, 0)
            };

            var sendViewModel = new TezosTokensSendViewModel(
                app: _app,
                navigationService: _navigationService,
                tokenContract: TokenContract.Contract.Address,
                tokenId: 0,
                tokenType: TokenContract.Contract.GetContractType(),
                tokenPreview: tokenPreview,
                from: null);

            _navigationService?.ShowPage(new SelectAddressPage(sendViewModel.SelectFromViewModel), TabNavigation.Portfolio);
        }

        //private void OnConvertButtonClicked()
        //{
        //    var currency = _app.Account.Currencies
        //       .FirstOrDefault(c => c is Fa12Config fa12 && fa12.TokenContractAddress == TokenContractAddress);

        //    if (currency == null)
        //        return; // msg to user

        //    //NavigationService.ConvertCurrency(currency);
        //}
    }
}
