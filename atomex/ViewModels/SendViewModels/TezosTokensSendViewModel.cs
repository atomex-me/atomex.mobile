﻿using System;
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
using atomex.Views;
using atomex.Views.Send;
using Atomex;
using Atomex.Common;
using Atomex.Core;
using Atomex.MarketData.Abstract;
using Atomex.TezosTokens;
using Atomex.ViewModels;
using atomex.ViewModels.Abstract;
using Atomex.Wallet.Abstract;
using Atomex.Wallet.Tezos;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Serilog;
using Xamarin.Forms;
using static atomex.Models.Message;

namespace atomex.ViewModels.SendViewModels
{
    public class TezosTokensSendViewModel : BaseViewModel, IDisposable
    {
        private IAtomexApp _app;
        private INavigationService _navigationService;

        public const string DefaultBaseCurrencyCode = "USD";
        public const string DefaultBaseCurrencyFormat = "$0.00";
        public const bool IsToken = true;

        [Reactive] public decimal SelectedFromBalance { get; set; }
        [Reactive] public string From { get; set; }
        [Reactive] public string TokenContract { get; set; }
        [Reactive] public int TokenId { get; set; }
        [Reactive] public string To { get; set; }
        [Reactive] public ImageSource TokenPreview { get; set; }
        private readonly string _tokenType;

        [Reactive] public decimal Amount { get; set; }
        [ObservableAsProperty] public string TotalAmountString { get; }

        public virtual string AmountString
        {
            get => Amount.ToString(CultureInfo.InvariantCulture);
            set
            {
                var temp = value.Replace(",", ".");
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

                Device.InvokeOnMainThreadAsync(() => this.RaisePropertyChanged(nameof(Amount)));
            }
        }

        public void SetAmountFromString(string value)
        {
            if (value == AmountString)
            {
                this.RaisePropertyChanged(nameof(AmountString));
                return;
            }

            var temp = value.Replace(",", ".");
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
                AmountString = amount > long.MaxValue 
                    ? long.MaxValue.ToString() 
                    : value;
            }

            this.RaisePropertyChanged(nameof(AmountString));
        }

        [Reactive] public decimal Fee { get; set; }

        public string FeeString
        {
            get => Fee.ToString(CultureInfo.InvariantCulture);
            set
            {
                var temp = value.Replace(",", ".");
                Fee = !decimal.TryParse(
                    s: temp,
                    style: NumberStyles.AllowDecimalPoint,
                    provider: CultureInfo.InvariantCulture,
                    result: out var fee) ? 0 : fee;

                Device.InvokeOnMainThreadAsync(() => this.RaisePropertyChanged(nameof(Fee)));
            }
        }

        public void SetFeeFromString(string value)
        {
            if (value == FeeString)
            {
                this.RaisePropertyChanged(nameof(FeeString));
                return;
            }

            var temp = value.Replace(",", ".");
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
                FeeString = amount > long.MaxValue 
                    ? long.MaxValue.ToString()
                    : value;
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
            INavigationService navigationService,
            string tokenContract,
            int tokenId,
            string tokenType,
            ImageSource tokenPreview,
            string from = null)
        {
            _app = app ?? throw new ArgumentNullException(nameof(app));
            _navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));
            Message = new Message();

            var tezosConfig = _app.Account
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
                .Select(totalAmount => totalAmount.ToString(CultureInfo.CurrentCulture))
                .ToPropertyExInMainThread(this, vm => vm.TotalAmountString);

            this.WhenAnyValue(
                    vm => vm.Amount,
                    vm => vm.Fee)
                .Subscribe(_ => OnQuotesUpdatedEventHandler(_app.QuotesProvider, EventArgs.Empty));

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
                                  (string.IsNullOrEmpty(Message.Text) ||
                                   (Message != null && Message.Type != MessageType.Error)) &&
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
                account: _app.Account,
                currency: tezosConfig,
                navigationService: _navigationService,
                mode: SelectAddressMode.SendFrom,
                selectedAddress: from,
                selectedTokenId: tokenId,
                tokenContract: tokenContract)
            {
                ConfirmAction = ConfirmFromAddress
            };

            SelectToViewModel = new SelectAddressViewModel(
                account: _app.Account,
                currency: tezosConfig,
                navigationService: _navigationService,
                selectedTokenId: tokenId,
                tokenContract: tokenContract)
            {
                ConfirmAction = ConfirmToAddress
            };
        }

        private void SubscribeToServices()
        {
            if (_app.Account?.Network == Network.TestNet)
                return;
            
            if (_app.HasQuotesProvider)
                _app.QuotesProvider.QuotesUpdated += OnQuotesUpdatedEventHandler;
        }

        private ReactiveCommand<Unit, Unit> _nextCommand;
        public ReactiveCommand<Unit, Unit> NextCommand => _nextCommand ??= ReactiveCommand.Create(OnNextCommand);

        private ReactiveCommand<Unit, Unit> _maxCommand;
        public ReactiveCommand<Unit, Unit> MaxCommand => _maxCommand ??= ReactiveCommand.Create(OnMaxClick);

        private ReactiveCommand<Unit, Unit> _selectFromCommand;

        public ReactiveCommand<Unit, Unit> SelectFromCommand => _selectFromCommand ??=
            (_selectFromCommand = ReactiveCommand.Create(FromClick));

        private ReactiveCommand<Unit, Unit> _selectToCommand;

        public ReactiveCommand<Unit, Unit> SelectToCommand => _selectToCommand ??=
            (_selectToCommand = ReactiveCommand.Create(ToClick));

        private ICommand _undoConfirmStageCommand;
        public ICommand UndoConfirmStageCommand => _undoConfirmStageCommand ??= new Command(() => ConfirmStage = false);

        private ICommand _closeConfirmationCommand;

        public ICommand CloseConfirmationCommand =>
            _closeConfirmationCommand ??= new Command(() => _navigationService?.ClosePopup());

        private void FromClick()
        {
            SelectFromViewModel.SelectAddressFrom = SelectAddressFrom.Change;
            _navigationService?.ShowPage(new SelectAddressPage(SelectFromViewModel), TabNavigation.Portfolio);
        }

        private void ToClick()
        {
            SelectToViewModel.SelectAddressFrom = SelectAddressFrom.Change;
            _navigationService?.ShowPage(new SelectAddressPage(SelectToViewModel), TabNavigation.Portfolio);
        }

        private async void OnNextCommand()
        {
            var tezosConfig = _app.Account
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
                account: _app.Account,
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

            var xtzAddress = await _app.Account
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
                if (IsLoading)
                    return;

                IsLoading = true;
                this.RaisePropertyChanged(nameof(IsLoading));

                try
                {
                    var error = await Send();

                    await Device.InvokeOnMainThreadAsync(async () =>
                    {
                        if (error != null)
                        {
                            _navigationService?.DisplaySnackBar(
                                SnackbarMessage.MessageType.Error,
                                error.Description);
                            return;
                        }

                        _navigationService?.ClosePopup();
                        await _navigationService!.ReturnToInitiatedPage(TabNavigation.Portfolio);

                        _navigationService?.DisplaySnackBar(
                            SnackbarMessage.MessageType.Success,
                            string.Format(CultureInfo.InvariantCulture, AppResources.SuccessSending));
                    });
                }
                catch (Exception e)
                {
                    await Device.InvokeOnMainThreadAsync(() =>
                        _navigationService?.DisplaySnackBar(
                            SnackbarMessage.MessageType.Error,
                            AppResources.SendingTransactionError));
                    Log.Error(e, "Tezos tokens transaction send error");
                }
                finally
                {
                    ConfirmStage = false;
                    IsLoading = false;
                    this.RaisePropertyChanged(nameof(IsLoading));
                }
            }
            else
            {
                ConfirmStage = true;
                _navigationService?.ShowPopup(new SendingConfirmationBottomSheet(this));
            }
        }

        private async Task UpdateAmount()
        {
            try
            {
                var tezosConfig = _app.Account
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
                    account: _app.Account,
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
                var tezosConfig = _app.Account
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
                        account: _app.Account,
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

                    var tokenAccount = _app.Account
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

                    SetFeeFromString(estimatedFee.ToString(CultureInfo.CurrentCulture));
                }
                else
                {
                    var xtzAddress = await _app.Account
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
                    SetFeeFromString(fee.ToString(CultureInfo.CurrentCulture));

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
                var tezosConfig = _app.Account
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
                    account: _app.Account,
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

                var tokenAccount = _app.Account
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
                SetAmountFromString(amount.ToString(CultureInfo.CurrentCulture));
            }
            catch (Exception e)
            {
                Log.Error(e, "Tezos tokens max click error");
            }
        }

        protected void ConfirmFromAddress(SelectAddressViewModel selectAddressViewModel,
            WalletAddressViewModel walletAddressViewModel)
        {
            From = walletAddressViewModel?.Address;
            TokenId = walletAddressViewModel?.TokenId ?? 0;
            SelectedFromBalance = walletAddressViewModel?.Balance ?? 0;

            switch (selectAddressViewModel.SelectAddressFrom)
            {
                case SelectAddressFrom.Init:
                    _navigationService?.ShowPage(new SelectAddressPage(SelectToViewModel), TabNavigation.Portfolio);
                    break;

                case SelectAddressFrom.Change:
                    _navigationService?.ClosePage(TabNavigation.Portfolio);
                    break;

                case SelectAddressFrom.InitSearch:
                    _navigationService?.ShowPage(new SelectAddressPage(SelectToViewModel), TabNavigation.Portfolio);
                    _navigationService?.RemovePreviousPage(TabNavigation.Portfolio);
                    break;

                case SelectAddressFrom.ChangeSearch:
                    _navigationService?.RemovePreviousPage(TabNavigation.Portfolio);
                    _navigationService?.ClosePage(TabNavigation.Portfolio);
                    break;
            }
        }

        protected void ConfirmToAddress(SelectAddressViewModel selectAddressViewModel,
            WalletAddressViewModel walletAddressViewModel)
        {
            To = walletAddressViewModel?.Address;

            switch (selectAddressViewModel.SelectAddressFrom)
            {
                case SelectAddressFrom.Init:
                    _navigationService?.ShowPage(new SendPage(this), TabNavigation.Portfolio);
                    break;

                case SelectAddressFrom.Change:
                    _navigationService?.ClosePage(TabNavigation.Portfolio);
                    break;

                case SelectAddressFrom.InitSearch:
                    _navigationService?.ShowPage(new SendPage(this), TabNavigation.Portfolio);
                    _navigationService?.RemovePreviousPage(TabNavigation.Portfolio);
                    break;

                case SelectAddressFrom.ChangeSearch:
                    _navigationService?.RemovePreviousPage(TabNavigation.Portfolio);
                    _navigationService?.ClosePage(TabNavigation.Portfolio);
                    break;
            }
        }

        protected void OnQuotesUpdatedEventHandler(object sender, EventArgs args)
        {
            try
            {
                if (sender is not IQuotesProvider quotesProvider)
                    return;
                
                if (_app.Account?.Network == Network.TestNet)
                    return;

                var quote = quotesProvider.GetQuote(CurrencyCode, BaseCurrencyCode);
                var feeQuote = quotesProvider.GetQuote(FeeCurrencyCode, BaseCurrencyCode);
                if (quote == null || feeQuote == null) return;

                Device.InvokeOnMainThreadAsync(() =>
                {
                    AmountInBase = Amount * (quote?.Bid ?? 0m);
                    FeeInBase = Fee * (feeQuote?.Bid ?? 0m);
                });
            }
            catch (Exception e)
            {
                Log.Error("Update quote error for {@Token} sending", CurrencyCode);
            }
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
                account: _app.Account,
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
                CurrencyCode = _app.Account.Currencies
                    .FirstOrDefault(c => c is Fa12Config fa12 && fa12.TokenContractAddress == TokenContract)
                    ?.Name.ToUpper() ?? "TOKENS";
            }

            SelectedFromBalance = tokenAddress?.AvailableBalance() ?? 0;
            this.RaisePropertyChanged(nameof(AmountString));
        }

        private async Task<Error> Send(CancellationToken cancellationToken = default)
        {
            var tokenAddress = await GetTokenAddressAsync(
                account: _app.Account,
                address: From,
                tokenContract: TokenContract,
                tokenId: TokenId,
                tokenType: _tokenType);

            var currencyName = _app.Account.Currencies
                .FirstOrDefault(c => c is TezosTokenConfig tokenConfig &&
                                     tokenConfig.TokenContractAddress == TokenContract &&
                                     tokenConfig.TokenId == TokenId)
                ?.Name ?? _tokenType;

            var tokenAccount = _app.Account.GetTezosTokenAccount<TezosTokenAccount>(
                currency: currencyName,
                tokenContract: TokenContract,
                tokenId: TokenId);

            var (_, error) = await tokenAccount.SendAsync(
                from: tokenAddress.Address,
                to: To,
                amount: Amount,
                fee: Fee,
                useDefaultFee: UseDefaultFee,
                cancellationToken: cancellationToken);

            return error;
        }

        protected void ShowMessage(MessageType messageType, RelatedTo element, string text, string tooltipText = null)
        {
            Message.Type = messageType;
            Message.RelatedElement = element;
            Message.Text = text;
            Message.TooltipText = tooltipText;

            this.RaisePropertyChanged(nameof(Message));
        }

        public void Dispose()
        {
            if (_app.Account?.Network == Network.TestNet)
                return;
            
            if (_app.HasQuotesProvider)
                _app.QuotesProvider.QuotesUpdated -= OnQuotesUpdatedEventHandler;
        }
    }
}