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
using atomex.Views.TezosTokens;
using Atomex;
using Atomex.Blockchain.Tezos;
using Atomex.Client.Common;
using Atomex.Wallet;
using Atomex.Wallet.Abstract;
using Atomex.Wallet.Tezos;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Serilog;
using Xamarin.Forms;
using static atomex.Models.SnackbarMessage;

namespace atomex.ViewModels.CurrencyViewModels
{
    public class TezosToken : BaseViewModel
    {
        public TezosTokenViewModel TezosTokenViewModel { get; set; }
        [Reactive] public bool IsSelected { get; set; }
        public Action<string, bool> OnChanged { get; set; }

        public TezosToken(
            TezosTokenViewModel tezosTokenViewModel,
            bool isSelected)
        {
            TezosTokenViewModel = tezosTokenViewModel;
            IsSelected = isSelected;

            this.WhenAnyValue(vm => vm.IsSelected)
                .Skip(1)
                .SubscribeInMainThread(flag =>
                    OnChanged?.Invoke(TezosTokenViewModel.CurrencyCode, flag));
        }

        private ICommand _toggleCommand;

        public ICommand ToggleCommand => _toggleCommand ??= ReactiveCommand.Create(() =>
            IsSelected = !IsSelected);
    }

    public class TezosTokensViewModel : BaseViewModel
    {
        private readonly IAtomexApp _app;
        private IAccount _account;
        private readonly INavigationService _navigationService;

        [Reactive] private ObservableCollection<TokenContract> Contracts { get; set; }

        [Reactive] public IList<TezosToken> AllTokens { get; set; }
        [Reactive] public IList<TezosTokenViewModel> UserTokens { get; set; }
        [Reactive] public IList<TezosTokenViewModel> DisplayedTokens { get; set; }
        [Reactive] public TezosTokenViewModel SelectedToken { get; set; }

        [Reactive] public int QtyDisplayedTokens { get; set; }
        private int _defaultQtyDisplayedTokens = 10;
        [Reactive] public bool IsTokensLoading { get; set; }
        public int LoadingStepTokens => 20;

        private TezosTokenViewModel _openToken;

        [Reactive] public string SearchPattern { get; set; }

        public TezosTokensViewModel(
            IAtomexApp app,
            INavigationService navigationService)
        {
            _app = app ?? throw new ArgumentNullException(nameof(app));
            _account = app.Account ?? throw new ArgumentNullException(nameof(_account));
            _navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));

            SubscribeToServices(_app);

            this.WhenAnyValue(vm => vm.Contracts)
                .WhereNotNull()
                .Subscribe(async _ => await GetTokensAsync());

            this.WhenAnyValue(vm => vm.SelectedToken)
                .WhereNotNull()
                .SubscribeInMainThread(async token =>
                {
                    _navigationService?.ShowPage(new TokenPage(token), TabNavigation.Portfolio);
                    _openToken = token;

                    await Task.Run(async () =>
                    {
                        await SelectedToken.LoadTransfersAsync();
                        SelectedToken.LoadAddresses();
                    });

                    SelectedToken = null;
                });

            this.WhenAnyValue(vm => vm.SearchPattern)
                .WhereNotNull()
                .Subscribe(async searchPattern =>
                {
                    if (UserTokens == null) return;

                    var tokens = await Task.Run(() =>
                    {
                        return UserTokens
                            .Where(token =>
                            {
                                if (token.TokenBalance.Name != null && token.TokenBalance.Symbol != null)
                                {
                                    return token.TokenBalance.Name.ToLower().Contains(searchPattern.ToLower()) ||
                                           token.TokenBalance.Symbol.ToLower().Contains(searchPattern.ToLower()) ||
                                           token.TokenBalance.Contract.Contains(searchPattern.ToLower());
                                }

                                return token.TokenBalance.Contract.Contains(searchPattern.ToLower());
                            })
                            .OrderByDescending(token => token.IsConvertable)
                            .ThenByDescending(token => token.TotalAmountInBase)
                            .ToList();
                    });

                    await Device.InvokeOnMainThreadAsync(() =>
                    {
                        DisplayedTokens = new ObservableCollection<TezosTokenViewModel>(tokens);
                        QtyDisplayedTokens = tokens.Count;
                    });
                });

            _ = Task.Run(ReloadTokenContractsAsync);
        }

        private void SubscribeToServices(IAtomexApp app)
        {
            app.AtomexClientChanged += OnAtomexClientChanged;
            app.Account.BalanceUpdated += OnBalanceUpdatedEventHandler;
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
            var tokens = new List<TezosTokenViewModel>();

            foreach (var contract in Contracts)
            {
                var contractTokens = await TezosTokenViewModelCreator.CreateOrGet(
                    atomexApp: _app,
                    navigationService: _navigationService,
                    contract: contract,
                    isNft: false);
                tokens.AddRange(contractTokens);

                Log.Debug("{@Count} tokens for {@Contract} loaded", contractTokens.Count(), contract.Address);
            }

            return tokens
                .OrderByDescending(token => token.IsConvertable)
                .ThenByDescending(token => token.TotalAmountInBase);
        }

        private void ChangeUserTokens(
            string token,
            bool isSelected)
        {
            try
            {
                if (string.IsNullOrEmpty(token)) return;

                var disabledTokens = AllTokens
                    .Where(t1 => !t1.IsSelected)
                    .Select(t2 => t2.TezosTokenViewModel.CurrencyCode)
                    .ToArray();
                
                UserTokens = AllTokens?
                    .Where(c => c.IsSelected)
                    .Select(vm => vm.TezosTokenViewModel)
                    .ToList();
                
                var newQty = isSelected 
                    ? QtyDisplayedTokens + 1 
                    : QtyDisplayedTokens - 1;

                Device.InvokeOnMainThreadAsync(() =>
                {
                    DisplayedTokens = new ObservableCollection<TezosTokenViewModel>(
                        UserTokens?.Take(newQty) ?? new List<TezosTokenViewModel>());

                    QtyDisplayedTokens = newQty;
                });

                _app.Account.UserData.DisabledTokens = disabledTokens;
                _app.Account.UserData.SaveToFile(_app.Account.SettingsFilePath);
            }
            catch (Exception e)
            {
                Log.Error(e, "Change user tokens error");
            }
        }

        public ICommand LoadMoreTokensCommand => new Command(async () => await LoadMoreTokens());

        private async Task LoadMoreTokens()
        {
            if (IsTokensLoading ||
                QtyDisplayedTokens >= UserTokens.Count ||
                !string.IsNullOrEmpty(SearchPattern)) return;

            IsTokensLoading = true;

            try
            {
                if (UserTokens == null)
                    return;

                var tokens = UserTokens
                    .Skip(QtyDisplayedTokens)
                    .Take(LoadingStepTokens)
                    .ToList();

                if (!tokens.Any())
                    return;

                var resultTokens = DisplayedTokens.Concat(tokens);

                await Device.InvokeOnMainThreadAsync(() =>
                    {
                        DisplayedTokens = new ObservableCollection<TezosTokenViewModel>(resultTokens);
                        QtyDisplayedTokens += tokens.Count;
                    }
                );
            }
            catch (Exception e)
            {
                Log.Error(e, "Error loading more collectibles error");
            }
            finally
            {
                IsTokensLoading = false;
            }
        }

        private ReactiveCommand<Unit, Unit> _clearSearchPatternCommand;

        public ReactiveCommand<Unit, Unit> ClearSearchPatternCommand => _clearSearchPatternCommand ??=
            _clearSearchPatternCommand = ReactiveCommand.Create(() => { SearchPattern = string.Empty; });

        private ReactiveCommand<Unit, Unit> _hideLowBalancesCommand;

        public ReactiveCommand<Unit, Unit> HideLowBalancesCommand => _hideLowBalancesCommand ??= ReactiveCommand.Create(
            () =>
            {
                _navigationService?.ClosePopup();
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
                if (!args.IsTokenUpdate ||
                    args.TokenContract != null && (args.TokenContract != _openToken.Contract.Address ||
                                                   args.TokenId != _openToken.TokenBalance.TokenId)) return;

                if (_openToken != null)
                    await Task.Run(async () => await _openToken.LoadTransfersAsync());

                await Task.Run(async () => await ReloadTokenContractsAsync());
            }
            catch (Exception e)
            {
                Log.Error(e, "Account balance updated event handler error");
            }
        }

        private async Task GetTokensAsync()
        {
            try
            {
                IsTokensLoading = true;

                var disabledTokens = _app.Account.UserData?.DisabledTokens ?? Array.Empty<string>();
                var tokens = await Task.Run(LoadTokens);
                var tokensViewModels = tokens
                    .Select(token =>
                    {
                        var vm = new TezosToken(
                            tezosTokenViewModel: token,
                            isSelected: !disabledTokens.Contains(token.CurrencyCode))
                        {
                            OnChanged = ChangeUserTokens
                        };

                        return vm;
                    })
                    .OrderByDescending(token => token.TezosTokenViewModel.IsConvertable)
                    .ThenByDescending(token => token.TezosTokenViewModel.TotalAmountInBase);

                await Device.InvokeOnMainThreadAsync(() =>
                {
                    AllTokens = new ObservableCollection<TezosToken>(tokensViewModels);

                    UserTokens = new ObservableCollection<TezosTokenViewModel>(AllTokens
                        .Where(c => c.IsSelected)
                        .Select(vm => vm.TezosTokenViewModel)
                        .ToList());

                    DisplayedTokens = new ObservableCollection<TezosTokenViewModel>(
                        UserTokens.Take(QtyDisplayedTokens <= _defaultQtyDisplayedTokens
                            ? _defaultQtyDisplayedTokens
                            : QtyDisplayedTokens));

                    QtyDisplayedTokens = DisplayedTokens.Count;
                });
            }
            catch (Exception e)
            {
                Log.Error(e, "Get tezos tokens error");
            }
            finally
            {
                IsTokensLoading = false;
            }
        }

        private ReactiveCommand<Unit, Unit> _updateTokensCommand;

        public ReactiveCommand<Unit, Unit> UpdateTokensCommand => _updateTokensCommand ??=
            _updateTokensCommand = ReactiveCommand.CreateFromTask(UpdateTokens);

        public async Task UpdateTokens()
        {
            var cancellation = new CancellationTokenSource();
            IsTokensLoading = true;

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
                        AppResources.TezosTokens + " " + AppResources.HasBeenUpdated));
            }
            catch (OperationCanceledException)
            {
                Log.Debug("Tezos tokens update canceled");
            }
            catch (Exception e)
            {
                Log.Error(e, "Tezos tokens update error");
            }
            finally
            {
                IsTokensLoading = false;
            }
        }

        private ReactiveCommand<Unit, Unit> _manageTokensCommand;

        public ReactiveCommand<Unit, Unit> ManageTokensCommand => _manageTokensCommand ??=
            ReactiveCommand.Create(() =>
                _navigationService?.ShowPopup(new ManageTokensBottomSheet(this)));

        private ReactiveCommand<Unit, Unit> _tokensActionSheetCommand;

        public ReactiveCommand<Unit, Unit> TokensActionSheetCommand => _tokensActionSheetCommand ??=
            ReactiveCommand.Create(() =>
                _navigationService?.ShowPopup(new TokensActionBottomSheet(this)));

        private ICommand _closeActionSheetCommand;

        public ICommand CloseActionSheetCommand => _closeActionSheetCommand ??=
            new Command(() => _navigationService?.ClosePopup());

        public void Reset()
        {
            try
            {
                SearchPattern = null;
                _openToken = null;

                if (UserTokens == null)
                    return;

                var tokens = UserTokens
                    .Take(_defaultQtyDisplayedTokens)
                    .ToList();

                if (!tokens.Any())
                    return;

                Device.InvokeOnMainThreadAsync(() =>
                    {
                        DisplayedTokens = new ObservableCollection<TezosTokenViewModel>(tokens);
                        QtyDisplayedTokens = tokens.Count;
                    }
                );
            }
            catch (Exception e)
            {
                Log.Error(e, "Reset QtyDisplayedTxs error");
            }
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

                    if (_app != null)
                        _app.AtomexClientChanged -= OnAtomexClientChanged;
                }

                _account = null;
                _disposedValue = true;
            }
        }

        ~TezosTokensViewModel()
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