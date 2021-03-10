using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using atomex.Models;
using atomex.Resources;
using Atomex;
using Atomex.Blockchain.Tezos;
using Atomex.Blockchain.Tezos.Internal;
using Atomex.Common;
using Atomex.Core;
using Atomex.MarketData.Abstract;
using Atomex.Wallet;
using Newtonsoft.Json.Linq;
using Serilog;
using Xamarin.Forms;

namespace atomex.ViewModel
{
    public class DelegateViewModel : BaseViewModel
    {
        private IAtomexApp AtomexApp { get; }

        private readonly Tezos _tezos;
        private WalletAddressViewModel _walletAddressViewModel;
        private TezosTransaction _tx;


        private bool _canDelegate;
        public bool CanDelegate
        {
            get => _canDelegate;
            set { _canDelegate = value; OnPropertyChanged(nameof(CanDelegate)); }
        }

        private List<Delegation> _delegations;
        public List<Delegation> Delegations
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

                BakerViewModel = FromBakersList.FirstOrDefault();
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

        private decimal _minFee;

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

                    if (feeAmount > _walletAddressViewModel?.WalletAddress?.Balance)
                    {
                        feeAmount = (decimal)_walletAddressViewModel?.WalletAddress?.Balance;
                        _fee = feeAmount;
                    }

                    if (feeAmount < _minFee)
                        _fee = _minFee;
                }

                OnPropertyChanged(nameof(Fee));
                OnQuotesUpdatedEventHandler(AtomexApp.QuotesProvider, EventArgs.Empty);
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
                if (_useDefaultFee)
                    Fee = _minFee;
                OnPropertyChanged(nameof(Fee));
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

        public async Task<string> Validate()
        {
            if (string.IsNullOrEmpty(Address))
            {
                return AppResources.EmptyAddressError;
            }

            if (!_tezos.IsValidAddress(Address))
            {
                return AppResources.InvalidAddressError;
            }

            if (Fee < _minFee)
            {
                return AppResources.CommissionLessThanZeroError;
            }

            var result = await GetDelegate();

            if (result.HasError)
            {
                return result.Error.Description;
            }

            return null;
        }

        public DelegateViewModel(IAtomexApp app)
        {
            AtomexApp = app ?? throw new ArgumentNullException(nameof(AtomexApp));

            _tezos = AtomexApp.Account.Currencies.Get<Tezos>("XTZ");
            FeeCurrencyCode = _tezos.FeeCode;
            BaseCurrencyCode = "USD";
            BaseCurrencyFormat = "$0.00";
            _minFee = 0.001258M;
            UseDefaultFee = true;
            IsLoadingDelegations = true;
            CanDelegate = false;

            SubscribeToServices();
            LoadDelegationInfoAsync().FireAndForget();
            LoadBakerList().FireAndForget();
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
                .GetUnspentAddressesAsync(_tezos.Name, cancellationToken).ConfigureAwait(false))
                .OrderByDescending(x => x.Balance)
                .Select(w => new WalletAddressViewModel(w, _tezos.Format))
                .ToList();

            if (!FromAddressList?.Any() ?? false)
            {
                //Warning = "You don't have non-empty accounts";
                return;
            }

            WalletAddressViewModel = FromAddressList.FirstOrDefault();
        }

        private async Task<Result<string>> GetDelegate(
            CancellationToken cancellationToken = default)
        {
            if (_walletAddressViewModel.WalletAddress == null)
                return new Error(Errors.InvalidWallets, AppResources.DelegationValidationError);

            var wallet = (HdWallet)AtomexApp.Account.Wallet;
            var keyStorage = wallet.KeyStorage;
            var rpc = new Rpc(_tezos.RpcNodeUri);

            JObject delegateData;
            try
            {
                delegateData = await rpc
                    .GetDelegate(_address)
                    .ConfigureAwait(false);
            }
            catch
            {
                return new Error(Errors.WrongDelegationAddress, AppResources.WrongDelegationAddressError);
            }

            if (delegateData["deactivated"].Value<bool>())
                return new Error(Errors.WrongDelegationAddress, AppResources.BakerIsDeactivated);

            var delegators = delegateData["delegated_contracts"]?.Values<string>();

            if (delegators.Contains(WalletAddressViewModel.WalletAddress.Address))
                return new Error(Errors.AlreadyDelegated, $"{AppResources.AlreadyDelegatedFrom} {WalletAddressViewModel.WalletAddress.Address} {AppResources.ToLabel} {_address}");

            var tx = new TezosTransaction
            {
                StorageLimit = _tezos.StorageLimit,
                GasLimit = _tezos.GasLimit,
                From = WalletAddressViewModel.WalletAddress.Address,
                To = _address,
                Fee = Fee.ToMicroTez(),
                Currency = _tezos,
                CreationTime = DateTime.UtcNow,
            };

            try
            {
                var calculatedFee = await tx.AutoFillAsync(keyStorage, WalletAddressViewModel.WalletAddress, UseDefaultFee);
                if (!calculatedFee)
                    return new Error(Errors.TransactionCreationError, AppResources.AutofillTransactionFailed);

                Fee = tx.Fee;
                _tx = tx;

            }
            catch (Exception e)
            {
                Log.Error(e, "Autofill delegation error");
                return new Error(Errors.TransactionCreationError, AppResources.AutofillTransactionFailed);
            }

            return AppResources.SuccessfulCheck;
        }

        public async Task<Result<string>> Delegate()
        {
            var wallet = (HdWallet)AtomexApp.Account.Wallet;
            var keyStorage = wallet.KeyStorage;

            try
            {
                var signResult = await _tx.SignDelegationOperationAsync(keyStorage, WalletAddressViewModel.WalletAddress, default);

                if (!signResult)
                {
                    Log.Error("Transaction signing error");
                }
                var result = await _tezos.BlockchainApi.TryBroadcastAsync(_tx);
                return result;
            }
            catch (Exception e)
            {
                Log.Error(e, "delegation send error.");
                return AppResources.DelegationError;
            }
        }

        public async Task LoadDelegationInfoAsync()
        {
            try
            {
                var balance = await AtomexApp.Account
                    .GetBalanceAsync(_tezos.Name)
                    .ConfigureAwait(false);

                var addresses = await AtomexApp.Account
                    .GetUnspentAddressesAsync(_tezos.Name)
                    .ConfigureAwait(false);

                var delegations = new List<Delegation>();

                var tzktApi = new TzktApi(_tezos);

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
                        delegations.Add(new Delegation
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

                    delegations.Add(new Delegation
                    {
                        Baker = baker,
                        Address = wa.Address,
                        Balance = wa.Balance,
                        BbUri = _tezos.BbUri,
                        DelegationTime = account.Value.DelegationTime,
                        Status = currentCycle - txCycle < 2 ? "Pending" :
                            currentCycle - txCycle < 7 ? "Confirmed" :
                            "Active"
                    });
                }

                //BakerData test = new BakerData()
                //{
                //    Logo = "https://api.baking-bad.org/logos/tezoshodl.png",
                //    Name = "TezosHODL",
                //    Address = "tz1sdfldjsflksjdlkf123sfa",
                //    Fee = 5,
                //    MinDelegation = 10,
                //    StakingAvailable = 10000.000000m
                //};
                //delegations.Add(new Delegation
                //{
                //    Baker = test,
                //    Address = "KT1Db2f2vWPQKFk6jbh4dCZJ1CkWWBvM6YMX",
                //    Balance = 67.7m,
                //    BbUri = "https://baking-bad.org/",
                //    Status = "Active"
                //});

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
    }
}

