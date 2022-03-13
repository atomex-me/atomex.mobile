using System;
using System.Globalization;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using atomex.Common;
using atomex.Models;
using atomex.Resources;
using atomex.Views.Popup;
using atomex.Views.Send;
using Atomex;
using Atomex.Blockchain.Tezos;
using Atomex.Core;
using Atomex.MarketData.Abstract;
using Atomex.TezosTokens;
using Atomex.ViewModels;
using Atomex.Wallet.Abstract;
using Atomex.Wallet.Tezos;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Rg.Plugins.Popup.Services;
using Serilog;
using Xamarin.Forms;

namespace atomex.ViewModel.SendViewModels
{
    public class TezosTokensSendViewModel : BaseViewModel
    {
        protected IAtomexApp App { get; }
        protected INavigation Navigation { get; set; }

        public const string DefaultBaseCurrencyCode = "USD";
        public const string DefaultBaseCurrencyFormat = "$0.00";
        public const bool IsToken = true;

        [Reactive] public decimal SelectedFromBalance { get; set; }
        [Reactive] public string From { get; set; }
        [Reactive] public string TokenContract { get; set; }
        [Reactive] public decimal TokenId { get; set; }
        [Reactive] public string To { get; set; }
        [Reactive] public UriImageSource TokenPreview { get; set; }
        private readonly string _tokenType;
        public bool IsFa2 => _tokenType == "FA2";

        [Reactive] public decimal Amount { get; set; }
        [ObservableAsProperty] public string TotalAmountString { get; }
        public string AmountString
        {
            get => Amount.ToString(CultureInfo.InvariantCulture);
            set
            {
                string temp = value.Replace(",", ".");
                if (!decimal.TryParse(
                    s: temp,
                    style: NumberStyles.AllowDecimalPoint,
                    provider: CultureInfo.InvariantCulture,
                    result: out var amount))
                {
                    Amount = 0;
                }
                else
                {
                    Amount = amount;

                    if (Amount > long.MaxValue)
                        Amount = long.MaxValue;
                }

                Device.InvokeOnMainThreadAsync(() =>
                {
                    this.RaisePropertyChanged(nameof(Amount));
                });
            }
        }

        public void SetAmountFromString(string value)
        {
            if (value == AmountString)
            {
                this.RaisePropertyChanged(nameof(AmountString));
                return;
            }

            string temp = value.Replace(",", ".");
            if (!decimal.TryParse(
                s: temp,
                style: NumberStyles.AllowDecimalPoint,
                provider: CultureInfo.InvariantCulture,
                result: out var amount))
            {
                AmountString = "0";
            }
            else
            {
                if (amount > long.MaxValue)
                    AmountString = long.MaxValue.ToString();
                else
                    AmountString = value;
            }

            this.RaisePropertyChanged(nameof(AmountString));
        }

        [Reactive] public decimal Fee { get; set; }

        public string FeeString
        {
            get => Fee.ToString(CultureInfo.InvariantCulture);
            set
            {
                string temp = value.Replace(",", ".");
                if (!decimal.TryParse(
                    s: temp,
                    style: NumberStyles.AllowDecimalPoint,
                    provider: CultureInfo.InvariantCulture,
                    result: out var fee))
                {
                    Fee = 0;
                }
                else
                {
                    Fee = fee;
                }

                Device.InvokeOnMainThreadAsync(() =>
                {
                    this.RaisePropertyChanged(nameof(Fee));
                });
            }
        }

        public void SetFeeFromString(string value)
        {
            if (value == FeeString)
            {
                this.RaisePropertyChanged(nameof(FeeString));
                return;
            }

            string temp = value.Replace(",", ".");
            if (!decimal.TryParse(
                s: temp,
                style: NumberStyles.AllowDecimalPoint,
                provider: CultureInfo.InvariantCulture,
                result: out var amount))
            {
                FeeString = "0";
            }
            else
            {
                if (amount > long.MaxValue)
                    FeeString = long.MaxValue.ToString();
                else
                    FeeString = value;
            }

            this.RaisePropertyChanged(nameof(FeeString));
        }

        [Reactive] public bool UseDefaultFee { get; set; }
        [Reactive] public decimal AmountInBase { get; set; }
        [Reactive] public decimal FeeInBase { get; set; }
        [Reactive] public string CurrencyCode { get; set; }
        [Reactive] public string FeeCurrencyCode { get; set; }
        public string BaseCurrencyCode { get; set; }

        [Reactive] public Message Message { get; set; }
        [Reactive] public bool ConfirmStage { get; set; }
        [Reactive] public bool CanSend { get; set; }
        [Reactive] public bool IsLoading { get; set; }

        public SelectAddressViewModel SelectFromViewModel { get; set; }
        public SelectAddressViewModel SelectToViewModel { get; set; }

        public TezosTokensSendViewModel(
            IAtomexApp app,
            INavigation navigation,
            string tokenContract,
            decimal tokenId,
            string tokenType,
            UriImageSource tokenPreview,
            string from = null)
        {
            App = app ?? throw new ArgumentNullException(nameof(App));
            Navigation = navigation ?? throw new ArgumentNullException(nameof(Navigation));
            Message = new Message();

            var tezosConfig = App.Account
                .Currencies
                .Get<TezosConfig>(TezosConfig.Xtz);

            var updateCurrencyCodeCommand = ReactiveCommand.Create(UpdateCurrencyCode);
            var updateAmountCommand = ReactiveCommand.CreateFromTask(UpdateAmount);
            var updateFeeCommand = ReactiveCommand.CreateFromTask(UpdateFee);

            this.WhenAnyValue(
                    vm => vm.From,
                    vm => vm.Amount,
                    vm => vm.Fee)
                .SubscribeInMainThread(_ =>
                {
                    Message.Text = string.Empty;
                    this.RaisePropertyChanged(nameof(Message));
                });

            this.WhenAnyValue(
                    vm => vm.From,
                    vm => vm.TokenId)
                .Select(_ => Unit.Default)
                .InvokeCommandInMainThread(updateCurrencyCodeCommand);

            this.WhenAnyValue(
                    vm => vm.Amount,
                    vm => vm.From,
                    vm => vm.To,
                    vm => vm.TokenId)
                .Select(_ => Unit.Default)
                .InvokeCommandInMainThread(updateAmountCommand);

            this.WhenAnyValue(
                    vm => vm.Fee,
                    vm => vm.From,
                    vm => vm.TokenId,
                    vm => vm.UseDefaultFee)
                .Select(_ => Unit.Default)
                .InvokeCommandInMainThread(updateFeeCommand);

            this.WhenAnyValue(vm => vm.Amount)
                .Select(totalAmount => totalAmount.ToString())
                .ToPropertyExInMainThread(this, vm => vm.TotalAmountString);

            this.WhenAnyValue(
                    vm => vm.Amount,
                    vm => vm.Fee)
                .Subscribe(_ => OnQuotesUpdatedEventHandler(App.QuotesProvider, EventArgs.Empty));

            this.WhenAnyValue(
                    vm => vm.Amount,
                    vm => vm.To,
                    vm => vm.Fee,
                    vm => vm.Message,
                    vm => vm.ConfirmStage)
                .Throttle(TimeSpan.FromMilliseconds(1))
                .SubscribeInMainThread(_ =>
                {
                    if (!ConfirmStage)
                    {
                        CanSend = To != null &&
                                  Amount > 0 &&
                                  (string.IsNullOrEmpty(Message.Text) || (Message != null && Message.Type != MessageType.Error)) &&
                                  !IsLoading;
                    }
                    else
                    {
                        CanSend = true;
                    }
                });

            CurrencyCode = string.Empty;
            FeeCurrencyCode = TezosConfig.Xtz;
            BaseCurrencyCode = DefaultBaseCurrencyCode;

            TokenContract = tokenContract;
            TokenId = tokenId;
            TokenPreview = tokenPreview;
            _tokenType = tokenType;

            if (from != null)
            {
                From = from;
                Amount = SelectedFromBalance;
            }

            UpdateCurrencyCode();
            SubscribeToServices();
            UseDefaultFee = true;

            SelectFromViewModel = new SelectAddressViewModel(
                account: App.Account,
                currency: tezosConfig,
                navigation: Navigation,
                mode: SelectAddressMode.SendFrom,
                selectedAddress: from,
                selectedTokenId: tokenId,
                tokenContract: tokenContract)
            {
                ConfirmAction = ConfirmFromAddress
            };

            SelectToViewModel = new SelectAddressViewModel(
                account: App.Account,
                currency: tezosConfig,
                navigation: Navigation)
            {
                ConfirmAction = ConfirmToAddress
            };
        }

        private void SubscribeToServices()
        {
            if (App.HasQuotesProvider)
                App.QuotesProvider.QuotesUpdated += OnQuotesUpdatedEventHandler;
        }

        private ReactiveCommand<Unit, Unit> _nextCommand;
        public ReactiveCommand<Unit, Unit> NextCommand => _nextCommand ??= ReactiveCommand.Create(OnNextCommand);

        private ReactiveCommand<Unit, Unit> _maxCommand;
        public ReactiveCommand<Unit, Unit> MaxCommand => _maxCommand ??= ReactiveCommand.Create(OnMaxClick);

        private ReactiveCommand<Unit, Unit> _selectFromCommand;
        public ReactiveCommand<Unit, Unit> SelectFromCommand => _selectFromCommand ??=
            (_selectFromCommand = ReactiveCommand.CreateFromTask(FromClick));

        private ReactiveCommand<Unit, Unit> _selectToCommand;
        public ReactiveCommand<Unit, Unit> SelectToCommand => _selectToCommand ??=
            (_selectToCommand = ReactiveCommand.CreateFromTask(ToClick));

        private ICommand _undoConfirmStageCommand;
        public ICommand UndoConfirmStageCommand => _undoConfirmStageCommand ??= new Command(() =>
            {
                ConfirmStage = false;

                if (PopupNavigation.Instance.PopupStack.Count > 0)
                    PopupNavigation.Instance.PopAsync();
            });

        private async Task FromClick()
        {
            SelectFromViewModel.SelectAddressFrom = SelectAddressFrom.Change;
            await Navigation.PushAsync(new SelectAddressPage(SelectFromViewModel));
        }

        private async Task ToClick()
        {
            SelectToViewModel.SelectAddressFrom = SelectAddressFrom.Change;
            await Navigation.PushAsync(new SelectAddressPage(SelectToViewModel));
        }

        private async void OnNextCommand()
        {
            var tezosConfig = App.Account
                .Currencies
                .Get<TezosConfig>(TezosConfig.Xtz);

            if (string.IsNullOrEmpty(To))
            {
                ShowMessage(
                    messageType: MessageType.Error,
                    element: RelatedTo.Address,
                    text: AppResources.EmptyAddressError);
                return;
            }

            if (!tezosConfig.IsValidAddress(To))
            {
                ShowMessage(
                    messageType: MessageType.Error,
                    element: RelatedTo.Address,
                    text: AppResources.InvalidAddressError);
                return;
            }

            if (Amount <= 0)
            {
                ShowMessage(
                    messageType: MessageType.Error,
                    element: RelatedTo.Amount,
                    text: AppResources.AmountLessThanZeroError);
                return;
            }

            if (Fee <= 0)
            {
                ShowMessage(
                    messageType: MessageType.Error,
                    element: RelatedTo.Fee,
                    text: AppResources.CommissionLessThanZeroError);
                return;
            }

            if (TokenContract == null || From == null)
            {
                ShowMessage(
                    messageType: MessageType.Error,
                    element: RelatedTo.All,
                    text: "Invalid 'From' address or token contract address");
                return;
            }

            if (!tezosConfig.IsValidAddress(TokenContract))
            {
                ShowMessage(
                   messageType: MessageType.Error,
                   element: RelatedTo.All,
                   text: "Invalid token contract address");
                return;
            }

            var fromTokenAddress = await GetTokenAddressAsync(
                account: App.Account,
                address: From,
                tokenContract: TokenContract,
                tokenId: TokenId,
                tokenType: _tokenType);

            if (fromTokenAddress == null)
            {
                ShowMessage(
                   messageType: MessageType.Error,
                   element: RelatedTo.All,
                   text: $"Insufficient token funds on address {From}. Please update your balance");
                return;
            }

            if (Amount > fromTokenAddress.Balance)
            {
                ShowMessage(
                   messageType: MessageType.Error,
                   element: RelatedTo.All,
                   text: $"Insufficient token funds on address {fromTokenAddress.Address}. " +
                        $"Please use Max button to find out how many tokens you can send");
                return;
            }

            var xtzAddress = await App.Account
                .GetAddressAsync(TezosConfig.Xtz, From);

            if (xtzAddress == null)
            {
                ShowMessage(
                   messageType: MessageType.Error,
                   element: RelatedTo.All,
                   text: $"Insufficient funds for fee. Please update your balance for address {From}");
                return;
            }

            if (xtzAddress.AvailableBalance() < Fee)
            {
                ShowMessage(
                    messageType: MessageType.Error,
                    element: RelatedTo.All,
                    text: "Insufficient funds for fee");
                return;
            }

            if (ConfirmStage)
            {
                try
                {
                    IsLoading = true;
                    this.RaisePropertyChanged(nameof(IsLoading));

                    var error = await Send();

                    if (error != null)
                    {
                        IsLoading = false;
                        this.RaisePropertyChanged(nameof(IsLoading));
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

                    IsLoading = false;
                    this.RaisePropertyChanged(nameof(IsLoading));

                    if (PopupNavigation.Instance.PopupStack.Count > 0)
                        await PopupNavigation.Instance.PopAsync();

                    await PopupNavigation.Instance.PushAsync(new CompletionPopup(
                        new PopupViewModel
                        {
                            Type = PopupType.Success,
                            Title = AppResources.Success,
                            Body = string.Format(CultureInfo.InvariantCulture, AppResources.SuccessSending),
                            ButtonText = AppResources.AcceptButton
                        }));

                    for (int i = Navigation.NavigationStack.Count; i > 3; i--)
                        Navigation.RemovePage(Navigation.NavigationStack[i - 1]);
                }
                catch (Exception e)
                {
                    Log.Error(e, "Transaction send error.");
                    IsLoading = false;
                    this.RaisePropertyChanged(nameof(IsLoading));
                    await PopupNavigation.Instance.PushAsync(new CompletionPopup(
                        new PopupViewModel
                        {
                            Type = PopupType.Error,
                            Title = AppResources.Error,
                            Body = AppResources.SendingTransactionError,
                            ButtonText = AppResources.AcceptButton
                        }));

                    Log.Error(e, "Tezos tokens transaction send error");
                }
                finally
                {
                    ConfirmStage = false;
                }
            }
            else
            {
                ConfirmStage = true;
                await PopupNavigation.Instance.PushAsync(new SendingConfirmationBottomSheet(this));
            }
        }

        private async Task UpdateAmount()
        {
            try
            {
                var tezosConfig = App.Account
                    .Currencies
                    .Get<TezosConfig>(TezosConfig.Xtz);

                if (From == null)
                {
                    ShowMessage(
                        messageType: MessageType.Error,
                        element: RelatedTo.All,
                        text: AppResources.InvalidFromAddress);
                    return;
                }

                if (TokenContract == null || !tezosConfig.IsValidAddress(TokenContract))
                {
                    ShowMessage(
                        messageType: MessageType.Error,
                        element: RelatedTo.All,
                        text: AppResources.InvalidTokenContract);
                    return;
                }

                var fromTokenAddress = await GetTokenAddressAsync(
                    account: App.Account,
                    address: From!,
                    tokenContract: TokenContract,
                    tokenId: TokenId,
                    tokenType: _tokenType);

                if (fromTokenAddress == null)
                {
                    ShowMessage(
                        messageType: MessageType.Error,
                        element: RelatedTo.All,
                        text: AppResources.InsufficientFunds);
                    return;
                }

                if (Amount > fromTokenAddress.Balance)
                {
                    ShowMessage(
                         messageType: MessageType.Error,
                         element: RelatedTo.Amount,
                         text: AppResources.InsufficientFunds,
                         tooltipText: AppResources.BigAmount);
                }
            }
            catch (Exception e)
            {
                Log.Error(e, "Tezos tokens update amount error");
            }
        }

        private async Task UpdateFee()
        {
            try
            {
                var tezosConfig = App.Account
                    .Currencies
                    .Get<TezosConfig>(TezosConfig.Xtz);

                if (From == null)
                {
                    ShowMessage(
                        messageType: MessageType.Error,
                        element: RelatedTo.All,
                        text: AppResources.InvalidFromAddress);
                    return;
                }

                if (TokenContract == null || !tezosConfig.IsValidAddress(TokenContract))
                {
                    ShowMessage(
                        messageType: MessageType.Error,
                        element: RelatedTo.All,
                        text: AppResources.InvalidTokenContract);
                    return;
                }

                if (UseDefaultFee)
                {
                    var fromTokenAddress = await GetTokenAddressAsync(
                        account: App.Account,
                        address: From!,
                        tokenContract: TokenContract,
                        tokenId: TokenId,
                        tokenType: _tokenType);

                    if (fromTokenAddress == null)
                    {
                        ShowMessage(
                            messageType: MessageType.Error,
                            element: RelatedTo.Amount,
                            text: AppResources.InsufficientFunds);
                        return;
                    }

                    var tokenAccount = App.Account
                        .GetTezosTokenAccount<TezosTokenAccount>(fromTokenAddress.Currency, TokenContract, TokenId);

                    var (estimatedFee, isEnougth) = await tokenAccount
                        .EstimateTransferFeeAsync(From);

                    if (!isEnougth)
                    {
                        ShowMessage(
                            messageType: MessageType.Error,
                            element: RelatedTo.All,
                            text: string.Format(AppResources.InsufficientChainFundsWithDetails, "XTZ", estimatedFee));
                        return;
                    }

                    SetFeeFromString(estimatedFee.ToString());
                }
                else
                {
                    var xtzAddress = await App.Account
                        .GetAddressAsync(TezosConfig.Xtz, From);

                    if (xtzAddress == null)
                    {
                        ShowMessage(
                            messageType: MessageType.Error,
                            element: RelatedTo.All,
                            text: string.Format(AppResources.InsufficientChainFunds, "XTZ"));
                        return;
                    }

                    var fee = Math.Min(Fee, tezosConfig.GetMaximumFee());
                    SetFeeFromString(fee.ToString());

                    if (xtzAddress.AvailableBalance() < Fee)
                    {
                        ShowMessage(
                            messageType: MessageType.Error,
                            element: RelatedTo.Amount,
                            text: string.Format(AppResources.InsufficientChainFunds, "XTZ"));
                        return;
                    }

                    if (Fee <= 0)
                    {
                        ShowMessage(
                            messageType: MessageType.Error,
                            element: RelatedTo.Fee,
                            text: AppResources.CommissionLessThanZeroError);
                    }
                }
            }
            catch (Exception e)
            {
                Log.Error(e, "Tezos tokens update fee error");
            }
        }

        protected async void OnMaxClick()
        {
            try
            {
                var tezosConfig = App.Account
                    .Currencies
                    .Get<TezosConfig>(TezosConfig.Xtz);

                if (From == null)
                {
                    SetAmountFromString("0");
                    return;
                }

                if (TokenContract == null || !tezosConfig.IsValidAddress(TokenContract))
                {
                    ShowMessage(
                        messageType: MessageType.Error,
                        element: RelatedTo.All,
                        text: AppResources.InvalidTokenContract);
                    SetAmountFromString("0");
                    return;
                }

                var fromTokenAddress = await GetTokenAddressAsync(
                    account: App.Account,
                    address: From,
                    tokenContract: TokenContract,
                    tokenId: TokenId,
                    tokenType: _tokenType);

                if (fromTokenAddress == null)
                {
                    ShowMessage(
                        messageType: MessageType.Error,
                        element: RelatedTo.All,
                        text: AppResources.InsufficientFunds);
                    SetAmountFromString("0");
                    return;
                }

                var tokenAccount = App.Account
                    .GetTezosTokenAccount<TezosTokenAccount>(fromTokenAddress.Currency, TokenContract, TokenId);

                var (estimatedFee, isEnougth) = await tokenAccount
                    .EstimateTransferFeeAsync(From);

                if (!isEnougth)
                {
                    ShowMessage(
                        messageType: MessageType.Error,
                        element: RelatedTo.All,
                        text: string.Format(AppResources.InsufficientChainFundsWithDetails, "XTZ", estimatedFee));
                    return;
                }

                var amount = fromTokenAddress.Balance;
                SetAmountFromString(amount.ToString());
            }
            catch (Exception e)
            {
                Log.Error(e, "Tezos tokens max click error");
            }
        }

        protected void ConfirmFromAddress(SelectAddressViewModel selectAddressViewModel, WalletAddressViewModel walletAddressViewModel)
        {
            From = walletAddressViewModel?.Address;
            TokenId = walletAddressViewModel?.TokenId ?? 0;
            SelectedFromBalance = walletAddressViewModel?.Balance ?? 0;

            switch (selectAddressViewModel.SelectAddressFrom)
            {
                case SelectAddressFrom.Init:
                    Navigation.PushAsync(new SelectAddressPage(SelectToViewModel));
                    break;

                case SelectAddressFrom.Change:
                    Navigation.PopAsync();
                    break;

                case SelectAddressFrom.InitSearch:
                    Navigation.PushAsync(new SelectAddressPage(SelectToViewModel));
                    Navigation.RemovePage(Navigation.NavigationStack[Navigation.NavigationStack.Count - 2]);
                    break;

                case SelectAddressFrom.ChangeSearch:
                    Navigation.RemovePage(Navigation.NavigationStack[Navigation.NavigationStack.Count - 2]);
                    Navigation.PopAsync();
                    break;
            }
        }

        protected void ConfirmToAddress(SelectAddressViewModel selectAddressViewModel, WalletAddressViewModel walletAddressViewModel)
        {
            To = walletAddressViewModel?.Address;

            switch (selectAddressViewModel.SelectAddressFrom)
            {
                case SelectAddressFrom.Init:
                    Navigation.PushAsync(new SendPage(this));
                    break;

                case SelectAddressFrom.Change:
                    Navigation.PopAsync();
                    break;

                case SelectAddressFrom.InitSearch:
                    Navigation.PushAsync(new SendPage(this));
                    Navigation.RemovePage(Navigation.NavigationStack[Navigation.NavigationStack.Count - 2]);
                    break;

                case SelectAddressFrom.ChangeSearch:
                    Navigation.RemovePage(Navigation.NavigationStack[Navigation.NavigationStack.Count - 2]);
                    Navigation.PopAsync();
                    break;
            }
        }

        protected void OnQuotesUpdatedEventHandler(object sender, EventArgs args)
        {
            if (sender is not ICurrencyQuotesProvider quotesProvider)
                return;

            var quote = quotesProvider.GetQuote(CurrencyCode, BaseCurrencyCode);
            var feeQuote = quotesProvider.GetQuote(FeeCurrencyCode, BaseCurrencyCode);

            Device.InvokeOnMainThreadAsync(() =>
            {
                AmountInBase = Amount * (quote?.Bid ?? 0m);
                FeeInBase = Fee * (feeQuote?.Bid ?? 0m);
            });
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

        private async void UpdateCurrencyCode()
        {
            if (TokenContract == null || From == null)
                return;

            var tokenAddress = await GetTokenAddressAsync(
                account: App.Account,
                address: From,
                tokenContract: TokenContract,
                tokenId: TokenId,
                tokenType: _tokenType);

            if (tokenAddress?.TokenBalance?.Symbol != null)
            {
                CurrencyCode = tokenAddress.TokenBalance.Symbol.ToUpper();
            }
            else
            {
                CurrencyCode = App.Account.Currencies
                    .FirstOrDefault(c => c is Fa12Config fa12 && fa12.TokenContractAddress == TokenContract)
                    ?.Name.ToUpper() ?? "TOKENS";
            }

            SelectedFromBalance = tokenAddress?.AvailableBalance() ?? 0;
            this.RaisePropertyChanged(nameof(AmountString));
        }

        private async Task<Error> Send(CancellationToken cancellationToken = default)
        {
            var tokenAddress = await GetTokenAddressAsync(
                account: App.Account,
                address: From,
                tokenContract: TokenContract,
                tokenId: TokenId,
                tokenType: _tokenType);

            if (tokenAddress.Currency == "FA12")
            {
                var currencyName = App.Account.Currencies
                    .FirstOrDefault(c => c is Fa12Config fa12 && fa12.TokenContractAddress == TokenContract)
                    ?.Name ?? "FA12";

                var tokenAccount = App.Account.GetTezosTokenAccount<Fa12Account>(
                    currency: currencyName,
                    tokenContract: TokenContract,
                    tokenId: TokenId);

                return await tokenAccount.SendAsync(
                    from: tokenAddress.Address,
                    to: To,
                    amount: Amount,
                    fee: Fee,
                    useDefaultFee: UseDefaultFee,
                    cancellationToken: cancellationToken);
            }
            else
            {
                var tokenAccount = App.Account.GetTezosTokenAccount<Fa2Account>(
                    currency: "FA2",
                    tokenContract: TokenContract,
                    tokenId: TokenId);

                var decimals = tokenAddress.TokenBalance.Decimals;
                var amount = Amount * (decimal)Math.Pow(10, decimals);
                var fee = (int)Fee.ToMicroTez();

                return await tokenAccount.SendAsync(
                    from: From,
                    to: To,
                    amount: amount,
                    tokenContract: TokenContract,
                    tokenId: (int)TokenId,
                    fee: fee,
                    useDefaultFee: UseDefaultFee,
                    cancellationToken: cancellationToken);
            }
        }

        protected void ShowMessage(MessageType messageType, RelatedTo element, string text, string tooltipText = null)
        {
            Message.Type = messageType;
            Message.RelatedTo = element;
            Message.Text = text;
            Message.TooltipText = tooltipText;

            this.RaisePropertyChanged(nameof(Message));
        }
    }
}
