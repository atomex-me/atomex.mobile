using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using atomex.Common;
using atomex.Models;
using atomex.Resources;
using atomex.Views.Delegate;
using Atomex;
using Atomex.Blockchain.Tezos;
using Atomex.Blockchain.Tezos.Internal;
using Atomex.Common;
using Atomex.Core;
using Atomex.MarketData.Abstract;
using Atomex.Wallet;
using Atomex.Wallet.Tezos;
using Newtonsoft.Json.Linq;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Serilog;
using Xamarin.Essentials;
using Xamarin.Forms;
using static atomex.Models.Message;

namespace atomex.ViewModel
{
    public enum DelegationSortField
    {
        ByRating,
        ByRoi,
        ByValidator,
        ByMinTez
    }

    public enum SortDirection
    {
        [Description("Asc")]
        Asc,
        [Description("Desc")]
        Desc
    }

    public class DelegateViewModel : BaseViewModel
    {
        private IAtomexApp _app { get; }
        private INavigationService _navigationService { get; }
        private readonly TezosConfig _tezosConfig;

        [Reactive] public string DelegateAddress { get; set; }
        [Reactive] public decimal DelegateAddressBalance { get; set; }
        [Reactive] public decimal DelegateAddressBalanceInBase { get; set; }
        [Reactive] public List<BakerViewModel> BakersList { get; set; }
        private List<BakerViewModel> _initialBakersList { get; set; }
        [Reactive] public BakerViewModel SelectedBaker { get; set; }
        [Reactive] public decimal Fee { get; set; }
        [Reactive] public string BaseCurrencyFormat { get; set; }
        [Reactive] public string FeeFormat { get; set; }
        [Reactive] public decimal FeeInBase { get; set; }
        [Reactive] public string FeeCurrencyCode { get; set; }
        [Reactive] public string BaseCurrencyCode { get; set; }
        [Reactive] public bool UseDefaultFee { get; set; }
        [Reactive] public string SearchPattern { get; set; }
        [Reactive] public DelegationSortField CurrentSortField { get; set; }
        [Reactive] public SortDirection CurrentSortDirection { get; set; }
        [Reactive] public Message Message { get; set; }
        [Reactive] public bool IsLoading { get; set; }
        [Reactive] public string SortButtonName { get; set; }
        [Reactive] public string Title { get; set; }
        [Reactive] public bool CanDelegate { get; set; }
        public string UndelegateWarning { get; set; }

        private ReactiveCommand<Unit, Unit> _delegateCommand;
        public ReactiveCommand<Unit, Unit> DelegateCommand => _delegateCommand ??= ReactiveCommand.CreateFromTask(
            async () =>
            {
                //await Task.Delay(Constants.DelayBeforeSendMs);
                await Delegate(fee: Fee); 
            });

        private ICommand _undoConfirmStageCommand;
        public ICommand UndoConfirmStageCommand =>
            _undoConfirmStageCommand ??= new Command(() => _navigationService?.CloseBottomSheet());

        private ReactiveCommand<Unit, Unit> _setSortTypeCommand;
        public ReactiveCommand<Unit, Unit> SetSortTypeCommand =>
            _setSortTypeCommand ??= ReactiveCommand.Create(() =>
            {
                CurrentSortDirection = CurrentSortDirection == SortDirection.Asc
                    ? SortDirection.Desc
                    : SortDirection.Asc;
            });

        private ReactiveCommand<Unit, Unit> _changeSortFieldCommand;
        public ReactiveCommand<Unit, Unit> ChangeSortFieldCommand => _changeSortFieldCommand ??=
            (_changeSortFieldCommand = ReactiveCommand.Create(() =>
            {
                CurrentSortField = CurrentSortField.Next();
            }));

        private ReactiveCommand<Unit, Unit> _searchCommand;
        public ReactiveCommand<Unit, Unit> SearchCommand =>
            _searchCommand ??= (_searchCommand = ReactiveCommand.Create(() =>
                _navigationService?.ShowPage(new SearchBakerPage(this), TabNavigation.Portfolio)));

        private ICommand _clearSearchFieldCommand;
        public ICommand ClearSearchFieldCommand =>
            _clearSearchFieldCommand ??= new Command(() => SearchPattern = string.Empty);

        private ReactiveCommand<string, Unit> _copyCommand;
        public ReactiveCommand<string, Unit> CopyCommand => _copyCommand ??= ReactiveCommand.Create<string>(value =>
        {
            Device.InvokeOnMainThreadAsync(() =>
            {
                if (value != null)
                {
                    _ = Clipboard.SetTextAsync(value);
                    _navigationService?.DisplaySnackBar(SnackbarMessage.MessageType.Regular, AppResources.AddressCopied);
                }
                else
                {
                    _navigationService?.ShowAlert(AppResources.Error, AppResources.CopyError, AppResources.AcceptButton);
                }
            });
        });

        private async Task CheckDelegation()
        {
            try
            {
                IsLoading = true;

                if (!_tezosConfig.IsValidAddress(SelectedBaker?.Address))
                {
                    ShowMessage(
                        messageType: MessageType.Error,
                        text: AppResources.InvalidAddressError);
                    return;
                }

                if (DelegateAddress == null)
                {
                    ShowMessage(
                        messageType: MessageType.Error,
                        text: AppResources.InvalidAddressError);
                    return;
                }

                if (Fee < 0)
                {
                    ShowMessage(
                        messageType: MessageType.Error,
                        text: AppResources.CommissionLessThanZeroError);
                    return;
                }

                if (Fee > DelegateAddressBalance - (SelectedBaker?.MinDelegation ?? 0))
                {
                    ShowMessage(
                        messageType: MessageType.Error,
                        text: AppResources.AvailableFundsError);
                    return;
                }

                var result = await GetDelegate();

                if (result.HasError)
                {
                    ShowMessage(
                        messageType: MessageType.Error,
                        text: result.Error.Description);
                }
            }
            catch (Exception e)
            {
                Log.Error(e, "Delegation check error");
            }
            finally
            {
                IsLoading = false;
            }
        }

        public async Task Undelegate(string address)
        {
            DelegateAddress = address;
            SelectedBaker = null;

            var autofillOperation = await RunAutofillOperation(
                DelegateAddress,
                null,
                default);

            if (autofillOperation.HasError)
            {
                _navigationService?.ShowAlert(
                    AppResources.Error,
                    autofillOperation.Error.Description,
                    AppResources.AcceptButton);

                return;
            }

            var (tx, isSuccess, isRunSuccess) = autofillOperation.Value;

            if (!isSuccess || !isRunSuccess)
            {
                _navigationService?.ShowAlert(
                    AppResources.Error,
                    "Autofill transaction failed",
                    AppResources.AcceptButton);

                return;
            }

            UndelegateWarning = string.Format(AppResources.UndelegateWarning, address, tx.Fee);
            _navigationService?.ShowBottomSheet(new UndoDelegationBottomSheet(this));
        }

        private async Task Delegate(decimal fee)
        {
            IsLoading = true;

            var wallet = (HdWallet)_app.Account.Wallet;
            var keyStorage = wallet.KeyStorage;

            var walletAddress = _app.Account
                .GetCurrencyAccount(TezosConfig.Xtz)
                .GetAddressAsync(DelegateAddress)
                .WaitForResult();

            var tezosAccount = _app.Account
                .GetCurrencyAccount<TezosAccount>(TezosConfig.Xtz);

            try
            {
                await tezosAccount.AddressLocker
                    .LockAsync(walletAddress.Address);

                // temporary fix: check operation sequence
                await TezosOperationsSequencer
                    .WaitAsync(walletAddress.Address, tezosAccount)
                    .ConfigureAwait(false);

                var tx = new TezosTransaction
                {
                    StorageLimit = _tezosConfig.StorageLimit,
                    GasLimit = _tezosConfig.GasLimit,
                    From = walletAddress.Address,
                    Fee = fee.ToMicroTez(),
                    Currency = _tezosConfig.Name,
                    CreationTime = DateTime.UtcNow,
                    UseRun = true,
                    UseOfflineCounter = true,
                    OperationType = OperationType.Delegation
                };

                if (SelectedBaker?.Address != null)
                    tx.To = SelectedBaker.Address;

                using var securePublicKey = _app.Account.Wallet.GetPublicKey(
                    currency: _tezosConfig,
                    keyIndex: walletAddress.KeyIndex,
                    keyType: walletAddress.KeyType);

                var _ = await tx.FillOperationsAsync(
                    securePublicKey: securePublicKey,
                    tezosConfig: _tezosConfig,
                    headOffset: TezosConfig.HeadOffset);

                var signResult = await tx
                    .SignAsync(keyStorage, walletAddress, _tezosConfig);

                if (!signResult)
                {
                    _navigationService?.ShowAlert(
                        AppResources.Error,
                        "Delegation transaction signing error",
                        AppResources.AcceptButton);

                    Log.Error("Delegation transaction signing error");
                    return;
                }

                var result = await _tezosConfig.BlockchainApi
                    .TryBroadcastAsync(tx);

                if (result.Error != null)
                {
                    _navigationService?.ShowAlert(
                        AppResources.Error,
                        result.Error.Description,
                        AppResources.AcceptButton);

                    return;
                }

                _navigationService?.CloseBottomSheet();
                await _navigationService?.ReturnToInitiatedPage(TabNavigation.Portfolio);

                var operationType = SelectedBaker?.Address != null
                    ? "delegated"
                    : "undelegated";

                await Device.InvokeOnMainThreadAsync(() =>
                {
                    _navigationService?.DisplaySnackBar(
                        SnackbarMessage.MessageType.Success,
                        $"Successfully {operationType}, your delegations list will updated very soon!");
                });
            }
            catch (HttpRequestException e)
            {
                _navigationService?.ShowAlert(
                    AppResources.Error,
                    "A network error has occurred while sending delegation transaction, check your internet connection and try again",
                    AppResources.AcceptButton);

                Log.Error(e, "Delegation send network error");
            }
            catch (Exception e)
            {
                _navigationService?.ShowAlert(
                    AppResources.Error,
                    "An error has occurred while delegation",
                    AppResources.AcceptButton);

                Log.Error(e, "Delegation send error");
            }
            finally
            {
                IsLoading = false;
                tezosAccount.AddressLocker.Unlock(walletAddress.Address);
            }
        }

        public DelegateViewModel(IAtomexApp app, INavigationService navigationService)
        {
            _app = app ?? throw new ArgumentNullException(nameof(app));
            _navigationService = navigationService ?? throw new ArgumentNullException(nameof(_navigationService));
            _tezosConfig = _app.Account.Currencies.Get<TezosConfig>(TezosConfig.Xtz);
            Message = new Message();

            this.WhenAnyValue(vm => vm.Fee)
                .SubscribeInMainThread(f => OnQuotesUpdatedEventHandler(_app.QuotesProvider, EventArgs.Empty));

            this.WhenAnyValue(vm => vm.SearchPattern)
                .WhereNotNull()
                .Select(searchPattern => searchPattern.ToLower())
                .SubscribeInMainThread(searchPattern =>
                {
                    if (searchPattern == string.Empty)
                    {
                        BakersList = new List<BakerViewModel>(_initialBakersList);
                        CurrentSortField = DelegationSortField.ByRating;
                        return;
                    }

                    BakersList = GetSortedBakersList(_initialBakersList)
                        .Where(baker => baker.Name.ToLower().Contains(searchPattern))
                        .ToList();
                });

            this.WhenAnyValue(
                    vm => vm.CurrentSortField,
                    vm => vm.CurrentSortDirection)
                .WhereAllNotNull()
                .Where(_ => BakersList != null)
                .SubscribeInMainThread(_ =>
                    {
                        BakersList = GetSortedBakersList(BakersList);

                        SortButtonName = CurrentSortField == DelegationSortField.ByRating
                            ? AppResources.SortByRatingButton
                            : CurrentSortField == DelegationSortField.ByRoi
                                ? AppResources.SortByRoiButton
                                : CurrentSortField == DelegationSortField.ByValidator
                                    ? AppResources.SortByValidatorButton
                                    : AppResources.SortByMinTezButton;
                    });

            this.WhenAnyValue(vm => vm.SelectedBaker)
                .WhereNotNull()
                .SubscribeInMainThread(selectedBaker =>
                    {
                        Message = new Message();
                        if (selectedBaker.StakingAvailable - DelegateAddressBalance < 0)
                            ShowMessage(
                                messageType: MessageType.Warning,
                                text: AppResources.BakerIsOverdelegatedWarning);
                        _ = CheckDelegation();

                        _navigationService?.ShowBottomSheet(new DelegationConfirmationBottomSheet(this));
                    });

            this.WhenAnyValue(
                    vm => vm.Message,
                    vm => vm.SelectedBaker,
                    vm => vm.Fee,
                    vm => vm.IsLoading)
                .WhereNotNull()
                .SubscribeInMainThread(selectedBaker =>
                {
                    CanDelegate = DelegateAddress != null &&
                                DelegateAddressBalance > 0 &&
                                Message.Type != MessageType.Error &&
                                !IsLoading;
                });

            FeeFormat = _tezosConfig.FeeFormat;
            FeeCurrencyCode = _tezosConfig.FeeCode;
            BaseCurrencyCode = "USD";
            BaseCurrencyFormat = "$0.00";
            UseDefaultFee = true;
            CurrentSortDirection = SortDirection.Desc;
            CurrentSortField = DelegationSortField.ByRating;
            SortButtonName = AppResources.SortByRatingButton;

            SubscribeToServices();
            _ = LoadBakerList();
        }

        public void InitializeWith(DelegationViewModel delegation)
        {
            DelegateAddress = delegation.Address;
            DelegateAddressBalance = delegation.Balance;

            if (delegation.Baker != null)
            {
                Title = AppResources.ChangeBaker;

                BakersList = new List<BakerViewModel>(BakersList?
                    .Select(baker =>
                    {
                        if (baker.Address == delegation.Baker.Address)
                            baker.IsCurrentlyActive = true;
                        return baker;
                    }));
            }
            else
            {
                Title = AppResources.DelegatingTo;
                SelectedBaker = null;
                BakersList = new List<BakerViewModel>(BakersList?
                    .Select(baker =>
                    {
                        if (baker.IsCurrentlyActive)
                            baker.IsCurrentlyActive = false;
                        return baker;
                    }));
            }

            OnQuotesUpdatedEventHandler(_app.QuotesProvider, EventArgs.Empty);
        }

        private async Task LoadBakerList()
        {
            List<BakerViewModel> bakers = null;

            try
            {
                await Task.Run(async () =>
                {
                    bakers = (await BbApi
                            .GetBakers(_app.Account.Network)
                            .ConfigureAwait(false))
                        .Select(bakerData => new BakerViewModel
                        {
                            Address = bakerData.Address,
                            Logo = bakerData.Logo,
                            Name = bakerData.Name,
                            Fee = bakerData.Fee,
                            Roi = bakerData.EstimatedRoi,
                            MinDelegation = bakerData.MinDelegation,
                            StakingAvailable = bakerData.StakingAvailable
                        })
                        .ToList();

                    // TODO: load logo from cash
                });
            }
            catch (Exception e)
            {
                Log.Error(e.Message, "Error while fetching bakers list");
            }

            await Device.InvokeOnMainThreadAsync(() =>
            {
                BakersList = bakers;
                _initialBakersList = new List<BakerViewModel>(BakersList);
                //UseDefaultFee = _useDefaultFee;
            });
        }

        private List<BakerViewModel> GetReverseInitialBakersList()
        {
            var bakers = new List<BakerViewModel>(_initialBakersList);
            bakers.Reverse();
            return bakers;
        }

        private List<BakerViewModel> GetSortedBakersList(IEnumerable<BakerViewModel> bakersList)
        {
            return CurrentSortField switch
            {
                DelegationSortField.ByRating when CurrentSortDirection == SortDirection.Desc
                    => _initialBakersList,
                DelegationSortField.ByRating when CurrentSortDirection == SortDirection.Asc
                    => GetReverseInitialBakersList(),

                DelegationSortField.ByRoi when CurrentSortDirection == SortDirection.Desc
                    => new List<BakerViewModel>(bakersList.OrderByDescending(baker => baker.Roi)),
                DelegationSortField.ByRoi when CurrentSortDirection == SortDirection.Asc
                    => new List<BakerViewModel>(bakersList.OrderBy(baker => baker.Roi)),

                DelegationSortField.ByMinTez when CurrentSortDirection == SortDirection.Desc
                    => new List<BakerViewModel>(bakersList.OrderByDescending(baker => baker.MinDelegation)),
                DelegationSortField.ByMinTez when CurrentSortDirection == SortDirection.Asc
                    => new List<BakerViewModel>(bakersList.OrderBy(baker => baker.MinDelegation)),

                DelegationSortField.ByValidator when CurrentSortDirection == SortDirection.Desc
                    => new List<BakerViewModel>(bakersList.OrderByDescending(baker => baker.Name)),
                DelegationSortField.ByValidator when CurrentSortDirection == SortDirection.Asc
                    => new List<BakerViewModel>(bakersList.OrderBy(baker => baker.Name)),

                _ => _initialBakersList
            };
        }

        private async Task<Result<bool>> GetDelegate(
            CancellationToken cancellationToken = default)
        {
            IsLoading = true;
            if (DelegateAddress == null)
                return new Error(Errors.InvalidWallets, "You don't have non-empty accounts.");

            JObject delegateData;

            try
            {
                var rpc = new Rpc(_tezosConfig.RpcNodeUri);

                delegateData = await rpc
                    .GetDelegate(SelectedBaker?.Address)
                    .ConfigureAwait(false);
            }
            catch (HttpRequestException)
            {
                return new Error(
                    Errors.InvalidConnection,
                    "A network error has occurred while checking baker data, " +
                    "check your internet connection and try again.");
            }
            catch (Exception)
            {
                return new Error(Errors.WrongDelegationAddress, "Wrong delegation address.");
            }

            if (delegateData["deactivated"].Value<bool>())
                return new Error(Errors.WrongDelegationAddress, "Baker is deactivated. Pick another one.");

            var delegators = delegateData["delegated_contracts"]?.Values<string>();

            if (delegators != null && delegators.Contains(DelegateAddress))
                return new Error(Errors.AlreadyDelegated,
                    $"Already delegated from {DelegateAddress} to {SelectedBaker?.Address}.");

            var autofillOperation = await RunAutofillOperation(
                DelegateAddress,
                SelectedBaker!.Address,
                cancellationToken);

            if (autofillOperation.HasError)
                return autofillOperation.Error;

            var (tx, isSuccess, isRunSuccess) = autofillOperation.Value;

            if (!isSuccess || !isRunSuccess)
                return new Error(Errors.TransactionCreationError, "Autofill transaction failed.");

            if (UseDefaultFee)
            {
                Fee = tx.Fee;

                if (Fee > DelegateAddressBalance)
                    return new Error(Errors.TransactionCreationError,
                        $"Insufficient funds at the address {DelegateAddress}.");
            }
            else
            {
                if (Fee < tx.Fee)
                    return new Error(Errors.TransactionCreationError,
                        $"Fee less than minimum {tx.Fee.ToString(CultureInfo.CurrentCulture)}.");
            }

            return true;
        }

        private async Task<Result<(TezosTransaction tx, bool isSuccess, bool isRunSuccess)>> RunAutofillOperation(
            string delegateAddress,
            string bakerAddress,
            CancellationToken cancellationToken)
        {
            try
            {
                var tx = new TezosTransaction
                {
                    StorageLimit = _tezosConfig.StorageLimit,
                    GasLimit = _tezosConfig.GasLimit,
                    From = delegateAddress,
                    Fee = 0,
                    Currency = _tezosConfig.Name,
                    CreationTime = DateTime.UtcNow,

                    UseRun = true,
                    UseOfflineCounter = false,
                    OperationType = OperationType.Delegation
                };

                if (bakerAddress != null)
                    tx.To = bakerAddress;

                var walletAddress = _app.Account
                    .GetCurrencyAccount(TezosConfig.Xtz)
                    .GetAddressAsync(delegateAddress, cancellationToken)
                    .WaitForResult();

                using var securePublicKey = _app.Account.Wallet.GetPublicKey(
                    currency: _tezosConfig,
                    keyIndex: walletAddress.KeyIndex,
                    keyType: walletAddress.KeyType);

                var (isSuccess, isRunSuccess, _) = await tx.FillOperationsAsync(
                    securePublicKey: securePublicKey,
                    tezosConfig: _tezosConfig,
                    headOffset: TezosConfig.HeadOffset,
                    cancellationToken: cancellationToken);

                return (tx, isSuccess, isRunSuccess);
            }
            catch (Exception e)
            {
                Log.Error(e, "Autofill transaction error");
                return new Error(Errors.TransactionCreationError, "Autofill transaction error. Try again later.");
            }
        }

        private void SubscribeToServices()
        {
            if (!_app.HasQuotesProvider) return;

            _app.QuotesProvider.QuotesUpdated += OnQuotesUpdatedEventHandler;
            _app.QuotesProvider.AvailabilityChanged += OnQuotesProviderAvailabilityChangedEventHandler;
        }

        private void OnQuotesUpdatedEventHandler(object sender, EventArgs args)
        {
            if (sender is not ICurrencyQuotesProvider quotesProvider)
                return;

            var quote = quotesProvider.GetQuote(FeeCurrencyCode, BaseCurrencyCode);

            if (quote != null)
            {
                FeeInBase = Fee.SafeMultiply(quote.Bid);
                DelegateAddressBalanceInBase = DelegateAddressBalance.SafeMultiply(quote.Bid);
            }
        }

        private void OnQuotesProviderAvailabilityChangedEventHandler(object sender, EventArgs args)
        {
            if (sender is not ICurrencyQuotesProvider provider)
                return;

            if (provider.IsAvailable)
                _ = LoadBakerList();
        }

        protected void ShowMessage(MessageType messageType, string text)
        {
            Message = new Message();
            Message.Type = messageType;
            Message.Text = text;

            this.RaisePropertyChanged(nameof(Message));
        }
    }
}

