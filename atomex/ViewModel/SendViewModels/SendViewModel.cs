using System;
using System.Globalization;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using atomex.Resources;
using atomex.ViewModel.CurrencyViewModels;
using atomex.Views.Popup;
using atomex.Views.Send;
using Atomex;
using Atomex.Core;
using Atomex.MarketData.Abstract;
using Atomex.ViewModels;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Rg.Plugins.Popup.Services;
using Serilog;
using Xamarin.Forms;

namespace atomex.ViewModel.SendViewModels
{
    public enum MessageType
    {
        Warning,
        Error
    }

    public enum RelatedTo
    {
        Amount,
        Fee,
        Address,
        Core
    }

    public class Message
    {
        public MessageType Type { get; set; }
        public RelatedTo RelatedTo { get; set; }
        public string Text { get; set; }
    }

    public abstract class SendViewModel : BaseViewModel
    {
        protected IAtomexApp App { get; }
        protected INavigation Navigation { get; set; }

        protected CurrencyConfig Currency { get; set; }
        public BaseViewModel SelectFromViewModel { get; set; }
        protected SelectAddressViewModel SelectToViewModel { get; set; }
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

        [Reactive] public bool UseDefaultFee { get; set; }
        [Reactive] public decimal AmountInBase { get; set; }
        [Reactive] public decimal FeeInBase { get; set; }
        [Reactive] public decimal TotalAmountInBase { get; set; }

        [Reactive] public Message Message { get; set; }
        [Reactive] public bool IsLoading { get; set; }

        public string CurrencyCode => CurrencyViewModel.CurrencyCode;
        public string FeeCurrencyCode => CurrencyViewModel.FeeCurrencyCode;
        public string BaseCurrencyCode => CurrencyViewModel.BaseCurrencyCode;

        public string AmountEntryPlaceholderString => $"{AppResources.EnterAmountLabel}, {CurrencyCode}";
        public string FeeEntryPlaceholderString => $"{AppResources.FeeLabel}, {FeeCurrencyCode}";

        protected abstract Task UpdateAmount();
        protected virtual Task UpdateFee() { return Task.CompletedTask; }
        protected abstract Task OnMaxClick();
        protected abstract Task<Error> Send(CancellationToken cancellationToken = default);
        protected abstract Task FromClick();
        protected abstract Task ToClick();

        public SendViewModel(IAtomexApp app, CurrencyViewModel currencyViewModel)
        {
            App = app ?? throw new ArgumentNullException(nameof(AtomexApp));

            CurrencyViewModel = currencyViewModel ?? throw new ArgumentNullException(nameof(CurrencyViewModel));
            Currency = currencyViewModel?.Currency;
            Navigation = currencyViewModel?.Navigation;

            Message = new Message();
            Message.RelatedTo = RelatedTo.Amount;
            UseDefaultFee = true;

            var updateAmountCommand = ReactiveCommand.CreateFromTask(UpdateAmount);
            var updateFeeCommand = ReactiveCommand.CreateFromTask(UpdateFee);

            this.WhenAnyValue(
                    vm => vm.From,
                    vm => vm.To,
                    vm => vm.Amount,
                    vm => vm.Fee
                )
                .Subscribe(_ => Message.Text = string.Empty);

            this.WhenAnyValue(
                    vm => vm.Amount,
                    vm => vm.Fee,
                    (amount, fee) => amount + fee
                )
                .Select(totalAmount => totalAmount.ToString())
                .ToPropertyEx(this, vm => vm.TotalAmountString);

            this.WhenAnyValue(
                    vm => vm.Amount,
                    vm => vm.From,
                    vm => vm.To,
                    (amount, from, to) => from
                 )
                .WhereNotNull()
                .Select(_ => Unit.Default)
                .InvokeCommand(updateAmountCommand);

            this.WhenAnyValue(
                    vm => vm.Amount,
                    vm => vm.Fee
                )
                .Subscribe(_ => OnQuotesUpdatedEventHandler(App.QuotesProvider, EventArgs.Empty));

            this.WhenAnyValue(vm => vm.Fee)
                .Select(_ => Unit.Default)
                .InvokeCommand(updateFeeCommand);

            this.WhenAnyValue(vm => vm.UseDefaultFee)
                .Where(useDefaultFee => useDefaultFee)
                .Select(_ => Unit.Default)
                .InvokeCommand(updateAmountCommand);

            this.WhenAnyValue(vm => vm.Fee)
                .Subscribe(fee =>
                {
                    FeeString = fee.ToString(CultureInfo.InvariantCulture);
                    this.RaisePropertyChanged(nameof(FeeString));
                });

            SubscribeToServices();
        }

        private void SubscribeToServices()
        {
            if (App.HasQuotesProvider)
                App.QuotesProvider.QuotesUpdated += OnQuotesUpdatedEventHandler;
        }

        private ReactiveCommand<Unit, Unit> _sendCommand;
        public ReactiveCommand<Unit, Unit> SendCommand =>
            _sendCommand ??= (_sendCommand = ReactiveCommand.CreateFromTask(SendButtonClicked));

        private ReactiveCommand<Unit, Unit> _maxCommand;
        public ReactiveCommand<Unit, Unit> MaxCommand =>
            _maxCommand ??= (_maxCommand = ReactiveCommand.CreateFromTask(async () =>
            {
                Message.Text = string.Empty;
                await OnMaxClick();
                this.RaisePropertyChanged(nameof(Message));
            }));

        private ReactiveCommand<Unit, Unit> _selectFromCommand;
        public ReactiveCommand<Unit, Unit> SelectFromCommand => _selectFromCommand ??=
            (_selectFromCommand = ReactiveCommand.CreateFromTask(FromClick));

        private ReactiveCommand<Unit, Unit> _selectToCommand;
        public ReactiveCommand<Unit, Unit> SelectToCommand => _selectToCommand ??=
            (_selectToCommand = ReactiveCommand.CreateFromTask(ToClick));

        private ReactiveCommand<Unit, Unit> _confirmationCommand;
        public ReactiveCommand<Unit, Unit> ConfirmationCommand => _confirmationCommand ??=
            (_confirmationCommand = ReactiveCommand.CreateFromTask(SendingConfirmation));

        private async Task SendingConfirmation()
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

            this.RaisePropertyChanged(nameof(Message));

            if (string.IsNullOrEmpty(Message?.Text))
                await PopupNavigation.Instance.PushAsync(new SendingConfirmationBottomSheet(this));
            else
                await Application.Current.MainPage.DisplayAlert(AppResources.Error, Message?.Text, AppResources.AcceptButton);
        }

        protected void ConfirmFromAddress(SelectAddressViewModel selectAddressViewModel, WalletAddressViewModel walletAddressViewModel)
        {
            From = walletAddressViewModel?.Address;
            SelectedFromBalance = walletAddressViewModel?.Balance ?? 0;

            switch (selectAddressViewModel.SelectAddressFrom)
            {
                case SelectAddressFrom.Init:
                    Navigation.PushAsync(new ToAddressPage(SelectToViewModel));
                    break;

                case SelectAddressFrom.Change:
                    Navigation.PopAsync();
                    break;

                case SelectAddressFrom.InitSearch:
                    Navigation.PushAsync(new ToAddressPage(SelectToViewModel));
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

        private async Task SendButtonClicked()
        {
            IsLoading = true;
            this.RaisePropertyChanged(nameof(IsLoading));
            try
            {
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
            }

            IsLoading = false;
            this.RaisePropertyChanged(nameof(IsLoading));

            await PopupNavigation.Instance.PopAsync();

            await PopupNavigation.Instance.PushAsync(new CompletionPopup(
                new PopupViewModel
                {
                    Type = PopupType.Success,
                    Title = AppResources.Success,
                    Body = string.Format(CultureInfo.InvariantCulture, AppResources.CurrencySentToAddress, Amount, CurrencyCode, To),
                    ButtonText = AppResources.AcceptButton
                }));

            for (int i = Navigation.NavigationStack.Count; i > 2; i--)
                Navigation.RemovePage(Navigation.NavigationStack[i - 1]);
        }

        protected virtual void OnQuotesUpdatedEventHandler(object sender, EventArgs args)
        {
            if (sender is not ICurrencyQuotesProvider quotesProvider)
                return;

            var quote = quotesProvider.GetQuote(CurrencyCode, BaseCurrencyCode);

            AmountInBase = Amount * (quote?.Bid ?? 0m);
            FeeInBase = Fee * (quote?.Bid ?? 0m);
            TotalAmountInBase = (Amount + Fee) * (quote?.Bid ?? 0m);
        }

        protected void ShowMessage(MessageType messageType, RelatedTo element, string text)
        {
            Message.Type = messageType;
            Message.RelatedTo = element;
            Message.Text = text;

            this.RaisePropertyChanged(nameof(Message));
        }
    }
}

