﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
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
using Atomex.Common;
using Atomex.Wallet;
using Atomex.Wallet.Abstract;
using Atomex.Wallet.Tezos;
using DynamicData;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Serilog;
using Xamarin.Essentials;
using Xamarin.Forms;
using static atomex.Models.SnackbarMessage;

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
                    OnChanged?.Invoke(TokenViewModel.CurrencyCode, flag);
                });
        }

        private ICommand _toggleCommand;
        public ICommand ToggleCommand => _toggleCommand ??= ReactiveCommand.Create(() => IsSelected = !IsSelected);
    }

    public class TezosTokensViewModel : BaseViewModel
    {
        private IAtomexApp _app { get; }
        private IAccount _account { get; set; }
        private INavigationService _navigationService { get; set; }
        private string _walletName { get; set; }
        [Reactive] private ObservableCollection<TokenContract> Contracts { get; set; }

        [Reactive] public IList<TezosToken> AllTokens { get; set; }
        [Reactive] public IList<TezosTokenViewModel> UserTokens { get; set; }
        [Reactive] public bool IsTokensLoading { get; set; }
        [Reactive] public bool IsUpdating { get; set; }
        [Reactive] public TezosTokenViewModel SelectedToken { get; set; }
        private TezosTokenViewModel _openToken;

        public string BaseCurrencyCode => "USD";

        public const double DefaultTokenRowHeight = 76;
        public const double TokenListHeaderHeight = 72;
        [Reactive] public double TokenListViewHeight { get; set; }

        public TezosTokensViewModel(
            IAtomexApp app,
            INavigationService navigationService)
        {
            _app = app ?? throw new ArgumentNullException(nameof(_app));
            _account = app.Account ?? throw new ArgumentNullException(nameof(_account));
            _navigationService = navigationService ?? throw new ArgumentNullException(nameof(_navigationService));
            _walletName = GetWalletName();
            
            SubscribeToServices(_app);

            this.WhenAnyValue(vm => vm.Contracts)
                .WhereNotNull()
                .SubscribeInMainThread(async _ =>
                    await GetTokensAsync());

            this.WhenAnyValue(vm => vm.UserTokens)
                .WhereNotNull()
                .SubscribeInMainThread(vm =>
                {
                    TokenListViewHeight = (UserTokens.Count + 1) * DefaultTokenRowHeight +
                        TokenListHeaderHeight;
                });

            this.WhenAnyValue(vm => vm.SelectedToken)
                .WhereNotNull()
                .SubscribeInMainThread(async (token) =>
                {
                    _navigationService?.ShowPage(new TokenPage(token), TabNavigation.Portfolio);
                    _openToken = token;
                    await Task.Run(() => SelectedToken.LoadTransfers());

                    SelectedToken = null;
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
            var tokens = new ObservableCollection<TezosTokenViewModel>();

            foreach (var contract in Contracts)
                tokens.AddRange(await TezosTokenViewModelCreator.CreateOrGet(_app, _navigationService, contract));

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

                Device.InvokeOnMainThreadAsync(() =>
                {
                    AllTokens?.Select(t => t.IsSelected == tokensList.Contains(t.TokenViewModel.CurrencyCode));

                    UserTokens = AllTokens?
                        .Where(c => c.IsSelected)
                        .Select(vm => vm.TokenViewModel)
                        .ToList();
                });
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
                if (!Atomex.Currencies.IsTezosToken(args.Currency)) return;

                if (_openToken != null)
                    _ = _openToken.LoadTransfers();

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

        private async Task GetTokensAsync()
        {
            try
            {
                IsTokensLoading = true;

                var userTokens = Preferences.Get(_walletName + "-" + "TezosTokens", string.Empty);
                var tokens = await Task.Run(
                    () => LoadTokens());

                List<string> result = new List<string>();
                result = userTokens != string.Empty
                    ? userTokens.Split(',').ToList()
                    : tokens.Select(t => t.CurrencyCode).ToList();

                await Device.InvokeOnMainThreadAsync(() =>
                {
                    AllTokens = new ObservableCollection<TezosToken>(tokens
                    .Select(token =>
                    {
                        var vm = new TezosToken
                        {
                            TokenViewModel = token,
                            IsSelected = result != null ? result.Contains(token.CurrencyCode) : false,
                            OnChanged = ChangeUserTokens
                        };

                        return vm;
                    })
                    .OrderByDescending(token => token.TokenViewModel.IsConvertable)
                    .ThenByDescending(token => token.TokenViewModel.TotalAmountInBase));

                    UserTokens = new ObservableCollection<TezosTokenViewModel>(AllTokens
                        .Where(c => c.IsSelected)
                        .Select(vm => vm.TokenViewModel)
                        .ToList());
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
            IsUpdating = true;

            try
            {
                var tezosAccount = _app.Account
                    .GetCurrencyAccount<TezosAccount>(TezosConfig.Xtz);

                var tezosTokensScanner = new TezosTokensScanner(tezosAccount);

                await Task.Run(async () =>
                {
                    await tezosTokensScanner.ScanAsync(
                        skipUsed: false,
                        cancellationToken: cancellation.Token);

                    // reload balances for all tezos tokens account
                    foreach (var currency in _app.Account.Currencies)
                        if (Atomex.Currencies.IsTezosToken(currency.Name))
                            _app.Account
                                .GetCurrencyAccount<TezosTokenAccount>(currency.Name)
                                .ReloadBalances();
                });

                await Device.InvokeOnMainThreadAsync(() =>
                {
                    _navigationService?.DisplaySnackBar(MessageType.Regular, AppResources.TezosTokens + " " + AppResources.HasBeenUpdated);
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
                IsUpdating = false;
            }
        }

        private ReactiveCommand<Unit, Unit> _manageTokensCommand;
        public ReactiveCommand<Unit, Unit> ManageTokensCommand => _manageTokensCommand ??= ReactiveCommand.Create(() =>
                _navigationService?.ShowBottomSheet(new ManageTokensBottomSheet(this)));

        private ReactiveCommand<Unit, Unit> _tokensActionSheetCommand;
        public ReactiveCommand<Unit, Unit> TokensActionSheetCommand => _tokensActionSheetCommand ??= ReactiveCommand.Create(() =>
            _navigationService?.ShowBottomSheet(new TokensActionBottomSheet(this)));

        private ICommand _closeActionSheetCommand;
        public ICommand CloseActionSheetCommand => _closeActionSheetCommand ??=
            new Command(() => _navigationService?.CloseBottomSheet());

        private string GetWalletName()
        {
            return new DirectoryInfo(_app?.Account?.Wallet?.PathToWallet).Parent.Name;
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