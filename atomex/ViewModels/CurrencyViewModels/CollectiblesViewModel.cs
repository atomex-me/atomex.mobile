using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Threading.Tasks;
using Atomex;
using Atomex.Blockchain.Tezos;
using Atomex.Client.Common;
using atomex.Common;
using atomex.Views.Collectibles;
using Atomex.Wallet;
using Atomex.Wallet.Tezos;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Serilog;
using Xamarin.Forms;

namespace atomex.ViewModels.CurrencyViewModels
{
    public class CollectiblesViewModel : BaseViewModel
    {
        private readonly IAtomexApp _app;
        private readonly INavigationService _navigationService;
        [Reactive] private ObservableCollection<TokenContract> Contracts { get; set; }
        [Reactive] public ObservableCollection<CollectibleViewModel> Collectibles { get; set; }

        [Reactive] public CollectibleViewModel SelectedCollectible { get; set; }
        [Reactive] public bool IsCollectiblesLoading { get; set; }

        public CollectiblesViewModel(
            IAtomexApp app,
            INavigationService navigationService)
        {
            _app = app ?? throw new ArgumentNullException(nameof(app));
            _navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));

            _app.AtomexClientChanged += OnAtomexClientChanged;
            _app.Account.BalanceUpdated += OnBalanceUpdatedEventHandler;

            this.WhenAnyValue(vm => vm.Contracts)
                .WhereNotNull()
                .SubscribeInMainThread(collectibles => _ = LoadCollectibles());

            this.WhenAnyValue(vm => vm.SelectedCollectible)
                .WhereNotNull()
                .SubscribeInMainThread(async (collectible) =>
                {
                    _navigationService?.ShowPage(new CollectiblePage(collectible), TabNavigation.Portfolio);
                    SelectedCollectible = null;
                });

            _ = ReloadTokenContractsAsync();
        }

        private void OnAtomexClientChanged(object sender, AtomexClientChangedEventArgs args)
        {
            if (_app.Account != null) return;

            _app.AtomexClientChanged -= OnAtomexClientChanged;
            Contracts?.Clear();
        }

        private async void OnBalanceUpdatedEventHandler(object sender, CurrencyEventArgs args)
        {
            try
            {
                await Device.InvokeOnMainThreadAsync(async () => { await ReloadTokenContractsAsync(); });
            }
            catch (Exception e)
            {
                Log.Error(e, "Tezos collectibles balance updated event handler error");
            }
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

        private async Task LoadCollectibles()
        {
            try
            {
                IsCollectiblesLoading = true;

                var nftTokens = new List<TezosTokenViewModel>();

                foreach (var contract in Contracts)
                    nftTokens.AddRange(await TezosTokenViewModelCreator.CreateOrGet(
                        atomexApp: _app,
                        navigationService: _navigationService,
                        contract: contract,
                        isNft: true));

                Collectibles = new ObservableCollection<CollectibleViewModel>(nftTokens
                    .GroupBy(nft => nft.Contract.Address)
                    .Select(nftGroup => new CollectibleViewModel(
                        app: _app,
                        navigationService: _navigationService)
                    {
                        Tokens = nftGroup.Select(g => g)
                    })
                    .OrderByDescending(collectible => collectible.Amount != 0)
                    .ThenBy(collectible => collectible.Name));
            }
            catch (Exception e)
            {
                Log.Error(e, "Load collectibles error");
            }
            finally
            {
                IsCollectiblesLoading = false;
            }
        }

        private ReactiveCommand<CollectibleViewModel, Unit> _selectCollectible;

        public ReactiveCommand<CollectibleViewModel, Unit> SelectCollectible => _selectCollectible ??=
            ReactiveCommand.Create<CollectibleViewModel>((collectible) =>
            {
                _navigationService?.ShowPage(new CollectiblePage(collectible), TabNavigation.Portfolio);
            });
    }
}