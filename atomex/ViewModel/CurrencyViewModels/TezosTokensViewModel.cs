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
        public bool IsConvertable => _app?.Account?.Currencies
            .Any(c => c is Fa12Config fa12 && fa12?.TokenContractAddress == TokenContractAddress) ?? false;

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

        public TezosTokensViewModel(IAtomexApp app, bool restore = false)
        {
            _app = app ?? throw new ArgumentNullException(nameof(app));
            ToastService = DependencyService.Get<IToastService>();

            SubscribeToUpdates();

            _ = LoadAsync();

            if (restore)
                _ = UpdateTokens();
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

        private ICommand _sendFa2PageCommand;
        public ICommand SendFa2PageCommand => _sendFa2PageCommand ??= new Command(async (value) => await OnSendButtonClicked(value));

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
                tokenContract: TokenContract,
                tokenId: 0);

            await Navigation.PushAsync(new SendTokenPage(sendViewModel));
        }

        private async Task OnSendButtonClicked(object obj)
        {
            var token = obj as TezosTokenViewModel;

            if (token == null)
                return;

            var sendViewModel = new TezosTokensSendViewModel(
                app: _app,
                navigation: Navigation,
                from: token.Address,
                tokenContract: TokenContract,
                tokenId: token.TokenBalance.TokenId);

            await Navigation.PushAsync(new SendTokenPage(sendViewModel));
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
    }
}
