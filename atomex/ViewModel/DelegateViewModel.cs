
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Atomex;
using Atomex.Blockchain.Tezos;
using Atomex.Blockchain.Tezos.Internal;
using Atomex.Common;
using Atomex.Core;
using Atomex.MarketData.Abstract;
using Atomex.Wallet;
using Newtonsoft.Json.Linq;
using Serilog;

namespace atomex.ViewModel
{
    public class DelegateViewModel : BaseViewModel
    {
        private IAtomexApp App { get; }

        private readonly Tezos _tezos;
        private WalletAddressViewModel _walletAddressViewModel;
        private TezosTransaction _tx;

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
                _bakerViewModel = value;
                OnPropertyChanged(nameof(BakerViewModel));

                if (_bakerViewModel != null)
                    Address = _bakerViewModel.Address;
            }
        }

        public string FeeString
        {
            get => Fee.ToString(_tezos.FeeFormat, CultureInfo.InvariantCulture);
            set
            {
                //if (!decimal.TryParse(value, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var fee))
                //    return;

                //Fee = fee.TruncateByFormat(_tezos.FeeFormat);
            }
        }

        private decimal _fee;
        public decimal Fee
        {
            get => _fee;
            set
            {
                _fee = value;

                //if (!UseDefaultFee)
                //{
                //    var feeAmount = _fee;

                //    if (feeAmount > _walletAddress.Balance)
                //        feeAmount = _walletAddress.Balance;

                //    _fee = feeAmount;

                //    OnPropertyChanged(nameof(FeeString));
                //    Warning = string.Empty;
                //}

                //OnQuotesUpdatedEventHandler(App.QuotesProvider, EventArgs.Empty);
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

                var baker = FromBakersList.FirstOrDefault(b => b.Address == _address);

                if (baker == null)
                    BakerViewModel = null;
                else if (baker != BakerViewModel)
                    BakerViewModel = baker;
            }
        }

        private string _warning;
        public string Warning
        {
            get => _warning;
            set { _warning = value; OnPropertyChanged(nameof(Warning)); }
        }

        private bool _delegationCheck;
        public bool DelegationCheck
        {
            get => _delegationCheck;
            set { _delegationCheck = value; OnPropertyChanged(nameof(DelegationCheck)); }
        }

        //    private ICommand _nextCommand;
        //    public ICommand NextCommand => _nextCommand ?? (_nextCommand = new Command(async () =>
        //    {
        //        if (DelegationCheck)
        //            return;

        //        DelegationCheck = true;

        //        try
        //        {
        //            if (string.IsNullOrEmpty(Address))
        //            {
        //                Warning = Resources.SvEmptyAddressError;
        //                return;
        //            }

        //            if (!_tezos.IsValidAddress(Address))
        //            {
        //                Warning = Resources.SvInvalidAddressError;
        //                return;
        //            }

        //            if (Fee < 0)
        //            {
        //                Warning = Resources.SvCommissionLessThanZeroError;
        //                return;
        //            }

        //            /*
        //            if (xTezos.GetFeeAmount(Fee, FeePrice) > CurrencyViewModel.AvailableAmount) {
        //                Warning = Resources.SvAvailableFundsError;
        //                return;
        //            }*/

        //            var result = await GetDelegate();

        //            if (result.HasError)
        //                Warning = result.Error.Description;
        //            else
        //            {
        //                var confirmationViewModel = new DelegateConfirmationViewModel(DialogViewer, _onDelegate)
        //                {
        //                    Currency = _tezos,
        //                    WalletAddress = WalletAddress,
        //                    UseDefaultFee = UseDefaultFee,
        //                    Tx = _tx,
        //                    From = WalletAddress.Address,
        //                    To = Address,
        //                    IsAmountLessThanMin = WalletAddress.Balance < BakerViewModel.MinDelegation,
        //                    BaseCurrencyCode = BaseCurrencyCode,
        //                    BaseCurrencyFormat = BaseCurrencyFormat,
        //                    Fee = Fee,
        //                    FeeInBase = FeeInBase,
        //                    CurrencyCode = _tezos.FeeCode,
        //                    CurrencyFormat = _tezos.FeeFormat
        //                };

        //                DialogViewer.PushPage(Dialogs.Delegate, Pages.DelegateConfirmation, confirmationViewModel);
        //            }
        //        }
        //        finally
        //        {
        //            DelegationCheck = false;
        //        }
        //    }));

        //    private readonly Action _onDelegate;


        public DelegateViewModel(IAtomexApp app)
        {
            FromBakersList = new List<BakerViewModel>()
            {
                new BakerViewModel()
                {
                    Logo = "https://api.baking-bad.org/logos/tezoshodl.png",
                    Name = "TezosHODL",
                    Address = "tz1sdfldjsflksjdlkf123sfa",
                    Fee = 5,
                    MinDelegation = 10,
                    StakingAvailable = 10000.000000m
                },
                new BakerViewModel()
                {
                    Logo = "XTZ",
                    Name = "Tezgate",
                    Address = "tz761sdfldflksdslkf123sjk",
                    Fee = 9.90m,
                    MinDelegation = 100,
                    StakingAvailable = 1356622.776437m
                }
            };

            App = app ?? throw new ArgumentNullException(nameof(app));

            _tezos = App.Account.Currencies.Get<Tezos>("XTZ");
            FeeCurrencyCode = _tezos.FeeCode;
            BaseCurrencyCode = "USD";
            BaseCurrencyFormat = "$0.00";
            UseDefaultFee = true;
            PrepareWallet().WaitForResult();
        }

        public DelegateViewModel(
            IAtomexApp app,
            Action onDelegate = null)
        {
            App = app ?? throw new ArgumentNullException(nameof(app));
            //_onDelegate = onDelegate;

            _tezos = App.Account.Currencies.Get<Tezos>("XTZ");
            FeeCurrencyCode = _tezos.FeeCode;
            BaseCurrencyCode = "USD";
            BaseCurrencyFormat = "$0.00";
            UseDefaultFee = true;

            SubscribeToServices();
            //LoadBakerList().FireAndForget();
            PrepareWallet().WaitForResult();
        }

        //    private async Task LoadBakerList()
        //    {
        //        List<BakerViewModel> bakers = null;

        //        try
        //        {
        //            await Task.Run(async () =>
        //            {
        //                bakers = (await BbApi
        //                    .GetBakers(App.Account.Network)
        //                    .ConfigureAwait(false))
        //                    .Select(x => new BakerViewModel
        //                    {
        //                        Address = x.Address,
        //                        Logo = x.Logo,
        //                        Name = x.Name,
        //                        Fee = x.Fee,
        //                        MinDelegation = x.MinDelegation,
        //                        StakingAvailable = x.StakingAvailable
        //                    })
        //                    .ToList();
        //            });
        //        }
        //        catch (Exception e)
        //        {
        //            Log.Error(e.Message, "Error while fetching bakers list");
        //        }

        //        await Application.Current.Dispatcher.InvokeAsync(() =>
        //        {
        //            FromBakersList = bakers;
        //        }, DispatcherPriority.Background);
        //    }

        private async Task PrepareWallet(CancellationToken cancellationToken = default)
        {
            FromAddressList = (await App.Account
                .GetUnspentAddressesAsync(_tezos.Name, cancellationToken).ConfigureAwait(false))
                .OrderByDescending(x => x.Balance)
                .Select(w => new WalletAddressViewModel(w, _tezos.Format))
                .ToList();

            if (!FromAddressList?.Any() ?? false)
            {
                Warning = "You don't have non-empty accounts";
                return;
            }

            WalletAddressViewModel = FromAddressList.FirstOrDefault();
        }

        //    private async Task<Result<string>> GetDelegate(
        //        CancellationToken cancellationToken = default)
        //    {
        //        if (_walletAddress == null)
        //            return new Error(Errors.InvalidWallets, "You don't have non-empty accounts");

        //        var wallet = (HdWallet)App.Account.Wallet;
        //        var keyStorage = wallet.KeyStorage;
        //        var rpc = new Rpc(_tezos.RpcNodeUri);

        //        JObject delegateData;
        //        try
        //        {
        //            delegateData = await rpc
        //                .GetDelegate(_address)
        //                .ConfigureAwait(false);
        //        }
        //        catch
        //        {
        //            return new Error(Errors.WrongDelegationAddress, "Wrong delegation address");
        //        }

        //        if (delegateData["deactivated"].Value<bool>())
        //            return new Error(Errors.WrongDelegationAddress, "Baker is deactivated. Pick another one");

        //        var delegators = delegateData["delegated_contracts"]?.Values<string>();

        //        if (delegators.Contains(_walletAddress.Address))
        //            return new Error(Errors.AlreadyDelegated, $"Already delegated from {_walletAddress.Address} to {_address}");

        //        var tx = new TezosTransaction
        //        {
        //            StorageLimit = _tezos.StorageLimit,
        //            GasLimit = _tezos.GasLimit,
        //            From = _walletAddress.Address,
        //            To = _address,
        //            Fee = Fee.ToMicroTez(),
        //            Currency = _tezos,
        //            CreationTime = DateTime.UtcNow,
        //        };

        //        try
        //        {
        //            var calculatedFee = await tx.AutoFillAsync(keyStorage, _walletAddress, UseDefaultFee);
        //            if (!calculatedFee)
        //                return new Error(Errors.TransactionCreationError, $"Autofill transaction failed");

        //            Fee = tx.Fee;
        //            _tx = tx;
        //        }
        //        catch (Exception e)
        //        {
        //            Log.Error(e, "Autofill delegation error");
        //            return new Error(Errors.TransactionCreationError, $"Autofill delegation error. Try again later");
        //        }

        //        return "Successful check";
        //    }

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

        //    private void DesignerMode()
        //    {
        //        FromBakersList = new List<BakerViewModel>()
        //        {
        //            new BakerViewModel()
        //            {
        //                Logo = "https://api.baking-bad.org/logos/tezoshodl.png",
        //                Name = "TezosHODL",
        //                Address = "tz1sdfldjsflksjdlkf123sfa",
        //                Fee = 5,
        //                MinDelegation = 10,
        //                StakingAvailable = 10000.000000m
        //            }
        //        };

        //        BakerViewModel = FromBakersList.FirstOrDefault();

        //        _address = "tz1sdfldjsflksjdlkf123sfa";
        //        _fee = 5;
        //        _feeInBase = 123m;
        //    }
    }
}

