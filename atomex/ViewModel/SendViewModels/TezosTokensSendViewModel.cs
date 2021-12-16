using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using atomex.Resources;
using atomex.ViewModel.CurrencyViewModels;
using atomex.Views;
using atomex.Views.Popup;
using Atomex;
using Atomex.Blockchain.Tezos;
using Atomex.Common;
using Atomex.Core;
using Atomex.MarketData.Abstract;
using Atomex.TezosTokens;
using Atomex.Wallet.Abstract;
using Atomex.Wallet.Tezos;
using Rg.Plugins.Popup.Services;
using Serilog;
using Xamarin.Essentials;
using Xamarin.Forms;
using ZXing;

namespace atomex.ViewModel.SendViewModels
{
    public class TezosTokensSendViewModel : BaseViewModel
    {
        public const string DefaultCurrencyFormat = "F8";
        public const string DefaultBaseCurrencyCode = "USD";
        public const string DefaultBaseCurrencyFormat = "$0.00";
        public const int MaxCurrencyDecimals = 9;

        private readonly IAtomexApp AtomexApp;

        private readonly INavigation Navigation;

        private ObservableCollection<WalletAddressViewModel> _fromAddressList;
        public ObservableCollection<WalletAddressViewModel> FromAddressList
        {
            get => _fromAddressList;
            set
            {
                _fromAddressList = value;
                OnPropertyChanged(nameof(FromAddressList));
            }
        }

        private WalletAddressViewModel _from;
        public WalletAddressViewModel From
        {
            get => _from;
            set
            {
                _from = value;
                OnPropertyChanged(nameof(From));

                Warning = string.Empty;
                Amount = _amount;
                Fee = _fee;
                
                UpdateCurrencyCode();
            }
        }

        private TezosTokenContractViewModel _tokenContract;
        public TezosTokenContractViewModel TokenContract
        {
            get => _tokenContract;
            set
            {
                _tokenContract = value;
                OnPropertyChanged(nameof(TokenContract));
            }
        }

        private decimal _tokenId;
        public decimal TokenId
        {
            get => _tokenId;
            set
            {
                _tokenId = value;
                OnPropertyChanged(nameof(TokenId));
            }
        }


        private readonly string _tokenType;
        public bool IsFa2 => _tokenType == "FA2";

        protected string _to;
        public string To
        {
            get => _to;
            set
            {
                _to = value;
                OnPropertyChanged(nameof(To));

                Warning = string.Empty;
            }
        }

        private decimal _amount;
        public decimal Amount
        {
            get => _amount;
            set { UpdateAmount(value); }
        }

        public string AmountString
        {
            get => Amount.ToString(CultureInfo.InvariantCulture);
            set
            {
                if (!decimal.TryParse(value, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture,
                    out var amount))
                {
                    if (amount == 0)
                        Amount = amount;
                    OnPropertyChanged(nameof(AmountString));
                    return;
                }

                Amount = amount;
                OnPropertyChanged(nameof(AmountString));
            }
        }

        private bool _isAmountUpdating;
        public bool IsAmountUpdating
        {
            get => _isAmountUpdating;
            set { _isAmountUpdating = value; OnPropertyChanged(nameof(IsAmountUpdating)); }
        }

        private decimal _fee;
        public decimal Fee
        {
            get => _fee;
            set { UpdateFee(value); }
        }

        public string FeeString
        {
            get => Fee.ToString(CultureInfo.InvariantCulture);
            set
            {
                if (!decimal.TryParse(value, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var fee))
                {
                    if (fee == 0)
                        Fee = fee;
                    OnPropertyChanged(nameof(FeeString));
                    return;
                }

                Fee = fee;
                OnPropertyChanged(nameof(FeeString));
            }
        }

        private bool _isFeeUpdating;
        public bool IsFeeUpdating
        {
            get => _isFeeUpdating;
            set { _isFeeUpdating = value; OnPropertyChanged(nameof(IsFeeUpdating)); }
        }

        private bool _useDefaultFee;
        public bool UseDefaultFee
        {
            get => _useDefaultFee;
            set
            {
                _useDefaultFee = value;
                OnPropertyChanged(nameof(UseDefaultFee));

                if (_useDefaultFee)
                {
                    Warning = string.Empty;
                    Fee = _fee;
                }
            }
        }

        private decimal _amountInBase;
        public decimal AmountInBase
        {
            get => _amountInBase;
            set { _amountInBase = value; OnPropertyChanged(nameof(AmountInBase)); }
        }

        private decimal _feeInBase;
        public decimal FeeInBase
        {
            get => _feeInBase;
            set { _feeInBase = value; OnPropertyChanged(nameof(FeeInBase)); }
        }

        private string _currencyCode;
        public string CurrencyCode
        {
            get => _currencyCode;
            set { _currencyCode = value; OnPropertyChanged(nameof(CurrencyCode)); }
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

        private string _warning;
        public string Warning
        {
            get => _warning;
            set { _warning = value; OnPropertyChanged(nameof(Warning)); }
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

        public string AmountEntryPlaceholderString => $"{AppResources.AmountEntryPlaceholder}, {CurrencyCode}";
        public string FeeEntryPlaceholderString => $"{AppResources.FeeLabel}, {FeeCurrencyCode}";

        public TezosTokensSendViewModel(
            IAtomexApp app,
            INavigation navigation,
            string tokenType,
            string from = null,
            TezosTokenContractViewModel tokenContract = null,
            decimal tokenId = 0)
        {
            AtomexApp = app ?? throw new ArgumentNullException(nameof(app));

            Navigation = navigation ?? throw new ArgumentNullException(nameof(Navigation));

            CurrencyCode = "";
            FeeCurrencyCode = TezosConfig.Xtz;
            BaseCurrencyCode = DefaultBaseCurrencyCode;

            _tokenContract = tokenContract;
            _tokenId = tokenId;
            _tokenType = tokenType;

            UpdateFromAddressList(from);
            UpdateCurrencyCode();

            SubscribeToServices();

            UseDefaultFee = true;
        }

        private void SubscribeToServices()
        {
            if (AtomexApp.HasQuotesProvider)
                AtomexApp.QuotesProvider.QuotesUpdated += OnQuotesUpdatedEventHandler;
        }

        private ICommand _nextCommand;
        public ICommand NextCommand => _nextCommand ??= new Command(OnNextButtonClicked);

        private async void OnNextButtonClicked()
        {
            var tezosConfig = AtomexApp.Account
                .Currencies
                .Get<TezosConfig>(TezosConfig.Xtz);

            if (FromAddressList == null)
            {
                Warning = $"Insufficient token funds on addresses! Please update your balance!";
                return;
            }

            if (string.IsNullOrEmpty(To))
            {
                Warning = AppResources.EmptyAddressError;
                return;
            }

            if (!tezosConfig.IsValidAddress(To))
            {
                Warning = AppResources.InvalidAddressError;
                return;
            }

            if (Amount <= 0)
            {
                Warning = AppResources.AmountLessThanZeroError;
                return;
            }

            if (Fee <= 0)
            {
                Warning = AppResources.CommissionLessThanZeroError;
                return;
            }

            if (TokenContract == null || From?.Address == null)
            {
                Warning = "Invalid 'From' address or token contract address!";
                return;
            }

            if (!tezosConfig.IsValidAddress(TokenContract?.Contract?.Address))
            {
                Warning = "Invalid token contract address!";
                return;
            }

            var fromTokenAddress = await GetTokenAddressAsync(
                account: AtomexApp.Account,
                address: From?.Address,
                tokenContract: TokenContract?.Contract?.Address,
                tokenId: _tokenId,
                tokenType: _tokenType );

            if (fromTokenAddress == null)
            {
                Warning = $"Insufficient token funds on address {From?.Address}! Please update your balance!";
                return;
            }

            if (_amount > fromTokenAddress.Balance)
            {
                Warning = $"Insufficient token funds on address {fromTokenAddress.Address}! Please use Max button to find out how many tokens you can send!";
                return;
            }

            var xtzAddress = await AtomexApp.Account
                .GetAddressAsync(TezosConfig.Xtz, From.Address);

            if (xtzAddress == null)
            {
                Warning = $"Insufficient funds for fee. Please update your balance for address {From?.Address}!";
                return;
            }

            if (xtzAddress.AvailableBalance() < _fee)
            {
                Warning = $"Insufficient funds for fee!";
                return;
            }

            if (string.IsNullOrEmpty(Warning))
                await Navigation.PushAsync(new SendingConfirmationPage(this));
            else
                await Application.Current.MainPage.DisplayAlert(AppResources.Error, Warning, AppResources.AcceptButton);

        }

        private async void UpdateAmount(decimal amount)
        {
            if (IsAmountUpdating)
                return;

            IsAmountUpdating = true;
            Warning = string.Empty;

            _amount = amount;
            OnPropertyChanged(nameof(AmountString));

            try
            {
                var tezosConfig = AtomexApp.Account
                    .Currencies
                    .Get<TezosConfig>(TezosConfig.Xtz);

                if (TokenContract == null || From?.Address == null)
                {
                    Warning = "Invalid 'From' address or token contract address!";
                    return;
                }

                if (!tezosConfig.IsValidAddress(TokenContract?.Contract?.Address))
                {
                    Warning = "Invalid token contract address!";
                    return;
                }

                var fromTokenAddress = await GetTokenAddressAsync(
                    account: AtomexApp.Account,
                    address: From?.Address,
                    tokenContract: TokenContract?.Contract?.Address,
                    tokenId: _tokenId,
                    tokenType: _tokenType);

                if (fromTokenAddress == null)
                {
                    Warning = $"Insufficient token funds on address {From?.Address}! Please update your balance!";
                    return;
                }

                if (_amount > fromTokenAddress.Balance)
                {
                    Warning = $"Insufficient token funds on address {fromTokenAddress.Address}! Please use Max button to find out how many tokens you can send!";
                    return;
                }

                OnQuotesUpdatedEventHandler(AtomexApp.QuotesProvider, EventArgs.Empty);
            }
            finally
            {
                IsAmountUpdating = false;
            }
        }

        private async void UpdateFee(decimal fee)
        {
            if (IsFeeUpdating)
                return;

            IsFeeUpdating = true;

            try
            {
                var tezosConfig = AtomexApp.Account
                    .Currencies
                    .Get<TezosConfig>(TezosConfig.Xtz);

                if (TokenContract == null || From?.Address == null)
                {
                    Warning = "Invalid 'From' address or token contract address!";
                    return;
                }

                if (!tezosConfig.IsValidAddress(TokenContract?.Contract?.Address))
                {
                    Warning = "Invalid token contract address!";
                    return;
                }

                if (UseDefaultFee)
                {
                    var fromTokenAddress = await GetTokenAddressAsync(
                        account: AtomexApp.Account,
                        address: From?.Address,
                        tokenContract: TokenContract?.Contract?.Address,
                        tokenId: _tokenId,
                        tokenType: _tokenType);

                    if (fromTokenAddress == null)
                    {
                        Warning = $"Insufficient token funds on address {From?.Address}! Please update your balance!";
                        return;
                    }

                    var tokenAccount = AtomexApp.Account
                        .GetTezosTokenAccount<TezosTokenAccount>(fromTokenAddress.Currency, TokenContract?.Contract?.Address, TokenId);

                    var (estimatedFee, isEnougth) = await tokenAccount
                        .EstimateTransferFeeAsync(From?.Address);

                    if (!isEnougth)
                    {
                        Warning = $"Insufficient funds for fee. Minimum {estimatedFee} XTZ is required!";
                        return;
                    }

                    _fee = estimatedFee;
                }
                else
                {
                    var xtzAddress = await AtomexApp.Account
                        .GetAddressAsync(TezosConfig.Xtz, From?.Address);

                    if (xtzAddress == null)
                    {
                        Warning = $"Insufficient funds for fee. Please update your balance for address {From?.Address}!";
                        return;
                    }

                    _fee = Math.Min(fee, tezosConfig.GetMaximumFee());

                    if (xtzAddress.AvailableBalance() < _fee)
                    {
                        Warning = $"Insufficient funds for fee!";
                        return;
                    }
                }

                OnPropertyChanged(nameof(FeeString));

                OnQuotesUpdatedEventHandler(AtomexApp.QuotesProvider, EventArgs.Empty);
            }
            finally
            {
                IsFeeUpdating = false;
            }
        }

        private async void OnMaxClick()
        {
            if (IsAmountUpdating)
                return;

            IsAmountUpdating = true;

            Warning = string.Empty;

            try
            {
                var tezosConfig = AtomexApp.Account
                    .Currencies
                    .Get<TezosConfig>(TezosConfig.Xtz);

                if (TokenContract == null || From?.Address == null)
                {
                    _amount = 0;
                    OnPropertyChanged(nameof(AmountString));

                    return;
                }

                if (!tezosConfig.IsValidAddress(TokenContract?.Contract?.Address))
                {
                    _amount = 0;
                    OnPropertyChanged(nameof(AmountString));

                    Warning = "Invalid token contract address!";
                    return;
                }

                var fromTokenAddress = await GetTokenAddressAsync(
                    account: AtomexApp.Account,
                    address: From?.Address,
                    tokenContract: TokenContract?.Contract?.Address,
                    tokenId: _tokenId,
                    tokenType: _tokenType);

                if (fromTokenAddress == null)
                {
                    _amount = 0;
                    OnPropertyChanged(nameof(AmountString));

                    Warning = $"Insufficient token funds on address {From?.Address}! Please update your balance!";
                    return;
                }

                _amount = fromTokenAddress.Balance;
                OnPropertyChanged(nameof(AmountString));

                OnQuotesUpdatedEventHandler(AtomexApp.QuotesProvider, EventArgs.Empty);
            }
            finally
            {
                IsAmountUpdating = false;
            }
        }

        private void OnQuotesUpdatedEventHandler(object sender, EventArgs args)
        {
            if (!(sender is ICurrencyQuotesProvider quotesProvider))
                return;

            AmountInBase = !string.IsNullOrEmpty(CurrencyCode)
                ? Amount * (quotesProvider.GetQuote(CurrencyCode, BaseCurrencyCode)?.Bid ?? 0m)
                : 0;

            FeeInBase = !string.IsNullOrEmpty(FeeCurrencyCode)
                ? Fee * (quotesProvider.GetQuote(FeeCurrencyCode, BaseCurrencyCode)?.Bid ?? 0m)
                : 0;
        }

        public static async Task<WalletAddress> GetTokenAddressAsync(
            IAccount account,
            string address,
            string tokenContract,
            decimal tokenId,
            string tokenType)
        {
            var tezosAccount = account
                .GetCurrencyAccount<TezosAccount>(TezosConfig.Xtz);

            return await tezosAccount
                .DataRepository
                .GetTezosTokenAddressAsync(tokenType, tokenContract, tokenId, address);
        }

        private void UpdateFromAddressList(string from)
        {
            _fromAddressList = new ObservableCollection<WalletAddressViewModel>(GetFromAddressList(TokenContract?.Contract?.Address));

            var tempFrom = from;

            if (tempFrom == null)
            {
                var unspentAddresses = _fromAddressList.Where(w => w.AvailableBalance > 0);
                var unspentTokenAddresses = _fromAddressList.Where(w => w.TokenBalance > 0);

                tempFrom = unspentTokenAddresses.MaxByOrDefault(w => w.TokenBalance)?.Address ??
                    unspentAddresses.MaxByOrDefault(w => w.AvailableBalance)?.Address;
            }

            OnPropertyChanged(nameof(FromAddressList));
        }

        private async void UpdateCurrencyCode()
        {
            if (TokenContract == null || From?.Address == null)
                return;

            var tokenAddress = await GetTokenAddressAsync(
                account: AtomexApp.Account,
                address: From?.Address,
                tokenContract: TokenContract?.Contract?.Address,
                tokenId: _tokenId,
                tokenType: _tokenType);

            if (tokenAddress?.TokenBalance?.Symbol != null)
            {
                CurrencyCode = tokenAddress.TokenBalance.Symbol;
                OnPropertyChanged(nameof(AmountString));
            }
            else
            {
                CurrencyCode = AtomexApp.Account.Currencies
                    .FirstOrDefault(c => c is Fa12Config fa12 && fa12.TokenContractAddress == TokenContract?.Contract?.Address)
                    ?.Name ?? "TOKENS";
                OnPropertyChanged(nameof(AmountString));
            }
        }

        private IEnumerable<WalletAddressViewModel> GetFromAddressList(string tokenContract)
        {
            if (tokenContract == null)
                return Enumerable.Empty<WalletAddressViewModel>();

            var tezosConfig = AtomexApp.Account
                .Currencies
                .Get<TezosConfig>(TezosConfig.Xtz);

            var tezosAccount = AtomexApp.Account
                .GetCurrencyAccount<TezosAccount>(TezosConfig.Xtz);

            var tezosAddresses = tezosAccount
                .GetUnspentAddressesAsync()
                .WaitForResult()
                .ToDictionary(w => w.Address, w => w);

            var tokenAddresses = tezosAccount.DataRepository
                .GetTezosTokenAddressesByContractAsync(tokenContract)
                .WaitForResult();

            return tokenAddresses
                .Where(w => w.Balance != 0)
                .Select(w =>
                {
                    var tokenBalance = w.Balance;

                    var showTokenBalance = tokenBalance != 0;

                    var tokenCode = w.TokenBalance?.Symbol ?? "TOKENS";

                    var tezosBalance = tezosAddresses.TryGetValue(w.Address, out var tezosAddress)
                        ? tezosAddress.AvailableBalance()
                        : 0m;

                    return new WalletAddressViewModel
                    {
                        Address = w.Address,
                        AvailableBalance = tezosBalance,
                        CurrencyFormat = tezosConfig.Format,
                        CurrencyCode = tezosConfig.Name,
                        IsFreeAddress = false,
                        ShowTokenBalance = showTokenBalance,
                        TokenBalance = tokenBalance,
                        TokenFormat = "F8",
                        TokenCode = tokenCode
                    };
                })
                .ToList();
        }

        private ICommand _showAddressesCommand;
        public ICommand ShowAddressesCommand => _showAddressesCommand ??= new Command(async () => await OnShowAddressesClicked());

        private ICommand _selectAddressCommand;
        public ICommand SelectAddressCommand => _selectAddressCommand ??= new Command<WalletAddressViewModel>(async (address) => await OnAddressClicked(address));

        private ICommand _pasteCommand;
        public ICommand PasteCommand => _pasteCommand ??= new Command(async () => await OnPasteButtonClicked());

        private ICommand _scanCommand;
        public ICommand ScanCommand => _scanCommand ??= new Command(async () => await OnScanButtonClicked());

        private ICommand _maxAmountCommand;
        public ICommand MaxAmountCommand => _maxAmountCommand ??= new Command(() => OnMaxClick());

        private ICommand _onScanAddressCommand;
        public ICommand OnScanAddressCommand => _onScanAddressCommand ??= new Command(async () => await OnScanResultCommand());

        public Result ScanResult { get; set; }

        private bool _isScanning = true;
        public bool IsScanning
        {
            get => _isScanning;
            set { _isScanning = value; OnPropertyChanged(nameof(IsScanning)); }
        }

        private bool _isAnalyzing = true;
        public bool IsAnalyzing
        {
            get => _isAnalyzing;
            set { _isAnalyzing = value; OnPropertyChanged(nameof(IsAnalyzing)); }
        }

        private async Task OnScanResultCommand()
        {
            IsScanning = IsAnalyzing = false;

            if (ScanResult == null)
            {
                await Application.Current.MainPage.DisplayAlert(AppResources.Error, "Incorrect QR code format", AppResources.AcceptButton);
                await Navigation.PopAsync();
                return;
            }

            Device.BeginInvokeOnMainThread(() =>
            {
                int indexOfChar = ScanResult.Text.IndexOf(':');
                if (indexOfChar == -1)
                    To = ScanResult.Text;
                else
                    To = ScanResult.Text.Substring(indexOfChar + 1);
            });

            await Navigation.PopAsync();
        }

        private async Task OnShowAddressesClicked()
        {
            await Navigation.PushAsync(new AddressesListPage(this));
        }

        private async Task OnAddressClicked(WalletAddressViewModel address)
        {
            From = address;
            
            await Navigation.PopAsync();
        }

        private async Task OnScanButtonClicked()
        {
            IsScanning = IsAnalyzing = true;
            PermissionStatus permissions = await Permissions.CheckStatusAsync<Permissions.Camera>();

            if (permissions != PermissionStatus.Granted)
                permissions = await Permissions.RequestAsync<Permissions.Camera>();
            if (permissions != PermissionStatus.Granted)
                return;

            await Navigation.PushAsync(new ScanningQrPage(this));
        }

        private async Task OnPasteButtonClicked()
        {
            if (Clipboard.HasText)
            {
                var text = await Clipboard.GetTextAsync();
                To = text;
            }
            else
            {
                await Application.Current.MainPage.DisplayAlert(AppResources.Error, AppResources.EmptyClipboard, AppResources.AcceptButton);
            }
        }

        private ICommand _sendCommand;
        public ICommand SendCommand => _sendCommand ??= new Command(async () => await Send());

        private async Task Send()
        {
            IsLoading = true;

            try
            {
                IsLoading = true;

                Error error;

                var tezosAccount = AtomexApp.Account
                    .GetCurrencyAccount<TezosAccount>(TezosConfig.Xtz);

                var tokenAddress = await GetTokenAddressAsync(
                    AtomexApp.Account,
                    From?.Address,
                    TokenContract?.Contract?.Address,
                    _tokenId,
                    _tokenType);

                if (tokenAddress.Currency == "FA12")
                {
                    var currencyName = AtomexApp.Account.Currencies
                        .FirstOrDefault(c => c is Fa12Config fa12 && fa12.TokenContractAddress == TokenContract?.Contract?.Address)
                        ?.Name ?? "FA12";

                    var tokenAccount = AtomexApp.Account.GetTezosTokenAccount<Fa12Account>(
                        currency: currencyName,
                        tokenContract: TokenContract?.Contract?.Address,
                        tokenId: TokenId);

                    error = await tokenAccount.SendAsync(
                        from: tokenAddress.Address,
                        to: To,
                        amount: Amount,
                        fee: Fee,
                        useDefaultFee: UseDefaultFee);
                }
                else
                {
                    var tokenAccount = AtomexApp.Account.GetTezosTokenAccount<Fa2Account>(
                        currency: "FA2",
                        tokenContract: TokenContract?.Contract?.Address,
                        tokenId: TokenId);

                    var decimals = tokenAddress.TokenBalance.Decimals;
                    var amount = Amount * (decimal)Math.Pow(10, decimals);
                    var fee = (int)Fee.ToMicroTez();

                    error = await tokenAccount.SendAsync(
                        from: From?.Address,
                        to: To,
                        amount: amount,
                        tokenContract: TokenContract?.Contract?.Address,
                        tokenId: (int)TokenId,
                        fee: fee,
                        useDefaultFee: UseDefaultFee);
                }

                if (error != null)
                {
                    IsLoading = false;
                    await PopupNavigation.Instance.PushAsync(new CompletionPopup(
                        new PopupViewModel
                        {
                            Type = PopupType.Error,
                            Title = AppResources.Error,
                            Body = error.Description,
                            ButtonText = AppResources.AcceptButton
                        }));
                    return;
                }

                await PopupNavigation.Instance.PushAsync(new CompletionPopup(
                    new PopupViewModel
                    {
                        Type = PopupType.Success,
                        Title = AppResources.Success,
                        Body = string.Format(CultureInfo.InvariantCulture, AppResources.CurrencySentToAddress, Amount, CurrencyCode, To),
                        ButtonText = AppResources.AcceptButton
                    }));

                Navigation.RemovePage(Navigation.NavigationStack[Navigation.NavigationStack.Count - 2]);
                await Navigation.PopAsync();
            }
            catch (Exception e)
            {
                await PopupNavigation.Instance.PushAsync(new CompletionPopup(
                    new PopupViewModel
                    {
                        Type = PopupType.Error,
                        Title = AppResources.Error,
                        Body = AppResources.SendingTransactionError,
                        ButtonText = AppResources.AcceptButton
                    }));
                Log.Error(e, "Transaction send error.");
            }

            IsLoading = false;
        }
    }
}
