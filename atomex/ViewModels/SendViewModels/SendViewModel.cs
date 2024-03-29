﻿using System;
using System.Globalization;
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
using Atomex.ViewModels;
using atomex.ViewModels.Abstract;
using atomex.ViewModels.CurrencyViewModels;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Serilog;
using Xamarin.Forms;
using static atomex.Models.Message;

namespace atomex.ViewModels.SendViewModels
{
    public abstract class SendViewModel : BaseViewModel, IDisposable
    {
        protected IAtomexApp App { get; }
        protected INavigationService NavigationService { get; }
        protected CurrencyConfig Currency { get; set; }
        public BaseViewModel SelectFromViewModel { get; set; }
        public SelectAddressViewModel SelectToViewModel { get; set; }
        [Reactive] public CurrencyViewModel CurrencyViewModel { get; set; }
        [Reactive] public string From { get; set; }
        [Reactive] public decimal SelectedFromBalance { get; set; }
        [Reactive] public string To { get; set; }
        [Reactive] public decimal Amount { get; set; }
        [ObservableAsProperty] public string TotalAmountString { get; }

        public string AmountString
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

        [Reactive] public bool UseDefaultFee { get; set; }
        [Reactive] public decimal AmountInBase { get; set; }
        [Reactive] public decimal FeeInBase { get; set; }
        [Reactive] public decimal TotalAmountInBase { get; set; }

        [Reactive] public SendStage Stage { get; set; }
        [Reactive] public Message Message { get; set; }
        [Reactive] public bool IsLoading { get; set; }

        public string CurrencyCode => CurrencyViewModel.CurrencyCode;
        public string CurrencyName => CurrencyViewModel.CurrencyName;
        public string FeeCurrencyCode => CurrencyViewModel.FeeCurrencyCode;
        public string BaseCurrencyCode => CurrencyViewModel.BaseCurrencyCode;
        public bool IsToken => CurrencyViewModel.Currency.IsToken;

        public string AmountEntryPlaceholderString => $"{AppResources.EnterAmountLabel}, {CurrencyCode}";
        public string FeeEntryPlaceholderString => $"{AppResources.FeeLabel}, {FeeCurrencyCode}";

        protected abstract Task UpdateAmount();

        protected virtual Task UpdateFee()
        {
            return Task.CompletedTask;
        }

        protected abstract Task OnMaxClick();
        protected abstract Task<Error> Send(CancellationToken cancellationToken = default);
        protected abstract void FromClick();
        protected abstract void ToClick();

        [Reactive] public decimal RecommendedMaxAmount { get; set; }
        [Reactive] public Message RecommendedMaxAmountWarning { get; set; }
        public bool ShowAdditionalConfirmation { get; set; }
        [Reactive] public bool UseRecommendedAmount { get; set; }
        [Reactive] public bool UseEnteredAmount { get; set; }
        [Reactive] public bool CanSend { get; set; }

        public decimal AmountToSend => UseRecommendedAmount && (RecommendedMaxAmount < Amount)
            ? RecommendedMaxAmount
            : Amount;

        [Reactive] public string SendRecommendedAmountMenu { get; set; }
        [Reactive] public string SendEnteredAmountMenu { get; set; }

        public SendViewModel(IAtomexApp app, CurrencyViewModel currencyViewModel, INavigationService navigationService)
        {
            App = app ?? throw new ArgumentNullException(nameof(app));
            NavigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));
            CurrencyViewModel = currencyViewModel ?? throw new ArgumentNullException(nameof(currencyViewModel));
            Currency = currencyViewModel?.Currency;

            Message = new Message();
            RecommendedMaxAmountWarning = new Message();
            UseDefaultFee = true;

            var updateAmountCommand = ReactiveCommand.CreateFromTask(UpdateAmount);
            var updateFeeCommand = ReactiveCommand.CreateFromTask(UpdateFee);

            this.WhenAnyValue(
                    vm => vm.From,
                    vm => vm.To,
                    vm => vm.Amount,
                    vm => vm.Fee
                )
                .SubscribeInMainThread(_ =>
                {
                    Message.Text = string.Empty;
                    this.RaisePropertyChanged(nameof(Message));
                });

            this.WhenAnyValue(
                    vm => vm.Amount,
                    vm => vm.Fee,
                    (amount, fee) => Currency.IsToken ? amount : amount + fee)
                .Select(totalAmount => totalAmount.ToString(CultureInfo.CurrentCulture))
                .ToPropertyExInMainThread(this, vm => vm.TotalAmountString);

            this.WhenAnyValue(
                    vm => vm.Amount,
                    vm => vm.From,
                    vm => vm.To,
                    (amount, from, to) => from
                )
                .WhereNotNull()
                .Select(_ => Unit.Default)
                .InvokeCommandInMainThread(updateAmountCommand);

            this.WhenAnyValue(
                    vm => vm.Amount,
                    vm => vm.Fee
                )
                .Subscribe(_ => OnQuotesUpdatedEventHandler(App.QuotesProvider, EventArgs.Empty));

            this.WhenAnyValue(vm => vm.Fee)
                .Select(_ => Unit.Default)
                .InvokeCommandInMainThread(updateFeeCommand);

            this.WhenAnyValue(vm => vm.UseDefaultFee)
                .Where(useDefaultFee => useDefaultFee && !string.IsNullOrEmpty(From))
                .Select(_ => Unit.Default)
                .InvokeCommandInMainThread(updateAmountCommand);

            var canSendObservable1 = this.WhenAnyValue(
                vm => vm.Amount,
                vm => vm.To,
                vm => vm.Message,
                vm => vm.RecommendedMaxAmountWarning);

            var canSendObservable2 = this.WhenAnyValue(
                vm => vm.UseRecommendedAmount,
                vm => vm.UseEnteredAmount,
                vm => vm.Stage);

            canSendObservable1.CombineLatest(canSendObservable2)
                .Throttle(TimeSpan.FromMilliseconds(1))
                .SubscribeInMainThread(_ =>
                {
                    if (Stage == SendStage.Edit)
                    {
                        CanSend = To != null &&
                                  Amount > 0 &&
                                  string.IsNullOrEmpty(Message.Text) &&
                                  (string.IsNullOrEmpty(RecommendedMaxAmountWarning.Text) ||
                                   (RecommendedMaxAmountWarning != null &&
                                    RecommendedMaxAmountWarning.Type != MessageType.Error) &&
                                   !IsLoading);
                    }
                    else if (Stage == SendStage.Confirmation)
                    {
                        CanSend = true;
                    }
                    else
                    {
                        CanSend = UseRecommendedAmount || UseEnteredAmount;
                    }
                });

            this.WhenAnyValue(vm => vm.Stage)
                .SubscribeInMainThread(s =>
                {
                    UseRecommendedAmount = false;
                    UseEnteredAmount = false;
                });

            this.WhenAnyValue(vm => vm.RecommendedMaxAmount)
                .SubscribeInMainThread(a =>
                {
                    SendRecommendedAmountMenu = string.Format(
                        AppResources.SendRecommendedAmountMenu,
                        RecommendedMaxAmount,
                        Currency.Name);
                });

            this.WhenAnyValue(vm => vm.Amount)
                .SubscribeInMainThread(a =>
                {
                    SendEnteredAmountMenu = string.Format(
                        AppResources.SendEnteredAmountMenu,
                        Amount,
                        Currency.Name);
                });

            SubscribeToServices();
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

        private void SubscribeToServices()
        {
            if (App.Account?.Network == Network.TestNet)
                return;
            
            if (App.HasQuotesProvider)
                App.QuotesProvider.QuotesUpdated += OnQuotesUpdatedEventHandler;
        }

        private ICommand _undoConfirmStageCommand;

        public ICommand UndoConfirmStageCommand => _undoConfirmStageCommand ??= new Command(() =>
            Stage = NavigationService.HasMultipleBottomSheets() 
                ? Stage 
                : SendStage.Edit);

        private ICommand _closeConfirmationCommand;

        public ICommand CloseConfirmationCommand =>
            _closeConfirmationCommand ??= new Command(() => NavigationService?.ClosePopup());

        private ReactiveCommand<Unit, Unit> _maxCommand;

        public ReactiveCommand<Unit, Unit> MaxCommand =>
            _maxCommand ??= (_maxCommand = ReactiveCommand.CreateFromTask(async () =>
            {
                Message.Text = string.Empty;
                await OnMaxClick();
                this.RaisePropertyChanged(nameof(Message));
            }));

        private ReactiveCommand<Unit, Unit> _showRecommendedMaxAmountTooltip;

        public ReactiveCommand<Unit, Unit> ShowRecommendedMaxAmountTooltip =>
            _showRecommendedMaxAmountTooltip ??= (_showRecommendedMaxAmountTooltip =
                ReactiveCommand.CreateFromTask(() => NavigationService?.ShowAlert(
                    AppResources.Warning, 
                    RecommendedMaxAmountWarning?.TooltipText,
                    AppResources.AcceptButton)));

        private ReactiveCommand<Unit, Unit> _selectFromCommand;

        public ReactiveCommand<Unit, Unit> SelectFromCommand => _selectFromCommand ??=
            (_selectFromCommand = ReactiveCommand.Create(FromClick));

        private ReactiveCommand<Unit, Unit> _selectToCommand;

        public ReactiveCommand<Unit, Unit> SelectToCommand => _selectToCommand ??=
            (_selectToCommand = ReactiveCommand.Create(ToClick));

        private ReactiveCommand<Unit, Unit> _nextCommand;

        public ReactiveCommand<Unit, Unit> NextCommand => _nextCommand ??=
            (_nextCommand = ReactiveCommand.CreateFromTask(OnNextCommand));

        private async Task OnNextCommand()
        {
            if (string.IsNullOrEmpty(To))
                ShowMessage(
                    messageType: MessageType.Error,
                    element: RelatedTo.Address,
                    text: AppResources.EmptyAddressError);

            else if (!Currency.IsValidAddress(To))
                ShowMessage(
                    messageType: MessageType.Error,
                    element: RelatedTo.Address,
                    text: AppResources.InvalidAddressError);

            else if (Amount <= 0)
                ShowMessage(
                    messageType: MessageType.Error,
                    element: RelatedTo.Amount,
                    text: AppResources.AmountLessThanZeroError);

            else if (Fee <= 0)
                ShowMessage(
                    messageType: MessageType.Error,
                    element: RelatedTo.Fee,
                    text: AppResources.CommissionLessThanZeroError);

            var feeAmount = !Currency.IsToken ? Fee : 0;

            if (Amount + feeAmount > CurrencyViewModel.AvailableAmount)
                ShowMessage(
                    messageType: MessageType.Error,
                    element: RelatedTo.Amount,
                    text: AppResources.AvailableFundsError);

            if (!string.IsNullOrEmpty(Message?.Text)) return;

            if ((Stage == SendStage.Confirmation && !ShowAdditionalConfirmation) ||
                Stage == SendStage.AdditionalConfirmation)
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
                            NavigationService?.DisplaySnackBar(
                                SnackbarMessage.MessageType.Error,
                                error.Description);
                            return;
                        }

                        NavigationService?.ClosePopup();
                        await NavigationService!.ReturnToInitiatedPage(TabNavigation.Portfolio);

                        NavigationService?.DisplaySnackBar(
                            SnackbarMessage.MessageType.Success,
                            string.Format(CultureInfo.InvariantCulture, AppResources.SuccessSending));
                    });
                }
                catch (Exception e)
                {
                    Log.Error(e, "Transaction send error");
                    await Device.InvokeOnMainThreadAsync(() =>
                        NavigationService?.DisplaySnackBar(
                            SnackbarMessage.MessageType.Error,
                            AppResources.SendingTransactionError));
                }
                finally
                {
                    IsLoading = false;
                    this.RaisePropertyChanged(nameof(IsLoading));
                    Stage = SendStage.Edit;
                }
            }
            else if (Stage == SendStage.Confirmation && ShowAdditionalConfirmation)
            {
                Stage = SendStage.AdditionalConfirmation;
                NavigationService?.ShowPopup(new WarningConfirmationBottomSheet(this));
            }
            else
            {
                Stage = SendStage.Confirmation;
                NavigationService?.ShowPopup(new SendingConfirmationBottomSheet(this));
            }
        }

        protected void ConfirmFromAddress(SelectAddressViewModel selectAddressViewModel,
            WalletAddressViewModel walletAddressViewModel)
        {
            From = walletAddressViewModel?.Address;
            SelectedFromBalance = walletAddressViewModel?.Balance ?? 0;

            switch (selectAddressViewModel.SelectAddressFrom)
            {
                case SelectAddressFrom.Init:
                    NavigationService?.ShowPage(new SelectAddressPage(SelectToViewModel), TabNavigation.Portfolio);
                    break;

                case SelectAddressFrom.Change:
                    NavigationService?.ClosePage(TabNavigation.Portfolio);
                    break;

                case SelectAddressFrom.InitSearch:
                    NavigationService?.ShowPage(new SelectAddressPage(SelectToViewModel), TabNavigation.Portfolio);
                    NavigationService?.RemovePreviousPage(TabNavigation.Portfolio);
                    break;

                case SelectAddressFrom.ChangeSearch:
                    NavigationService?.RemovePreviousPage(TabNavigation.Portfolio);
                    NavigationService?.ClosePage(TabNavigation.Portfolio);
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
                    NavigationService?.ShowPage(new SendPage(this), TabNavigation.Portfolio);
                    break;

                case SelectAddressFrom.Change:
                    NavigationService?.ClosePage(TabNavigation.Portfolio);
                    break;

                case SelectAddressFrom.InitSearch:
                    NavigationService?.ShowPage(new SendPage(this), TabNavigation.Portfolio);
                    NavigationService?.RemovePreviousPage(TabNavigation.Portfolio);
                    break;

                case SelectAddressFrom.ChangeSearch:
                    NavigationService?.RemovePreviousPage(TabNavigation.Portfolio);
                    NavigationService?.ClosePage(TabNavigation.Portfolio);
                    break;
            }
        }

        protected virtual void OnQuotesUpdatedEventHandler(object sender, EventArgs args)
        {
            if (sender is not IQuotesProvider quotesProvider)
                return;
            
            if (App.Account?.Network == Network.TestNet)
                return;

            try
            {
                var quote = quotesProvider.GetQuote(CurrencyCode, BaseCurrencyCode);
                if (quote == null) return;

                Device.InvokeOnMainThreadAsync(() =>
                {
                    AmountInBase = Amount * (quote?.Bid ?? 0m);
                    FeeInBase = Fee * (quote?.Bid ?? 0m);
                    TotalAmountInBase = AmountInBase + FeeInBase;
                });
            }
            catch (Exception e)
            {
                Log.Error(e, "Update quotes error for {@Currency} sending", CurrencyCode);
            }
        }

        protected void ShowMessage(MessageType messageType, RelatedTo element, string text, string tooltipText = null)
        {
            Message.Type = messageType;
            Message.RelatedElement = element;
            Message.Text = text;
            Message.TooltipText = tooltipText;

            this.RaisePropertyChanged(nameof(Message));
        }

        protected void SetRecommededAmountWarning(MessageType messageType, RelatedTo element, string text,
            string tooltipText = null)
        {
            RecommendedMaxAmountWarning.Type = messageType;
            RecommendedMaxAmountWarning.RelatedElement = element;
            RecommendedMaxAmountWarning.Text = text;
            RecommendedMaxAmountWarning.TooltipText = tooltipText;

            this.RaisePropertyChanged(nameof(RecommendedMaxAmountWarning));
        }
        
        public void Dispose()
        {
            if (App.Account?.Network == Network.TestNet)
                return;
            
            if (App.HasQuotesProvider)
                App.QuotesProvider.QuotesUpdated -= OnQuotesUpdatedEventHandler;
        }
    }
}