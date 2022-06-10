using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using atomex.Common;
using Atomex;
using Atomex.Blockchain.Tezos;
using Atomex.Common;
using Atomex.MarketData.Abstract;
using Atomex.Services;
using Atomex.Wallet;
using Atomex.Wallet.Tezos;
using DynamicData;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Serilog;
using Xamarin.Essentials;
using Xamarin.Forms;

namespace atomex.ViewModel.CurrencyViewModels
{
    public class TezosToken : BaseViewModel
    {
        public TezosTokenViewModel TokenViewModel { get; set; }
        [Reactive] public bool IsSelected { get; set; }
        public Action<string, bool> OnChanged { get; set; }

        public TezosToken()
        {
            this.WhenAnyValue(vm => vm.IsSelected)
                .Skip(1)
                .SubscribeInMainThread(flag =>
                {
                    OnChanged?.Invoke(TokenViewModel.Symbol, flag);
                });
        }

        private ICommand _toggleCommand;
        public ICommand ToggleCommand => _toggleCommand ??= ReactiveCommand.Create(() => IsSelected = !IsSelected);
    }

    public class TezosTokensViewModel : BaseViewModel
    {
        private IAtomexApp _app { get; }
        private INavigationService _navigationService { get; set; }
        private ICurrencyQuotesProvider _quotesProvider { get; set; }
        [Reactive] private ObservableCollection<TokenContract> Contracts { get; set; }

        [Reactive] public IEnumerable<TezosToken> AllTokens { get; set; }
        [Reactive] public IEnumerable<TezosTokenViewModel> UserTokens { get; set; }

        public Action TokensChanged;

        public string BaseCurrencyCode => "USD";
        private string _walletName;

        public TezosTokensViewModel(
            IAtomexApp app,
            INavigationService navigationService)
        {
            _app = app ?? throw new ArgumentNullException(nameof(_app));
            _navigationService = navigationService ?? throw new ArgumentNullException(nameof(_navigationService));
            GetWalletName();

            SubscribeToServices(_app);
            
            this.WhenAnyValue(vm => vm.Contracts)
                .WhereNotNull()
                .SubscribeInMainThread(_ => OnQuotesUpdatedEventHandler(_app.QuotesProvider, EventArgs.Empty));

            _ = ReloadTokenContractsAsync();
        }

        private void SubscribeToServices(IAtomexApp app)
        {
            app.AtomexClientChanged += OnAtomexClientChanged;
            app.Account.BalanceUpdated += OnBalanceUpdatedEventHandler;

            _quotesProvider = app.QuotesProvider;
            _quotesProvider.QuotesUpdated += OnQuotesUpdatedEventHandler;
        }

        private async Task ReloadTokenContractsAsync()
        {
            var contracts = (await _app.Account
                    .GetCurrencyAccount<TezosAccount>(TezosConfig.Xtz)
                    .DataRepository
                    .GetTezosTokenContractsAsync())
                .ToList();

            Contracts = new ObservableCollection<TokenContract>(contracts);
        }

        private async Task<IEnumerable<TezosTokenViewModel>> LoadTokens()
        {
            var tezosConfig = _app.Account
                .Currencies
                .Get<TezosConfig>(TezosConfig.Xtz);

            var tokens = new ObservableCollection<TezosTokenViewModel>();

            foreach (var contract in Contracts)
            {
                var tezosAccount = _app.Account
                    .GetCurrencyAccount<TezosAccount>(TezosConfig.Xtz);

                var tokenWalletAddresses = await tezosAccount
                    .DataRepository
                    .GetTezosTokenAddressesByContractAsync(contract.Address);

                var tokenGroups = tokenWalletAddresses
                    .Where(walletAddress => walletAddress.Balance != 0)
                    .GroupBy(walletAddress => new
                    {
                        walletAddress.TokenBalance.TokenId,
                        walletAddress.TokenBalance.Contract
                    });

                var tokensViewModels = tokenGroups
                    .Select(walletAddressGroup =>
                        walletAddressGroup.Skip(1).Aggregate(walletAddressGroup.First(), (result, walletAddress) =>
                        {
                            result.Balance += walletAddress.Balance;
                            result.TokenBalance.ParsedBalance = result.TokenBalance.GetTokenBalance() +
                                                                walletAddress.TokenBalance.GetTokenBalance();

                            return result;
                        }))
                    .Select(walletAddress => new TezosTokenViewModel(_app, _navigationService)
                    {
                        TezosConfig = tezosConfig,
                        TokenBalance = walletAddress.TokenBalance,
                        Address = walletAddress.Address,
                        Contract = contract,
                    });
                tokens.AddRange(tokensViewModels);
            }

            return tokens
                .Where(token => !token.TokenBalance.IsNft);
        }

        private void ChangeUserTokens(
            string token,
            bool isSelected)
        {
            try
            {
                if (string.IsNullOrEmpty(token)) return;

                var tokens = Preferences.Get(_walletName + "-" + "TezosTokens", string.Empty);
                List<string> tokensList = new List<string>();
                tokensList = tokens.Split(',').ToList();

                if (isSelected && tokensList.Any(item => item == token))
                    return;

                if (isSelected)
                    tokensList.Add(token);
                else
                    tokensList.RemoveAll(c => c.Contains(token));

                var result = string.Join(",", tokensList);
                Preferences.Set(_walletName + "-" + "TezosTokens", result);

                AllTokens?.Select(t => t.IsSelected == tokensList.Contains(t.TokenViewModel.Symbol));

                UserTokens = AllTokens?
                    .Where(c => c.IsSelected)
                    .Select(vm => vm.TokenViewModel)
                    .ToList();

                TokensChanged?.Invoke();
            }
            catch (Exception e)
            {
                Log.Error(e, "Change user tokens error");
            }
        }

        private ReactiveCommand<Unit, Unit> _hideLowBalancesCommand;
        public ReactiveCommand<Unit, Unit> HideLowBalancesCommand => _hideLowBalancesCommand ??= ReactiveCommand.Create(() =>
        {
            _navigationService?.CloseBottomSheet();
            // TODO: filters tezos tokens
        });

        private void OnAtomexClientChanged(object sender, AtomexClientChangedEventArgs e)
        {
            Contracts?.Clear();
        }

        private async void OnBalanceUpdatedEventHandler(object sender, CurrencyEventArgs args)
        {
            try
            {
                if (!Currencies.IsTezosToken(args.Currency)) return;

                await Device.InvokeOnMainThreadAsync(async () =>
                {
                    await ReloadTokenContractsAsync();
                });
            }
            catch (Exception e)
            {
                Log.Error(e, "Account balance updated event handler error");
            }
        }

        private async void OnQuotesUpdatedEventHandler(object sender, EventArgs args)
        {
            if (sender is not ICurrencyQuotesProvider quotesProvider)
                return;

            var userTokens = Preferences.Get(_walletName + "-" + "TezosTokens", string.Empty);
            var tokens = await LoadTokens();

            List<string> result = new List<string>();
            result = userTokens != string.Empty
                ? userTokens.Split(',').ToList()
                : tokens.Select(t => t.Symbol).ToList();

            AllTokens = new ObservableCollection<TezosToken>(tokens
                .Select(token =>
                {
                    var quote = quotesProvider.GetQuote(token.TokenBalance.Symbol, BaseCurrencyCode);

                    var vm = new TezosToken
                    {
                        TokenViewModel = token,
                        IsSelected = result.Contains(token.Symbol),
                        OnChanged = ChangeUserTokens
                    };

                    if (quote == null) return vm;

                    token.CurrentQuote = quote.Bid;
                    token.BalanceInBase = token.TokenBalance.GetTokenBalance().SafeMultiply(quote.Bid);

                    return vm;
                })
                .OrderByDescending(token => token.TokenViewModel.IsConvertable)
                .ThenByDescending(token => token.TokenViewModel.BalanceInBase));

            UserTokens = new ObservableCollection<TezosTokenViewModel>(AllTokens
                .Where(c => c.IsSelected)
                .Select(vm => vm.TokenViewModel)
                .ToList());

            TokensChanged?.Invoke();
        }

        private ICommand _closeActionSheetCommand;
        public ICommand CloseActionSheetCommand => _closeActionSheetCommand ??=
            new Command(() => _navigationService?.CloseBottomSheet());

        private string GetWalletName()
        {
            return new DirectoryInfo(_app?.Account?.Wallet?.PathToWallet).Parent.Name;
        }
    }
}