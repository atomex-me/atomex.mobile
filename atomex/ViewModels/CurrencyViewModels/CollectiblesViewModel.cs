using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Atomex;
using Atomex.Blockchain.Tezos;
using Atomex.Client.Common;
using atomex.Common;
using atomex.Resources;
using atomex.Views.Collectibles;
using Atomex.Wallet;
using Atomex.Wallet.Tezos;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Serilog;
using Xamarin.Forms;
using static atomex.Models.SnackbarMessage;

namespace atomex.ViewModels.CurrencyViewModels
{
    public class Collectible : BaseViewModel
    {
        public CollectibleViewModel CollectibleViewModel { get; set; }
        [Reactive] public bool IsSelected { get; set; }
        public Action<string, bool> OnChanged { get; set; }

        public Collectible(
            CollectibleViewModel collectibleViewModel,
            bool isSelected)
        {
            CollectibleViewModel = collectibleViewModel;
            IsSelected = isSelected;

            this.WhenAnyValue(vm => vm.IsSelected)
                .Skip(1)
                .SubscribeInMainThread(flag =>
                    OnChanged?.Invoke(CollectibleViewModel.Name, flag));
        }

        private ICommand _toggleCommand;

        public ICommand ToggleCommand => _toggleCommand ??= ReactiveCommand.Create(() =>
            IsSelected = !IsSelected);
    }

    public class CollectiblesViewModel : BaseViewModel
    {
        private readonly IAtomexApp _app;
        private readonly INavigationService _navigationService;
        [Reactive] private IList<TokenContract> Contracts { get; set; }
        [Reactive] public IList<Collectible> AllCollectibles { get; set; }
        [Reactive] public IList<CollectibleViewModel> UserCollectibles { get; set; }
        [Reactive] public IList<CollectibleViewModel> DisplayedCollectibles { get; set; }
        [Reactive] public bool IsCollectiblesLoading { get; set; }
        [Reactive] public CollectibleViewModel SelectedCollectible { get; set; }

        [Reactive] public int QtyDisplayedCollectibles { get; set; }
        private int _defaultQtyDisplayedCollectibles = 10;
        public int LoadingStepCollectibles => 10;

        [Reactive] public string SearchPattern { get; set; }

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
                .Subscribe(collectibles =>
                    _ = LoadCollectibles());

            this.WhenAnyValue(vm => vm.SelectedCollectible)
                .WhereNotNull()
                .SubscribeInMainThread(async (collectible) =>
                {
                    _navigationService?.ShowPage(new CollectiblePage(collectible), TabNavigation.Portfolio);
                    SelectedCollectible = null;
                });

            this.WhenAnyValue(vm => vm.SearchPattern)
                .WhereNotNull()
                .Subscribe(async searchPattern =>
                {
                    if (UserCollectibles == null) return;

                    var collectibles = await Task.Run(() =>
                    {
                        return UserCollectibles
                            .Where(c => 
                                c.Name?.ToLower().Contains(searchPattern.ToLower()) ?? false)
                            .OrderByDescending(collectible => collectible.Amount != 0)
                            .ThenBy(c => c.Name)
                            .ToList();
                    });

                    await Device.InvokeOnMainThreadAsync(() =>
                    {
                        DisplayedCollectibles = new ObservableCollection<CollectibleViewModel>(collectibles);
                        QtyDisplayedCollectibles = collectibles.Count;
                    });
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
                if (args.IsTokenUpdate &&
                    (args.TokenContract == null || (Contracts != null && Contracts.Select(c => c.Address)
                        .Contains(args.TokenContract))))
                    await Task.Run(async () => await ReloadTokenContractsAsync());
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
                var disabledCollectibles = _app.Account.UserData?.DisabledCollectibles ?? Array.Empty<string>();

                await Task.Run(async () =>
                {
                    foreach (var contract in Contracts)
                        nftTokens.AddRange(await TezosTokenViewModelCreator.CreateOrGet(
                            atomexApp: _app,
                            navigationService: _navigationService,
                            contract: contract,
                            isNft: true));
                });

                var groupedNftTokens = nftTokens
                    .GroupBy(nft => nft.Contract.Address)
                    .Select(collectible =>
                    {
                        var vm = new Collectible(
                            collectibleViewModel: new CollectibleViewModel(
                                app: _app,
                                navigationService: _navigationService)
                            {
                                Tokens = collectible
                                    .Select(g => g)
                                    .OrderByDescending(t => t.TotalAmount != 0)
                                    .ThenBy(token => token.TokenBalance?.Name)
                            },
                            isSelected: !disabledCollectibles.Contains(collectible.First().Contract.Address))
                        {
                            OnChanged = ChangeUserCollectibles
                        };

                        return vm;
                    })
                    .OrderByDescending(collectible => collectible.CollectibleViewModel.Amount != 0)
                    .ThenBy(collectible => collectible.CollectibleViewModel.Name);

                await Device.InvokeOnMainThreadAsync(() =>
                    AllCollectibles = new ObservableCollection<Collectible>(groupedNftTokens));

                UserCollectibles = new ObservableCollection<CollectibleViewModel>(AllCollectibles
                    .Where(c => c.IsSelected)
                    .Select(vm => vm.CollectibleViewModel)
                    .ToList());

                var collectibles = new ObservableCollection<CollectibleViewModel>(
                    UserCollectibles
                        .Take(QtyDisplayedCollectibles <= _defaultQtyDisplayedCollectibles 
                            ? _defaultQtyDisplayedCollectibles 
                            : QtyDisplayedCollectibles))
                    .ToList();

                await Device.InvokeOnMainThreadAsync(() =>
                {
                    DisplayedCollectibles = new ObservableCollection<CollectibleViewModel>(collectibles);
                    QtyDisplayedCollectibles = collectibles.Count;
                });
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

        public ICommand LoadMoreCollectiblesCommand => new Command(async () => await LoadMoreCollectibles());

        private async Task LoadMoreCollectibles()
        {
            if (IsCollectiblesLoading ||
                QtyDisplayedCollectibles >= UserCollectibles.Count ||
                !string.IsNullOrEmpty(SearchPattern)) return;

            IsCollectiblesLoading = true;

            try
            {
                if (UserCollectibles == null)
                    return;

                var collectibles = UserCollectibles
                    .Skip(QtyDisplayedCollectibles)
                    .Take(LoadingStepCollectibles)
                    .ToList();

                if (!collectibles.Any())
                    return;

                var resultCollectibles = DisplayedCollectibles.Concat(collectibles);

                await Device.InvokeOnMainThreadAsync(() =>
                    {
                        DisplayedCollectibles = new ObservableCollection<CollectibleViewModel>(resultCollectibles);
                        QtyDisplayedCollectibles += collectibles.Count;
                    }
                );
            }
            catch (Exception e)
            {
                Log.Error(e, "Error loading more collectibles error");
            }
            finally
            {
                IsCollectiblesLoading = false;
            }
        }

        private void ChangeUserCollectibles(
            string collectible,
            bool isSelected)
        {
            try
            {
                if (string.IsNullOrEmpty(collectible)) return;

                var disabledCollectibles = AllCollectibles
                    .Where(c => !c.IsSelected)
                    .Select(collectible => collectible.CollectibleViewModel.ContractAddress)
                    .ToArray();

                UserCollectibles = AllCollectibles?
                    .Where(c => c.IsSelected)
                    .Select(vm => vm.CollectibleViewModel)
                    .ToList();
                
                var newQty = isSelected 
                    ? QtyDisplayedCollectibles + 1 
                    : QtyDisplayedCollectibles - 1;
                
                Device.InvokeOnMainThreadAsync(() =>
                {
                    DisplayedCollectibles = new ObservableCollection<CollectibleViewModel>(
                        UserCollectibles?.Take(newQty) ?? new List<CollectibleViewModel>());

                    QtyDisplayedCollectibles = newQty;
                });

                _app.Account.UserData.DisabledCollectibles = disabledCollectibles;
                _app.Account.UserData.SaveToFile(_app.Account.SettingsFilePath);
            }
            catch (Exception e)
            {
                Log.Error(e, "Change user collectibles error");
            }
        }

        private ReactiveCommand<Unit, Unit> _clearSearchPatternCommand;

        public ReactiveCommand<Unit, Unit> ClearSearchPatternCommand => _clearSearchPatternCommand ??=
            _clearSearchPatternCommand = ReactiveCommand.Create(() => { SearchPattern = string.Empty; });

        private ReactiveCommand<Unit, Unit> _manageCollectiblesCommand;

        public ReactiveCommand<Unit, Unit> ManageCollectiblesCommand => _manageCollectiblesCommand ??=
            ReactiveCommand.Create(() =>
                _navigationService?.ShowPopup(new ManageCollectiblesBottomSheet(this)));

        private ReactiveCommand<CollectibleViewModel, Unit> _selectCollectible;

        public ReactiveCommand<CollectibleViewModel, Unit> SelectCollectible => _selectCollectible ??=
            ReactiveCommand.Create<CollectibleViewModel>(collectible =>
                _navigationService?.ShowPage(new CollectiblePage(collectible), TabNavigation.Portfolio));

        private ReactiveCommand<Unit, Unit> _updateCollectiblesCommand;

        public ReactiveCommand<Unit, Unit> UpdateCollectiblesCommand => _updateCollectiblesCommand ??=
            (_updateCollectiblesCommand = ReactiveCommand.CreateFromTask(UpdateCollectibles));

        public async Task UpdateCollectibles()
        {
            var cancellation = new CancellationTokenSource();
            IsCollectiblesLoading = true;

            try
            {
                var tezosAccount = _app.Account
                    .GetCurrencyAccount<TezosAccount>(TezosConfig.Xtz);

                var tezosTokensScanner = new TezosTokensScanner(tezosAccount);

                await tezosTokensScanner.UpdateBalanceAsync(
                    cancellationToken: cancellation.Token);

                await Device.InvokeOnMainThreadAsync(() =>
                    _navigationService?.DisplaySnackBar(
                        MessageType.Regular,
                        AppResources.Collectibles + " " + AppResources.HasBeenUpdated));
            }
            catch (OperationCanceledException)
            {
                Log.Debug("Tezos collectibles update canceled");
            }
            catch (Exception e)
            {
                Log.Error(e, "Tezos collectibles update error");
            }
            finally
            {
                IsCollectiblesLoading = false;
            }
        }

        public void Reset()
        {
            try
            {
                SearchPattern = null;

                if (UserCollectibles == null)
                    return;

                var collectibles = UserCollectibles
                    .Take(_defaultQtyDisplayedCollectibles)
                    .ToList();

                if (!collectibles.Any())
                    return;

                Device.InvokeOnMainThreadAsync(() =>
                    {
                        DisplayedCollectibles = new ObservableCollection<CollectibleViewModel>(collectibles);
                        QtyDisplayedCollectibles = collectibles.Count;
                    }
                );
            }
            catch (Exception e)
            {
                Log.Error(e, "Reset QtyDisplayedTxs error");
            }
        }

        private ICommand _closeActionSheetCommand;

        public ICommand CloseActionSheetCommand => _closeActionSheetCommand ??=
            new Command(() => _navigationService?.ClosePopup());
    }
}