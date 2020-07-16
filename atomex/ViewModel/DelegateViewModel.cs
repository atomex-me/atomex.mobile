using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
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
    public class Delegation
    {
        public BakerData Baker { get; set; }
        public string Address { get; set; }
        public decimal Balance { get; set; }
        public string TxExplorerUri { get; set; }
    }

    public class DelegateViewModel : BaseViewModel
    {
        private IAtomexApp App { get; }

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

        private bool _loadingDelegations;
        public bool LoadingDelegations
        {
            get => _loadingDelegations;
            set { _loadingDelegations = value; OnPropertyChanged(nameof(LoadingDelegations)); }
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
                OnQuotesUpdatedEventHandler(App.QuotesProvider, EventArgs.Empty);
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


        //public DelegateViewModel(IAtomexApp app)
        //{
        //    FromBakersList = new List<BakerViewModel>()
        //    {
        //        new BakerViewModel()
        //        {
        //            Logo = "https://api.baking-bad.org/logos/tezoshodl.png",
        //            Name = "TezosHODL",
        //            Address = "tz1sdfldjsflksjdlkf123sfa",
        //            Fee = 5,
        //            MinDelegation = 10,
        //            StakingAvailable = 10000.000000m
        //        },
        //        new BakerViewModel()
        //        {
        //            Logo = "XTZ",
        //            Name = "Tezgate",
        //            Address = "tz1VxS7ff4YnZRs8b4mMP4WaMVpoQjuo1rjf",
        //            Fee = 9.90m,
        //            MinDelegation = 100,
        //            StakingAvailable = 1356622.776437m
        //        }
        //    };
        //    App = app ?? throw new ArgumentNullException(nameof(app));
        //    _tezos = App.Account.Currencies.Get<Tezos>("XTZ");
        //    FeeCurrencyCode = _tezos.FeeCode;
        //    BaseCurrencyCode = "USD";
        //    BaseCurrencyFormat = "$0.00";
        //    UseDefaultFee = true;
        //    LoadDelegationInfoAsync().FireAndForget();
        //    PrepareWallet().WaitForResult();
        //}

        public DelegateViewModel(IAtomexApp app)
        {
            App = app ?? throw new ArgumentNullException(nameof(app));

            _tezos = App.Account.Currencies.Get<Tezos>("XTZ");
            FeeCurrencyCode = _tezos.FeeCode;
            BaseCurrencyCode = "USD";
            BaseCurrencyFormat = "$0.00";
            _minFee = 0.001258M;
            UseDefaultFee = true;
            LoadingDelegations = true;

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
                        .GetBakers(App.Account.Network)
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
            FromAddressList = (await App.Account
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
                return new Error(Errors.InvalidWallets, AppResources.DontHaveNonEmptyAccountsError);

            var wallet = (HdWallet)App.Account.Wallet;
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
                return new Error(Errors.AlreadyDelegated, $"{AppResources.AlreadyDelegatedFrom} {WalletAddressViewModel.WalletAddress.Address} {AppResources.to} {_address}");

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
            var wallet = (HdWallet)App.Account.Wallet;
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
                var balance = await App.Account
                    .GetBalanceAsync(_tezos.Name)
                    .ConfigureAwait(false);

                var addresses = await App.Account
                    .GetUnspentAddressesAsync(_tezos.Name)
                    .ConfigureAwait(false);

                var rpc = new Rpc(_tezos.RpcNodeUri);

                var delegations = new List<Delegation>();

                foreach (var wa in addresses)
                {
                    var accountData = await rpc
                        .GetAccount(wa.Address)
                        .ConfigureAwait(false);

                    var @delegate = accountData["delegate"]?.ToString();

                    if (string.IsNullOrEmpty(@delegate))
                        continue;


                    var baker = await BbApi
                        .GetBaker(@delegate, App.Account.Network)
                        .ConfigureAwait(false);

                    delegations.Add(new Delegation
                    {
                        Baker = baker,
                        Address = wa.Address,
                        Balance = wa.Balance,
                        TxExplorerUri = _tezos.TxExplorerUri + wa.Address
                    });
                }

                await Device.InvokeOnMainThreadAsync(() =>
                {
                    CanDelegate = balance.Available > 0;
                    LoadingDelegations = false;
                    Delegations = delegations;
                });
            }
            catch (Exception e)
            {
                Log.Error(e, "LoadDelegationInfoAsync error");
            }
        }

        private void SubscribeToServices()
        {
            if (App.HasQuotesProvider)
                App.QuotesProvider.QuotesUpdated += OnQuotesUpdatedEventHandler;
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

