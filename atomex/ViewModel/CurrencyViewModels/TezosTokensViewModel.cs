using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;
using atomex.Common;
using Atomex;
using Atomex.Blockchain.Tezos;
using Atomex.Common;
using Atomex.Services;
using Atomex.Wallet;
using Atomex.Wallet.Tezos;
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
        public ObservableCollection<TezosTokenViewModel> Tokens { get; set; }

        public TezosTokensViewModel(IAtomexApp app, INavigationService navigationService)
        {
            _app = app ?? throw new ArgumentNullException(nameof(_app));
            _navigationService = navigationService ?? throw new ArgumentNullException(nameof(_navigationService));

            _app.AtomexClientChanged += OnAtomexClientChanged;
            _app.Account.BalanceUpdated += OnBalanceUpdatedEventHandler;

            this.WhenAnyValue(vm => vm.Contracts)
                .WhereNotNull()
                .Select(_ => Unit.Default)
                .InvokeCommandInMainThread(LoadTokensCommand);

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

        private async void LoadTokens()
        {
            var tezosConfig = _app.Account
                .Currencies
                .Get<TezosConfig>(TezosConfig.Xtz);

            var tokens = new ObservableCollection<TezosTokenViewModel>();

            foreach (var contract in Contracts!)
            {
                var tezosAccount = _app.Account
                    .GetCurrencyAccount<TezosAccount>(TezosConfig.Xtz);

                var tokenWalletAddresses = await tezosAccount
                    .DataRepository
                    .GetTezosTokenAddressesByContractAsync(contract.Address);


                var tokenGroups = tokenWalletAddresses
                    .Where(walletAddress => walletAddress.Balance != 0)
                    .GroupBy(walletAddress => walletAddress.TokenBalance.TokenId);


                var tokensViewModels = new ObservableCollection<TezosTokenViewModel>();
                // .Select(walletAddress => new TezosTokenViewModel
                // {
                //     TezosConfig = tezosConfig,
                //     TokenBalance = walletAddress.TokenBalance,
                //     Address = walletAddress.Address
                // }));

                //tokens.AddRange(tokensViewModels);
            }

            Log.Fatal("Tokens collection updated");
            Tokens = new ObservableCollection<TezosTokenViewModel>(tokens);
            //
            // var tokens = Contracts?.AggregateAsync(
            //     new ObservableCollection<TezosTokenViewModel>(),
            //     async Task<IEnumerable<TezosTokenViewModel>>(tokens, contract) =>
            //     {
            //
            //
            //         return tokens;
            //     });
        }

        private ReactiveCommand<Unit, Unit> _loadTokensCommand;

        private ReactiveCommand<Unit, Unit> LoadTokensCommand => _loadTokensCommand ??=
            ReactiveCommand.Create(LoadTokens);


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

        protected async void OnBalanceUpdatedEventHandler(object sender, CurrencyEventArgs args)
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
    }
}