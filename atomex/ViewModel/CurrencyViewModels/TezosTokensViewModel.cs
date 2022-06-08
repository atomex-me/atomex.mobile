using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using atomex.Common;
using atomex.Resources;
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
using Xamarin.Forms;
using static atomex.Models.SnackbarMessage;

namespace atomex.ViewModel.CurrencyViewModels
{
    public class TezosTokensViewModel : BaseViewModel
    {
        private const string FA2 = "FA2";
        private const string FA12 = "FA12";
        private IAtomexApp _app { get; }
        private INavigationService _navigationService { get; set; }
        private ICurrencyQuotesProvider _quotesProvider { get; set; }
        [Reactive] private ObservableCollection<TokenContract> Contracts { get; set; }
        [Reactive] public ObservableCollection<TezosTokenViewModel> Tokens { get; set; }
        [Reactive] public bool IsUpdating { get; set; }

        public Action TokensChanged;

        public TezosTokensViewModel(IAtomexApp app, INavigationService navigationService)
        {
            _app = app ?? throw new ArgumentNullException(nameof(_app));
            _navigationService = navigationService ?? throw new ArgumentNullException(nameof(_navigationService));
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

        private ReactiveCommand<Unit, Unit> _updateTokensCommand;
        public ReactiveCommand<Unit, Unit> UpdateTokensCommand => _updateTokensCommand ??=
            ReactiveCommand.CreateFromTask(async () =>
            {
                IsUpdating = true;
                await ReloadTokenContractsAsync();
                await Task.Delay(1500);
                OnQuotesUpdatedEventHandler(_quotesProvider, EventArgs.Empty);
                IsUpdating = false;
                _navigationService?.DisplaySnackBar(MessageType.Regular, AppResources.TezosTokens + " " + AppResources.HasBeenUpdated);
            });

        private ReactiveCommand<Unit, Unit> _manageTokensCommand;
        public ReactiveCommand<Unit, Unit> ManageTokensCommand => _manageTokensCommand ??= ReactiveCommand.Create(() =>
        {
            // TODO: Manage Tezos tokens bottom sheet
        });

        private ReactiveCommand<Unit, Unit> _hideLowBalancesCommand;
        public ReactiveCommand<Unit, Unit> HideLowBalancesCommand => _hideLowBalancesCommand ??= ReactiveCommand.Create(() =>
        {
            _navigationService?.CloseBottomSheet();
            // TODO: filters tezos tokens
        });

        public async Task UpdateTokens()
        {
            IsUpdating = true;
            await ReloadTokenContractsAsync();
            OnQuotesUpdatedEventHandler(_quotesProvider, EventArgs.Empty);
            IsUpdating = false;
        }

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

            var tokens = await LoadTokens();
            Tokens = new ObservableCollection<TezosTokenViewModel>(tokens
                .Select(token =>
                {
                    var quote = quotesProvider.GetQuote(token.TokenBalance.Symbol.ToLower());
                    if (quote == null) return token;

                    token.CurrentQuote = quote.Bid;
                    token.BalanceInBase = token.TokenBalance.GetTokenBalance().SafeMultiply(quote.Bid);
                    return token;
                })
                .OrderByDescending(token => token.Contract.Name?.ToLower() == "tzbtc")
                .ThenByDescending(token => token.Contract.Name?.ToLower() == "kusd")
                .ThenByDescending(token => token.BalanceInBase));

            TokensChanged?.Invoke();
        }

        private ICommand _closeActionSheetCommand;
        public ICommand CloseActionSheetCommand => _closeActionSheetCommand ??=
            new Command(() => _navigationService?.CloseBottomSheet());
    }
}