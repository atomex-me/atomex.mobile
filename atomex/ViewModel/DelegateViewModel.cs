using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using atomex.Resources;
using Atomex;
using Atomex.Blockchain.Tezos;
using Atomex.Blockchain.Tezos.Internal;
using Atomex.Common;
using Atomex.Core;
using Atomex.MarketData.Abstract;
using Atomex.Wallet;
using Atomex.Wallet.Tezos;
using Newtonsoft.Json.Linq;
using Serilog;
using Xamarin.Essentials;
using Xamarin.Forms;

namespace atomex.ViewModel
{
    public class DelegateViewModel : BaseViewModel
    {
        private IAtomexApp AtomexApp { get; }

        public INavigation Navigation { get; set; }

        private readonly TezosConfig _tezosConfig; 

        private bool _canDelegate;
        public bool CanDelegate
        {
            get => _canDelegate;
            set { _canDelegate = value; OnPropertyChanged(nameof(CanDelegate)); }
        }

        private List<DelegationViewModel> _delegations;
        public List<DelegationViewModel> Delegations
        {
            get => _delegations;
            set { _delegations = value; OnPropertyChanged(nameof(Delegations)); }
        }

        private bool _isLoadingDelegations;
        public bool IsLoadingDelegations
        {
            get => _isLoadingDelegations;
            set { _isLoadingDelegations = value; OnPropertyChanged(nameof(IsLoadingDelegations)); }
        }

        private bool _isFeeCalculation = false;
        public bool IsFeeCalculation
        {
            get => _isFeeCalculation;
            set { _isFeeCalculation = value; OnPropertyChanged(nameof(IsFeeCalculation)); }
        }

        private float _opacity = 1f;
        public float Opacity
        {
            get => _opacity;
            set { _opacity = value; OnPropertyChanged(nameof(Opacity)); }
        }

        private bool _isLoading = false;
        public bool IsLoading
        {
            get => _isLoading;
            set
            {
                if (_isLoading == value)
                    return;
                
                _isLoading = value;

                if (_isLoading)
                    Opacity = 0.3f;
                else
                    Opacity = 1f;

                OnPropertyChanged(nameof(IsLoading));
            }
        }

        private WalletAddressViewModel _walletAddressViewModel;
        public WalletAddressViewModel WalletAddressViewModel
        {
            get => _walletAddressViewModel;
            set
            {
                _walletAddressViewModel = value;
                OnPropertyChanged(nameof(WalletAddressViewModel));
            }
        }

        private List<BakerViewModel> _fromBakersList;
        public List<BakerViewModel> FromBakersList
        {
            get => _fromBakersList;
            private set
            {
                _fromBakersList = value;
                OnPropertyChanged(nameof(FromBakersList));

                FoundBakersList = FromBakersList;
                BakerViewModel = FromBakersList.FirstOrDefault();
            }
        }

        
        private List<BakerViewModel> _foundBakersList;
        public List<BakerViewModel> FoundBakersList
        {
            get => _foundBakersList;
            private set
            {
                _foundBakersList = value;
                OnPropertyChanged(nameof(FoundBakersList));
            }
        }

        private List<WalletAddressViewModel> _fromAddressList;
        public List<WalletAddressViewModel> FromAddressList
        {
            get => _fromAddressList;
            private set
            {
                _fromAddressList = value;
                OnPropertyChanged(nameof(FromAddressList));

                WalletAddressViewModel = FromAddressList.FirstOrDefault();
            }
        }

        private BakerViewModel _bakerViewModel;
        public BakerViewModel BakerViewModel
        {
            get => _bakerViewModel;
            set
            {
                if (_bakerViewModel == value)
                    return;

                _bakerViewModel = value;
                OnPropertyChanged(nameof(BakerViewModel));

                if (_bakerViewModel == FromBakersList.First())
                {
                    Address = null;
                    return;
                }
                if (_bakerViewModel != null)
                    Address = _bakerViewModel.Address;
            }
        }

        private decimal _fee;
        public decimal Fee
        {
            get => _fee;
            set
            {
                _fee = value;

                if (!UseDefaultFee)
                {
                    var feeAmount = _fee;

                    if (feeAmount > _walletAddressViewModel?.AvailableBalance)
                    {
                        feeAmount = (decimal)_walletAddressViewModel?.AvailableBalance;
                        _fee = feeAmount;
                    }
                }

                OnPropertyChanged(nameof(Fee));
                OnPropertyChanged(nameof(FeeString));
                OnQuotesUpdatedEventHandler(AtomexApp.QuotesProvider, EventArgs.Empty);
            }
        }

        public string FeeString
        {
            get => Fee.ToString(CultureInfo.InvariantCulture);
            set
            {
                string temp = value.Replace(",", ".");
                if (!decimal.TryParse(temp, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var fee))
                    return;

                Fee = fee;
            }
        }

        private string _baseCurrencyFormat;
        public string BaseCurrencyFormat
        {
            get => _baseCurrencyFormat;
            set { _baseCurrencyFormat = value; OnPropertyChanged(nameof(BaseCurrencyFormat)); }
        }

        private decimal _feeInBase;
        public decimal FeeInBase
        {
            get => _feeInBase;
            set { _feeInBase = value; OnPropertyChanged(nameof(FeeInBase)); }
        }

        private string _feeCurrencyCode;
        public string FeeCurrencyCode
        {
            get => _feeCurrencyCode;
            set { _feeCurrencyCode = value; OnPropertyChanged(nameof(FeeCurrencyCode)); }
        }

        private string _baseCurrencyCode;
        public string BaseCurrencyCode
        {
            get => _baseCurrencyCode;
            set { _baseCurrencyCode = value; OnPropertyChanged(nameof(BaseCurrencyCode)); }
        }

        private bool _useDefaultFee;
        public bool UseDefaultFee
        {
            get => _useDefaultFee;
            set
            {
                _useDefaultFee = value;
                OnPropertyChanged(nameof(UseDefaultFee));
            }
        }

        private string _address;
        public string Address
        {
            get => _address;
            set
            {
                _address = value;
                OnPropertyChanged(nameof(Address));

                if (_address != null)
                {
                    var baker = FromBakersList.FirstOrDefault(b => b.Address == _address);

                    if (baker == null)
                        BakerViewModel = FromBakersList.First();
                    else if (baker != BakerViewModel)
                        BakerViewModel = baker;
                }
            }
        }

        public void SetWalletAddress(string address)
        {
            if (address != null)
            {
                var walletAddressViewModel = FromAddressList.FirstOrDefault(vm => vm.Address == address);

                if (walletAddressViewModel == null)
                    WalletAddressViewModel = FromAddressList.First();
                else
                    WalletAddressViewModel = walletAddressViewModel;
            }
        }

        private ICommand _validateCommand;
        public ICommand ValidateCommand => _validateCommand ??= new Command(async () => await Validate());

        private async Task Validate()
        {
            try
            {
                if (string.IsNullOrEmpty(Address))
                {
                    await Application.Current.MainPage.DisplayAlert(AppResources.Error, AppResources.EmptyAddressError, AppResources.AcceptButton);
                    return;
                }
                if (!_tezosConfig.IsValidAddress(Address))
                {
                    await Application.Current.MainPage.DisplayAlert(AppResources.Error, AppResources.InvalidAddressError, AppResources.AcceptButton);
                    return;
                }
                if (Fee <= 0)
                {
                    await Application.Current.MainPage.DisplayAlert(AppResources.Error, AppResources.CommissionLessThanZeroError, AppResources.AcceptButton);
                    return;
                }

                var result = await GetDelegate();

                if (result.HasError)
                {
                    await Application.Current.MainPage.DisplayAlert(AppResources.Error, result.Error.Description, AppResources.AcceptButton);
                    return;
                }

                if (BakerViewModel.IsFull)
                {
                    var res = await Application.Current.MainPage.DisplayAlert(AppResources.Warning, AppResources.BakerIsOverdelegatedWarning, AppResources.AcceptButton, AppResources.CancelButton);
                    if (!res) return;
                }

                if (BakerViewModel.MinDelegation > WalletAddressViewModel.AvailableBalance)
                {
                    var res = await Application.Current.MainPage.DisplayAlert(AppResources.Warning, AppResources.DelegationLimitWarning, AppResources.AcceptButton, AppResources.CancelButton);
                    if (!res) return;
                }

                await Navigation.PushAsync(new DelegationConfirmationPage(this));
            }
            catch (Exception e)
            {
                await Application.Current.MainPage.DisplayAlert(AppResources.Error, AppResources.DelegationValidationError, AppResources.AcceptButton);
                Log.Error(e, "Delegation validation error");
            }
        }

        public DelegateViewModel(IAtomexApp app, INavigation navigation)
        {
            AtomexApp = app ?? throw new ArgumentNullException(nameof(AtomexApp));
            Navigation = navigation ?? throw new ArgumentNullException(nameof(Navigation)); ;

            _tezosConfig = AtomexApp.Account.Currencies.Get<TezosConfig>("XTZ");
            FeeCurrencyCode = _tezosConfig.FeeCode;
            BaseCurrencyCode = "USD";
            BaseCurrencyFormat = "$0.00";
            UseDefaultFee = true;
            IsLoadingDelegations = true;
            CanDelegate = false;

            SubscribeToServices();
            _ = LoadDelegationInfoAsync();
            _ = LoadBakerList();
            PrepareWallet().WaitForResult();
        }

        private async Task LoadBakerList()
        {
            List<BakerViewModel> bakers = null;

            try
            {
                await Task.Run(async () =>
                {
                    bakers = (await BbApi
                        .GetBakers(AtomexApp.Account.Network)
                        .ConfigureAwait(false))
                        .Select(x => new BakerViewModel
                        {
                            Address = x.Address,
                            Logo = x.Logo,
                            Name = x.Name,
                            Fee = x.Fee,
                            MinDelegation = x.MinDelegation,
                            StakingAvailable = x.StakingAvailable,
                            EstimatedRoi = x.EstimatedRoi
                        })
                        .ToList();
                    bakers.Insert(0, new BakerViewModel
                    {
                        Name = "Custom Baker",
                        Logo = "CustomBakerLogo"
                    });
                });
            }
            catch (Exception e)
            {
                Log.Error(e.Message, "Error while fetching bakers list");
            }

            await Device.InvokeOnMainThreadAsync(() =>
            {
                FromBakersList = bakers;
            });
        }

        private async Task PrepareWallet(CancellationToken cancellationToken = default)
        {
            FromAddressList = (await AtomexApp.Account
                .GetUnspentAddressesAsync(_tezosConfig.Name, cancellationToken).ConfigureAwait(false))
                .OrderByDescending(x => x.Balance)
                .Select(w => new WalletAddressViewModel
                {
                    Address = w.Address,
                    AvailableBalance = w.AvailableBalance(),
                    CurrencyCode = _tezosConfig.Name,
                    CurrencyFormat = _tezosConfig.Format
                })
                .ToList();

            if (!FromAddressList?.Any() ?? false)
            {
                //Warning = "You don't have non-empty accounts";
                return;
            }

            WalletAddressViewModel = FromAddressList.FirstOrDefault();
        }

        private ICommand _setBakerAddressCommand;
        public ICommand SetBakerAddressCommand => _setBakerAddressCommand ??= new Command<string>((name) => SetBakerAddress(name));

        private void SetBakerAddress(string address)
        {
            Address = address;
        }

        private ICommand _showBakersCommand;
        public ICommand ShowBakersCommand => _showBakersCommand ??= new Command(async () => await ShowBakersList());

        private async Task ShowBakersList()
        {
            var bakersListPage = new BakerListPage(this);

            OnBakerSelected = async (selected) =>
            {
                BakerViewModel = selected;
                Address = selected.Address;
                await GetDelegate();
            };


            await Navigation.PushAsync(bakersListPage);
        }

        public Action<BakerViewModel> OnBakerSelected;

        private ICommand _selectBakerCommand;
        public ICommand SelectBakerCommand => _selectBakerCommand ??= new Command<BakerViewModel>(async (item) => await BakerSelected(item));

        private async Task BakerSelected(BakerViewModel baker)
        {
            if (baker == null)
                return;

            OnBakerSelected.Invoke(baker);

            await Navigation.PopAsync();
        }
        private ICommand _searchBakersCommand;
        public ICommand SearchBakersCommand => _searchBakersCommand ??= new Command<string>((value) => OnSearchBarTextChanged(value));

        private void OnSearchBarTextChanged(string value)
        {
            try
            {
                if (string.IsNullOrEmpty(value))
                    FoundBakersList = FromBakersList;
                else
                    FoundBakersList = FromBakersList.Where(x => x.Name.ToLower().Contains(value.ToLower())).ToList();
            }
            catch (Exception e)
            {
                Log.Error(e.Message);
            }
        }

        private ICommand _getDelegateCommand;
        public ICommand GetDelegateCommand => _getDelegateCommand ??= new Command(async () => await GetDelegate());

        private async Task<Result<string>> GetDelegate(
            CancellationToken cancellationToken = default)
        {

            if (_walletAddressViewModel.Address == null)
                return new Error(Errors.InvalidWallets, "You don't have non-empty accounts");

            JObject delegateData;

            try
            {
                IsLoading = true;

                var rpc = new Rpc(_tezosConfig.RpcNodeUri);

                delegateData = await rpc
                    .GetDelegate(_address)
                    .ConfigureAwait(false);
            }
            catch
            {
                IsLoading = false;
                return new Error(Errors.WrongDelegationAddress, AppResources.WrongDelegationAddressError);
            }

            if (delegateData["deactivated"].Value<bool>())
            {
                IsLoading = false;
                return new Error(Errors.WrongDelegationAddress, AppResources.BakerIsDeactivated);
            }    

            var delegators = delegateData["delegated_contracts"]?.Values<string>();

            if (delegators.Contains(WalletAddressViewModel.Address))
            {
                IsLoading = false;
                return new Error(Errors.AlreadyDelegated, $"{AppResources.AlreadyDelegatedFrom} {WalletAddressViewModel.Address} {AppResources.ToLabel} {_address}");
            }

            try
            {
                var tx = new TezosTransaction
                {
                    StorageLimit = _tezosConfig.StorageLimit,
                    GasLimit = _tezosConfig.GasLimit,
                    From = WalletAddressViewModel.Address,
                    To = _address,
                    Fee = 0, //Fee.ToMicroTez(),
                    Currency = _tezosConfig.Name,
                    CreationTime = DateTime.UtcNow,

                    UseRun = true,
                    UseOfflineCounter = false,
                    OperationType = OperationType.Delegation
                };

                var walletAddress = AtomexApp.Account
                    .GetCurrencyAccount(TezosConfig.Xtz)
                    .GetAddressAsync(WalletAddressViewModel.Address)
                    .WaitForResult();

                using var securePublicKey = AtomexApp.Account.Wallet.GetPublicKey(
                    currency: _tezosConfig,
                    keyIndex: walletAddress.KeyIndex,
                    keyType: walletAddress.KeyType);

                var (isSuccess, isRunSuccess) = await tx.FillOperationsAsync(
                    securePublicKey: securePublicKey,
                    tezosConfig: _tezosConfig,
                    headOffset: TezosConfig.HeadOffset,
                    cancellationToken: cancellationToken);

                if (!isSuccess)
                {
                    IsLoading = false;
                    return new Error(Errors.TransactionCreationError, AppResources.AutofillTransactionFailed);
                }

                IsLoading = false;

                if (isRunSuccess)
                {
                    Fee = tx.Fee;
                    OnPropertyChanged(nameof(FeeString));

                     if (Fee > WalletAddressViewModel.AvailableBalance)
                        return new Error(Errors.InsufficientAmount, AppResources.InsufficientFunds);
                }
                else
                {
                    return new Error(Errors.TransactionCreationError, AppResources.AutofillTransactionFailed);
                }
            }
            catch (Exception e)
            {
                Log.Error(e, "Autofill delegation error");
                IsLoading = false;
                return new Error(Errors.TransactionCreationError, AppResources.AutofillTransactionFailed);
            }
            return AppResources.SuccessfulCheck;
        }

        private async Task<Result<string>> Delegate()
        {
            var wallet = (HdWallet)AtomexApp.Account.Wallet;
            var keyStorage = wallet.KeyStorage;
            var tezos = _tezosConfig;

            var tezosAccount = AtomexApp.Account
                .GetCurrencyAccount<TezosAccount>("XTZ");

            try
            {
                await tezosAccount.AddressLocker
                    .LockAsync(WalletAddressViewModel.Address);

                var tx = new TezosTransaction
                {
                    StorageLimit = _tezosConfig.StorageLimit,
                    GasLimit = _tezosConfig.GasLimit,
                    From = WalletAddressViewModel.Address,
                    To = _address,
                    Fee = Fee.ToMicroTez(),
                    Currency = _tezosConfig.Name,
                    CreationTime = DateTime.UtcNow,

                    UseRun = true,
                    UseOfflineCounter = true,
                    OperationType = OperationType.Delegation
                };

                var walletAddress = AtomexApp.Account
                    .GetCurrencyAccount(TezosConfig.Xtz)
                    .GetAddressAsync(WalletAddressViewModel.Address)
                    .WaitForResult();

                using var securePublicKey = AtomexApp.Account.Wallet.GetPublicKey(
                    currency: _tezosConfig,
                    keyIndex: walletAddress.KeyIndex,
                    keyType: walletAddress.KeyType);

                await tx.FillOperationsAsync(
                    securePublicKey: securePublicKey,
                    tezosConfig: _tezosConfig,
                    headOffset: TezosConfig.HeadOffset);

                var signResult = await tx
                    .SignAsync(keyStorage, walletAddress, _tezosConfig);

                if (!signResult)
                {
                    Log.Error("Transaction signing error");
                    return new Error(Errors.TransactionSigningError, "Transaction signing error");
                }

                var result = await tezos.BlockchainApi
                    .TryBroadcastAsync(tx);

                return result;
            }
            catch (Exception e)
            {
                Log.Error(e, "Delegation send error");
                return new Error(Errors.TransactionCreationError, AppResources.DelegationError);
            }
            finally
            {
                tezosAccount.AddressLocker.Unlock(WalletAddressViewModel.Address);
            }
        }

        private ICommand _delegateCommand;
        public ICommand DelegateCommand => _delegateCommand ??= new Command(async () => await OnDelegateButtonClicked());

        private async Task OnDelegateButtonClicked()
        {
            try
            {
                IsLoading = true;
                var result = await Delegate();
                if (result.Error != null)
                {
                    IsLoading = false;
                    await Application.Current.MainPage.DisplayAlert(AppResources.Error, result.Error.Description, AppResources.AcceptButton);
                    return;
                }
                await LoadDelegationInfoAsync();
                var res = await Application.Current.MainPage.DisplayAlert(AppResources.SuccessDelegation, AppResources.DelegationListWillBeUpdated + "\r\n" + AppResources.ExplorerUri + ": " + result.Value, null, AppResources.CopyAndExitButton);
                if (!res)
                {
                    await Clipboard.SetTextAsync(result.Value);
                    for (var i = 1; i < 3; i++)
                    {
                        Navigation.RemovePage(Navigation.NavigationStack[Navigation.NavigationStack.Count - 2]);
                    }
                    await Navigation.PopAsync();
                }
            }
            catch (Exception e)
            {
                IsLoading = false;
                await Application.Current.MainPage.DisplayAlert(AppResources.Error, AppResources.DelegationError, AppResources.AcceptButton);
                Log.Error(e, "Delegation error");
            }
        }


        private async Task LoadDelegationInfoAsync()
        {
            try
            {
                var balance = await AtomexApp.Account
                    .GetBalanceAsync(_tezosConfig.Name)
                    .ConfigureAwait(false);

                var addresses = await AtomexApp.Account
                    .GetUnspentAddressesAsync(_tezosConfig.Name)
                    .ConfigureAwait(false);

                var delegations = new List<DelegationViewModel>();

                var tzktApi = new TzktApi(_tezosConfig);

                var head = await tzktApi.GetHeadLevelAsync();

                var headLevel = head.Value;

                decimal currentCycle = AtomexApp.Account.Network == Network.MainNet ?
                    Math.Floor((headLevel - 1) / 4096) :
                    Math.Floor((headLevel - 1) / 2048);

                foreach (var wa in addresses)
                {
                    var account = await tzktApi.GetAccountByAddressAsync(wa.Address);

                    if (account == null || account.HasError)
                    {
                        delegations.Add(new DelegationViewModel(this, Navigation)
                        {
                            Address = wa.Address,
                            Balance = wa.Balance
                        });
                        continue;
                    }

                    var baker = await BbApi
                        .GetBaker(account.Value.DelegateAddress, AtomexApp.Account.Network)
                        .ConfigureAwait(false);

                    decimal txCycle = AtomexApp.Account.Network == Network.MainNet ?
                        Math.Floor((account.Value.DelegationLevel - 1) / 4096) :
                        Math.Floor((account.Value.DelegationLevel - 1) / 2048);

                    delegations.Add(new DelegationViewModel(this, Navigation)
                    {
                        Baker = baker,
                        Address = wa.Address,
                        Balance = wa.Balance,
                        BbUri = _tezosConfig.BbUri,
                        DelegationTime = account.Value.DelegationTime,
                        Status = currentCycle - txCycle < 2 ? "Pending" :
                            currentCycle - txCycle < 7 ? "Confirmed" :
                            "Active"
                    });
                }

                await Device.InvokeOnMainThreadAsync(() =>
                {
                    IsLoadingDelegations = false;
                    Delegations = delegations;
                    CanDelegate = Delegations.Count > 0; 
                });
            }
            catch (Exception e)
            {
                Log.Error(e, "LoadDelegationInfoAsync error");
            }
        }

        private void SubscribeToServices()
        {
            if (AtomexApp.HasQuotesProvider)
                AtomexApp.QuotesProvider.QuotesUpdated += OnQuotesUpdatedEventHandler;
        }

        private void OnQuotesUpdatedEventHandler(object sender, EventArgs args)
        {
            if (!(sender is ICurrencyQuotesProvider quotesProvider))
                return;

            var quote = quotesProvider.GetQuote(FeeCurrencyCode, BaseCurrencyCode);

            if (quote != null)
                FeeInBase = Fee * quote.Bid;
        }

        private ICommand _selectDelegationCommand;
        public ICommand SelectDelegationCommand => _selectDelegationCommand ??= new Command<DelegationViewModel>(async (value) => await OnDelegationTapped(value));

        private async Task OnDelegationTapped(DelegationViewModel delegation)
        {
            if (delegation.Baker == null)
                await Navigation.PushAsync(new DelegatePage(this));
            else
                await Navigation.PushAsync(new DelegationInfoPage(delegation));
        }
    }
}

