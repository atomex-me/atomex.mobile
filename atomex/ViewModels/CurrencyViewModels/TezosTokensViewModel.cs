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
        [Reactive] private IList<TezosTokenViewModel> _initialTokens { get; set; }
        [Reactive] public bool IsTokensLoading { get; set; }
        [Reactive] public TezosTokenViewModel SelectedToken { get; set; }
        private TezosTokenViewModel _openToken;

        public string BaseCurrencyCode => "USD";

        public const double DefaultTokenRowHeight = 76;
        public const double TokenListHeaderHeight = 76;
        [Reactive] public double TokenListViewHeight { get; set; }

        [Reactive] public string SearchPattern { get; set; }
        private bool _searched;

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
                .SubscribeInMainThread(async _ =>
                    await GetTokensAsync());

            this.WhenAnyValue(vm => vm.UserTokens)
                .Where(_ => !_searched)
                .WhereNotNull()
                .SubscribeInMainThread(vm =>
                    TokenListViewHeight = (UserTokens.Count + 1) * DefaultTokenRowHeight +
                                          TokenListHeaderHeight);

            this.WhenAnyValue(vm => vm.SelectedToken)
                .WhereNotNull()
                .SubscribeInMainThread(async (token) =>
                {
                    _navigationService?.ShowPage(new TokenPage(token), TabNavigation.Portfolio);
                    _openToken = token;

                    await Task.Run(async () =>
                    {
                        await SelectedToken.LoadTransfers();
                        SelectedToken.LoadAddresses();
                    });

                    SelectedToken = null;
                });

            this.WhenAnyValue(vm => vm.SearchPattern)
                .SubscribeInMainThread(searchPattern =>
                {
                    if (UserTokens == null) return;

                    _searched = true;

                    var tokens = new ObservableCollection<TezosTokenViewModel>(
                        _initialTokens
                            .Where(token =>
                            {
                                if (token.TokenBalance.Name != null && token.TokenBalance.Symbol != null)
                                {
                                    return token.TokenBalance.Name.ToLower().Contains(searchPattern.ToLower()) ||
                                           token.TokenBalance.Symbol.ToLower().Contains(searchPattern.ToLower()) ||
                                           token.TokenBalance.Contract.Contains(searchPattern.ToLower());
                                }

                                return token.TokenBalance.Contract.Contains(searchPattern.ToLower());
                            }));

                    UserTokens = new ObservableCollection<TezosTokenViewModel>(tokens)
                        .OrderByDescending(token => token.IsConvertable)
                        .ThenByDescending(token => token.TotalAmountInBase)
                        .ToList();

                    _searched = false;
                });

            _ = ReloadTokenContractsAsync();
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

            return tokens.OrderByDescending(token => token.IsConvertable)
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
                    .Where(t => !t.IsSelected)
                    .Select(token => token.TezosTokenViewModel.CurrencyCode)
                    .ToArray();

                Device.InvokeOnMainThreadAsync(() =>
                {
                    UserTokens = AllTokens?
                        .Where(c => c.IsSelected)
                        .Select(vm => vm.TezosTokenViewModel)
                        .ToList();

                    _initialTokens = UserTokens != null
                        ? new List<TezosTokenViewModel>(UserTokens)
                        : new List<TezosTokenViewModel>();
                });

                _app.Account.UserData.DisabledTokens = disabledTokens;
                _app.Account.UserData.SaveToFile(_app.Account.SettingsFilePath);
            }
            catch (Exception e)
            {
                Log.Error(e, "Change user tokens error");
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
                                                   args.TokenId != _openToken.TokenBalance.TokenId))
                {
                    return;
                }

                if (_openToken != null)
                    _ = _openToken.LoadTransfers();

                await Device.InvokeOnMainThreadAsync(async () => { await ReloadTokenContractsAsync(); });
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

                var tokens = await Task.Run(
                    () => LoadTokens());

                await Device.InvokeOnMainThreadAsync(() =>
                {
                    AllTokens = new ObservableCollection<TezosToken>(tokens
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
                        .ThenByDescending(token => token.TezosTokenViewModel.TotalAmountInBase));

                    UserTokens = new ObservableCollection<TezosTokenViewModel>(AllTokens
                        .Where(c => c.IsSelected)
                        .Select(vm => vm.TezosTokenViewModel)
                        .ToList());

                    _initialTokens = new List<TezosTokenViewModel>(UserTokens);
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
            (_updateTokensCommand = ReactiveCommand.CreateFromTask(UpdateTokens));

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
                {
                    _navigationService?.DisplaySnackBar(
                        MessageType.Regular,
                        AppResources.TezosTokens + " " + AppResources.HasBeenUpdated);
                });
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

        public ReactiveCommand<Unit, Unit> ManageTokensCommand => _manageTokensCommand ??= ReactiveCommand.Create(() =>
            _navigationService?.ShowPopup(new ManageTokensBottomSheet(this)));

        private ReactiveCommand<Unit, Unit> _tokensActionSheetCommand;

        public ReactiveCommand<Unit, Unit> TokensActionSheetCommand => _tokensActionSheetCommand ??=
            ReactiveCommand.Create(() =>
                _navigationService?.ShowPopup(new TokensActionBottomSheet(this)));

        private ICommand _closeActionSheetCommand;

        public ICommand CloseActionSheetCommand => _closeActionSheetCommand ??=
            new Command(() => _navigationService?.ClosePopup());

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