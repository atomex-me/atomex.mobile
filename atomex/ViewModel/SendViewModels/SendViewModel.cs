using System;
using System.Globalization;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using atomex.Resources;
using atomex.ViewModel.CurrencyViewModels;
using atomex.Views.Popup;
using Atomex;
using Atomex.Core;
using Atomex.MarketData.Abstract;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Rg.Plugins.Popup.Services;
using Serilog;
using Xamarin.Forms;

namespace atomex.ViewModel.SendViewModels
{
    public abstract class SendViewModel : BaseViewModel
    {
        protected IAtomexApp App { get; }
        protected INavigation Navigation { get; set; }

        protected CurrencyConfig Currency { get; set; }
        public BaseViewModel SelectFromViewModel { get; set; }
        protected SelectAddressViewModel SelectToViewModel { get; set; }
        [Reactive] public CurrencyViewModel CurrencyViewModel { get; set; }
        [Reactive] public string From { get; set; }
        [Reactive] public decimal SelectedAmount { get; set; }
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
                    Fee = Math.Min(fee, Currency.GetMaximumFee());
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
        [Reactive] public string Warning { get; set; }
        [Reactive] public bool IsLoading { get; set; }

        public string CurrencyCode => CurrencyViewModel.CurrencyCode;
        public string FeeCurrencyCode => CurrencyViewModel.FeeCurrencyCode;
        public string BaseCurrencyCode => CurrencyViewModel.BaseCurrencyCode;

        public string AmountEntryPlaceholderString => $"{AppResources.EnterAmountLabel}, {CurrencyCode}";
        public string FeeEntryPlaceholderString => $"{AppResources.FeeLabel}, {FeeCurrencyCode}";

        protected abstract Task UpdateAmount();
        protected abstract Task UpdateFee();
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

            UseDefaultFee = true;

            var updateAmountCommand = ReactiveCommand.CreateFromTask(UpdateAmount);
            var updateFeeCommand = ReactiveCommand.CreateFromTask(UpdateFee);

            this.WhenAnyValue(
                vm => vm.From,
                vm => vm.To,
                vm => vm.Amount,
                vm => vm.Fee
                )
                .Subscribe(_ => Warning = string.Empty);

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
                    vm => vm.To
                )
                .Select(_ => Unit.Default)
                .InvokeCommand(updateAmountCommand);

            this.WhenAnyValue(vm => vm.Fee)
                .Select(_ => Unit.Default)
                .InvokeCommand(updateFeeCommand);

            this.WhenAnyValue(vm => vm.UseDefaultFee)
                .Where(useDefaultFee => useDefaultFee)
                .Select(_ => Unit.Default)
                .InvokeCommand(updateAmountCommand);

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
                Warning = string.Empty;
                await OnMaxClick();
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
            {
                Warning = AppResources.EmptyAddressError;
                return;
            }

            if (!Currency.IsValidAddress(To))
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

            var isToken = Currency.FeeCurrencyName != Currency.Name;

            var feeAmount = !isToken ? Fee : 0;

            if (Amount + feeAmount > CurrencyViewModel.AvailableAmount)
            {
                Warning = AppResources.AvailableFundsError;
                return;
            }

            if (string.IsNullOrEmpty(Warning))
                await PopupNavigation.Instance.PushAsync(new Views.Send.SendingConfirmationBottomSheet(this));
            else
                await Application.Current.MainPage.DisplayAlert(AppResources.Error, Warning, AppResources.AcceptButton);
        }

        protected async void ConfirmToAddress(string address, decimal? balance)
        {
            To = address;
            await Navigation.PushAsync(new Views.Send.SendPage(this));
        }

        protected async void ChangeToAddress(string address, decimal? balance)
        {
            To = address;
            await Navigation.PopAsync();
        }

        protected async void ScanAddress(string address)
        {
            To = address;
            await Navigation.PopAsync();
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

        protected void OnQuotesUpdatedEventHandler(object sender, EventArgs args)
        {
            if (sender is not ICurrencyQuotesProvider quotesProvider)
                return;

            var quote = quotesProvider.GetQuote(CurrencyCode, BaseCurrencyCode);

            AmountInBase = Amount * (quote?.Bid ?? 0m);
            FeeInBase = Fee * (quote?.Bid ?? 0m);
            TotalAmountInBase = (Amount + Fee) * (quote?.Bid ?? 0m);
        }
    }
}

