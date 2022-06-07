using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;
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
using Xamarin.Forms;

namespace atomex.ViewModel.CurrencyViewModels
{
    public class TezosTokensViewModel : BaseViewModel
    {
        private const string FA2 = "FA2";
        private const string FA12 = "FA12";
        private IAtomexApp _app { get; }
        private INavigationService _navigationService { get; set; }
        [Reactive] private ObservableCollection<TokenContract> Contracts { get; set; }
        [Reactive] public ObservableCollection<TezosTokenViewModel> Tokens { get; set; }

        public Action TokensChanged;

        public TezosTokensViewModel(IAtomexApp app, INavigationService navigationService)
        {
            _app = app ?? throw new ArgumentNullException(nameof(_app));
            _navigationService = navigationService ?? throw new ArgumentNullException(nameof(_navigationService));

            _app.AtomexClientChanged += OnAtomexClientChanged;
            _app.Account.BalanceUpdated += OnBalanceUpdatedEventHandler;
            _app.QuotesProvider.QuotesUpdated += OnQuotesUpdatedEventHandler;

            this.WhenAnyValue(vm => vm.Contracts)
                .WhereNotNull()
                .SubscribeInMainThread(_ => OnQuotesUpdatedEventHandler(_app.QuotesProvider, EventArgs.Empty));

            _ = ReloadTokenContractsAsync();
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
                    .Select(walletAddress => new TezosTokenViewModel
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

        private ReactiveCommand<Unit, Unit> _setTokenCommand;
        private ReactiveCommand<Unit, Unit> SetTokenCommand => _setTokenCommand ??=
            ReactiveCommand.Create(() => { });

        private void OnAtomexClientChanged(object sender, AtomexClientChangedEventArgs e)
        {
            // Tokens?.Clear();
            // Transactions?.Clear();
            Contracts?.Clear();
            // TokenContract = null;
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
    }
}