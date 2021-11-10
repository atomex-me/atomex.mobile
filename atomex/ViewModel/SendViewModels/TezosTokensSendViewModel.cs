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

namespace atomex.ViewModel.SendViewModels
{
    public class TezosTokensSendViewModel : BaseViewModel
    {
        public const string DefaultCurrencyFormat = "F8";
        public const string DefaultBaseCurrencyCode = "USD";
        public const string DefaultBaseCurrencyFormat = "$0.00";
        public const int MaxCurrencyDecimals = 9;

        private readonly IAtomexApp _app;

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

        public string CurrencyFormat { get; set; }
        public string FeeCurrencyFormat { get; set; }

        private string _baseCurrencyFormat;
        public string BaseCurrencyFormat
        {
            get => _baseCurrencyFormat;
            set { _baseCurrencyFormat = value; OnPropertyChanged(nameof(BaseCurrencyFormat)); }
        }

        private decimal _amount;
        public decimal Amount
        {
            get => _amount;
            set { _ = UpdateAmount(value); }
        }

        public string AmountString
        {
            //get => Amount.ToString(CurrencyFormat, CultureInfo.InvariantCulture);
            get => Amount.ToString(CultureInfo.InvariantCulture);
            set
            {
                string temp = value.Replace(",", ".");
                if (!decimal.TryParse(temp, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var amount))
                {
                    ResetSendValues(raiseOnPropertyChanged: false);
                    return;
                }

                _ = UpdateAmount(amount, raiseOnPropertyChanged: false);
            }
        }

        private decimal _fee;
        public decimal Fee
        {
            get => _fee;
            set { _ = UpdateFee(value); }
        }

        public string FeeString
        {
            get => Fee.ToString(FeeCurrencyFormat, CultureInfo.InvariantCulture);
            set
            {
                string temp = value.Replace(",", ".");
                if (!decimal.TryParse(temp, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var fee))
                    return;

                Fee = fee;
            }
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
                    _ = UpdateAmount(_amount);

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
            string from = null,
            TezosTokenContractViewModel tokenContract = null,
            decimal tokenId = 0)
        {
            _app = app ?? throw new ArgumentNullException(nameof(app));

            Navigation = navigation ?? throw new ArgumentNullException(nameof(Navigation));

            var tezosConfig = _app.Account
                .Currencies
                .Get<TezosConfig>(TezosConfig.Xtz);

            CurrencyCode = "";
            FeeCurrencyCode = TezosConfig.Xtz;
            BaseCurrencyCode = DefaultBaseCurrencyCode;

            CurrencyFormat = DefaultCurrencyFormat;
            FeeCurrencyFormat = tezosConfig.FeeFormat;
            BaseCurrencyFormat = DefaultBaseCurrencyFormat;

            _tokenContract = tokenContract;
            _tokenId = tokenId;

            UpdateFromAddressList(from);
            UpdateCurrencyCode();

            SubscribeToServices();

            UseDefaultFee = true;
        }

        private void SubscribeToServices()
        {
            if (_app.HasQuotesProvider)
                _app.QuotesProvider.QuotesUpdated += OnQuotesUpdatedEventHandler;
        }

        private void ResetSendValues(bool raiseOnPropertyChanged = true)
        {
            _amount = 0;
            OnPropertyChanged(nameof(Amount));

            if (raiseOnPropertyChanged)
                OnPropertyChanged(nameof(AmountString));

            AmountInBase = 0;

            Fee = 0;

            OnPropertyChanged(nameof(FeeString));

            FeeInBase = 0;
        }

        private ICommand _nextCommand;
        public ICommand NextCommand => _nextCommand ??= new Command(OnNextButtonClicked);

        private async void OnNextButtonClicked()
        {
            var tezosConfig = _app.Account
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

            var fromTokenAddress = await GetTokenAddressAsync(_app.Account, From.Address, TokenContract?.Contract?.Address, TokenId);

            if (fromTokenAddress == null)
            {
                Warning = $"Insufficient token funds on address {From.Address}! Please update your balance!";
                return;
            }

            if (_amount > fromTokenAddress.Balance)
            {
                Warning = $"Insufficient token funds on address {fromTokenAddress.Address}! Please use Max button to find out how many tokens you can send!";
                return;
            }

            var xtzAddress = await _app.Account
                .GetAddressAsync(TezosConfig.Xtz, From.Address);

            if (xtzAddress == null)
            {
                Warning = $"Insufficient funds for fee. Please update your balance for address {From.Address}!";
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

        private async Task UpdateAmount(decimal amount, bool raiseOnPropertyChanged = true)
        {
            Warning = string.Empty;

            if (amount == 0)
            {
                ResetSendValues(raiseOnPropertyChanged);
                return;
            }

            _amount = amount;

            try
            {
                var tezosConfig = _app.Account
                    .Currencies
                    .Get<TezosConfig>(TezosConfig.Xtz);

                if (FromAddressList == null)
                {
                    _amount = 0;
                    OnPropertyChanged(nameof(AmountString));

                    Warning = $"Insufficient token funds on addresses! Please update your balance!";
                    return;
                }

                if (TokenContract == null || From == null)
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
                    account: _app.Account,
                    address: From.Address,
                    tokenContract: TokenContract?.Contract?.Address,
                    tokenId: TokenId);

                if (fromTokenAddress == null)
                {
                    Warning = $"Insufficient token funds on address {From.Address}! Please update your balance!";
                    return;
                }

                if (_amount > fromTokenAddress.Balance)
                {
                    Warning = $"Insufficient token funds on address {fromTokenAddress.Address}! Please use Max button to find out how many tokens you can send!";
                    return;
                }

                OnQuotesUpdatedEventHandler(_app.QuotesProvider, EventArgs.Empty);
            }
            catch (Exception e)
            {
                Log.Error(e, "Update amount error");
            }
        }

        private async Task UpdateFee(decimal fee)
        {
            try
            {
                var tezosConfig = _app.Account
                    .Currencies
                    .Get<TezosConfig>(TezosConfig.Xtz);

                if (TokenContract == null || From == null)
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
                        account: _app.Account,
                        address: From.Address,
                        tokenContract: TokenContract?.Contract?.Address,
                        tokenId: TokenId);

                    if (fromTokenAddress == null)
                    {
                        Warning = $"Insufficient token funds on address {From.Address}! Please update your balance!";
                        return;
                    }

                    var tokenAccount = _app.Account
                        .GetTezosTokenAccount<TezosTokenAccount>(fromTokenAddress.Currency, TokenContract?.Contract?.Address, TokenId);

                    var (estimatedFee, isEnougth) = await tokenAccount
                        .EstimateTransferFeeAsync(From.Address);

                    if (!isEnougth)
                    {
                        Warning = $"Insufficient funds for fee. Minimum {estimatedFee} XTZ is required!";
                        return;
                    }

                    _fee = estimatedFee;
                }
                else
                {
                    var xtzAddress = await _app.Account
                        .GetAddressAsync(TezosConfig.Xtz, From.Address);

                    if (xtzAddress == null)
                    {
                        Warning = $"Insufficient funds for fee. Please update your balance for address {From.Address}!";
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

                OnQuotesUpdatedEventHandler(_app.QuotesProvider, EventArgs.Empty);
            }
            catch (Exception e)
            {
                Log.Error(e, "Update fee error");
            }
        }

        private async Task OnMaxClick()
        {

            Warning = string.Empty;

            try
            {
                var tezosConfig = _app.Account
                    .Currencies
                    .Get<TezosConfig>(TezosConfig.Xtz);

                if (TokenContract == null || From == null)
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
                    account: _app.Account,
                    address: From.Address,
                    tokenContract: TokenContract?.Contract?.Address,
                    tokenId: TokenId);

                if (fromTokenAddress == null)
                {
                    _amount = 0;
                    OnPropertyChanged(nameof(AmountString));

                    Warning = $"Insufficient token funds on address {From.Address}! Please update your balance!";
                    return;
                }

                _amount = fromTokenAddress.Balance;
                OnPropertyChanged(nameof(AmountString));

                OnQuotesUpdatedEventHandler(_app.QuotesProvider, EventArgs.Empty);
            }
            catch (Exception e)
            {
                Log.Error(e, "Max click error");
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

        public async Task<WalletAddress> GetTokenAddressAsync(
            IAccount account,
            string address,
            string tokenContract,
            decimal tokenId)
        {
            var tezosAccount = account
                .GetCurrencyAccount<TezosAccount>(TezosConfig.Xtz);

            return await tezosAccount
                .DataRepository
                .GetTezosTokenAddressAsync(TokenContract?.Contract?.GetContractType(), tokenContract, tokenId, address);
        }

        private void UpdateFromAddressList(string from)
        {
            _fromAddressList = new ObservableCollection<WalletAddressViewModel>(GetFromAddressList(TokenContract?.Contract?.Address));

            if (string.IsNullOrEmpty(from))
            {
                var unspentAddresses = _fromAddressList.Where(w => w.AvailableBalance > 0);
                var unspentTokenAddresses = _fromAddressList.Where(w => w.TokenBalance > 0);

                From = unspentTokenAddresses.MaxByOrDefault(w => w.TokenBalance) ??
                    unspentAddresses.MaxByOrDefault(w => w.AvailableBalance);
            }
            else
            {
                var fromAddressViewModel = _fromAddressList.Where(w => w.Address == from).First();
                From = fromAddressViewModel;
            }

            OnPropertyChanged(nameof(FromAddressList));
        }

        private async void UpdateCurrencyCode()
        {
            if (TokenContract == null || From == null)
                return;

            var tokenAddress = await GetTokenAddressAsync(
                account: _app.Account,
                address: From.Address,
                tokenContract: TokenContract?.Contract?.Address,
                tokenId: TokenId);

            if (tokenAddress?.TokenBalance?.Symbol != null)
            {
                CurrencyCode = tokenAddress.TokenBalance.Symbol;
                CurrencyFormat = $"F{Math.Min(tokenAddress.TokenBalance.Decimals, MaxCurrencyDecimals)}";
                OnPropertyChanged(nameof(AmountString));
            }
            else
            {
                CurrencyCode = _app.Account.Currencies
                    .FirstOrDefault(c => c is Fa12Config fa12 && fa12.TokenContractAddress == TokenContract?.Contract?.Address)
                    ?.Name ?? "TOKENS";
                CurrencyFormat = DefaultCurrencyFormat;
                OnPropertyChanged(nameof(AmountString));
            }
        }

        private IEnumerable<WalletAddressViewModel> GetFromAddressList(string tokenContract)
        {
            if (tokenContract == null)
                return Enumerable.Empty<WalletAddressViewModel>();

            var tezosConfig = _app.Account
                .Currencies
                .Get<TezosConfig>(TezosConfig.Xtz);

            var tezosAccount = _app.Account
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
        public ICommand MaxAmountCommand => _maxAmountCommand ??= new Command(async () => await OnMaxClick());

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
            PermissionStatus permissions = await Permissions.CheckStatusAsync<Permissions.Camera>();

            if (permissions != PermissionStatus.Granted)
                permissions = await Permissions.RequestAsync<Permissions.Camera>();
            if (permissions != PermissionStatus.Granted)
                return;

            var scanningQrPage = new ScanningQrPage(selected =>
            {
                To = selected;
            });

            await Navigation.PushAsync(scanningQrPage);
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

                var tezosAccount = _app.Account
                    .GetCurrencyAccount<TezosAccount>(TezosConfig.Xtz);

                var tokenAddress = await GetTokenAddressAsync(
                    _app.Account,
                    From.Address,
                    TokenContract?.Contract?.Address,
                    TokenId);

                if (tokenAddress.Currency == "FA12")
                {
                    var currencyName = _app.Account.Currencies
                        .FirstOrDefault(c => c is Fa12Config fa12 && fa12.TokenContractAddress == TokenContract?.Contract?.Address)
                        ?.Name ?? "FA12";

                    var tokenAccount = _app.Account
                        .GetTezosTokenAccount<Fa12Account>(currencyName, TokenContract?.Contract?.Address, TokenId);

                    error = await tokenAccount
                        .SendAsync(new WalletAddress[] { tokenAddress }, To, Amount, Fee, 1, UseDefaultFee);
                }
                else
                {
                    var tokenAccount = _app.Account
                        .GetTezosTokenAccount<Fa2Account>("FA2", TokenContract?.Contract?.Address, TokenId);

                    var decimals = tokenAddress.TokenBalance.Decimals;
                    var amount = Amount * (decimal)Math.Pow(10, decimals);
                    var fee = (int)Fee.ToMicroTez();

                    error = await tokenAccount.SendAsync(
                        from: From.Address,
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
