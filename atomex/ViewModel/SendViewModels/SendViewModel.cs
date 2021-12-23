using System;
using System.Globalization;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using atomex.Resources;
using atomex.ViewModel.CurrencyViewModels;
using atomex.Views;
using atomex.Views.Popup;
using Atomex;
using Atomex.Core;
using Atomex.MarketData.Abstract;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Rg.Plugins.Popup.Services;
using Serilog;
using Xamarin.Essentials;
using Xamarin.Forms;
using ZXing;

namespace atomex.ViewModel.SendViewModels
{
    public abstract class SendViewModel : BaseViewModel
    {
        protected IAtomexApp App { get; }
        protected INavigation Navigation { get; set; }
        protected CurrencyConfig Currency { get; set; }

        [Reactive] public CurrencyViewModel CurrencyViewModel { get; set; }
        [Reactive] public string From { get; set; }
        [Reactive] public decimal SelectedFromAmount { get; set; }
        [Reactive] public string To { get; set; }
        [Reactive] protected decimal Amount { get; set; }

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

        [Reactive] protected decimal Fee { get; set; }

        public string FeeString
        {
            get => Amount.ToString(CultureInfo.InvariantCulture);
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
        [Reactive] public string Warning { get; set; }
        [Reactive] public bool IsLoading { get; set; }
        [Reactive] public bool IsScanning { get; set; }
        [Reactive] public bool IsAnalyzing { get; set; }

        public string CurrencyCode => CurrencyViewModel.CurrencyCode;
        public string FeeCurrencyCode => CurrencyViewModel.FeeCurrencyCode;
        public string BaseCurrencyCode => CurrencyViewModel.BaseCurrencyCode;

        public string AmountEntryPlaceholderString => $"{AppResources.AmountEntryPlaceholder}, {CurrencyCode}";
        public string FeeEntryPlaceholderString => $"{AppResources.FeeLabel}, {FeeCurrencyCode}";

        protected abstract Task UpdateAmount(decimal amount);
        protected abstract Task UpdateFee(decimal fee);
        protected abstract Task OnMaxClick();
        protected abstract Task<Error> Send(CancellationToken cancellationToken = default);

        public SendViewModel(IAtomexApp app, CurrencyViewModel currencyViewModel)
        {
            App = app ?? throw new ArgumentNullException(nameof(AtomexApp));

            CurrencyViewModel = currencyViewModel ?? throw new ArgumentNullException(nameof(CurrencyViewModel));
            Currency = currencyViewModel?.Currency;

            Navigation = currencyViewModel?.Navigation;

            UseDefaultFee = true;

            var updateAmountCommand = ReactiveCommand.CreateFromTask<decimal>(UpdateAmount);
            var updateFeeCommand = ReactiveCommand.CreateFromTask<decimal>(UpdateFee);

            this.WhenAnyValue(
                    vm => vm.To,
                    vm => vm.Amount,
                    vm => vm.Fee
                )
                .Subscribe(_ => Warning = string.Empty);

            this.WhenAnyValue(vm => vm.Amount)
                .InvokeCommand(updateAmountCommand);

            this.WhenAnyValue(vm => vm.Fee)
                .InvokeCommand(updateFeeCommand);

            this.WhenAnyValue(vm => vm.UseDefaultFee)
                .Where(useDefaultFee => useDefaultFee)
                .Subscribe(_ => updateAmountCommand.Execute(Amount));

            SubscribeToServices();
        }

        private void SubscribeToServices()
        {
            if (App.HasQuotesProvider)
                App.QuotesProvider.QuotesUpdated += OnQuotesUpdatedEventHandler;
        }

        private ReactiveCommand<Unit, Unit> _nextCommand;
        public ReactiveCommand<Unit, Unit> NextCommand =>
            _nextCommand ??= (_nextCommand = ReactiveCommand.CreateFromTask(OnNextButtonClicked));

        private ReactiveCommand<Unit, Unit> _sendCommand;
        public ReactiveCommand<Unit, Unit> SendCommand =>
            _sendCommand ??= (_sendCommand = ReactiveCommand.CreateFromTask(SendClicked));

        private ReactiveCommand<Unit, Unit> _maxCommand;
        public ReactiveCommand<Unit, Unit> MaxCommand =>
            _maxCommand ??= (_maxCommand = ReactiveCommand.CreateFromTask(async () =>
            {
                Warning = string.Empty;
                await OnMaxClick();
            }));

        private ReactiveCommand<Unit, Unit> _showAddressesCommand;
        public ReactiveCommand<Unit, Unit> ShowAddressesCommand =>
            _showAddressesCommand ??= (_showAddressesCommand = ReactiveCommand.CreateFromTask(OnFromAddressClicked));

        private ReactiveCommand<Unit, Unit> _pasteCommand;
        public ReactiveCommand<Unit, Unit> PasteCommand =>
            _pasteCommand ??= (_pasteCommand = ReactiveCommand.CreateFromTask(OnPasteButtonClicked));

        private ReactiveCommand<Unit, Unit> _scanCommand;
        public ReactiveCommand<Unit, Unit> ScanCommand =>
            _scanCommand ??= (_scanCommand = ReactiveCommand.CreateFromTask(OnScanButtonClicked));

        private ReactiveCommand<Unit, Unit> _scanResultCommand;
        public ReactiveCommand<Unit, Unit> ScanResultCommand =>
            _scanResultCommand ??= (_scanResultCommand = ReactiveCommand.CreateFromTask(OnScanResult));

        private async Task OnFromAddressClicked()
        {
            if (Currency is BitcoinBasedConfig)
            await Navigation.PushAsync(new AddressesListPage(this));
        }

        [Reactive] public Result ScanResult { get; set; }

        private async Task OnScanResult()
        {
            IsScanning = false;
            IsAnalyzing = false;
            this.RaisePropertyChanged(nameof(IsScanning));
            this.RaisePropertyChanged(nameof(IsAnalyzing));

            if (ScanResult == null)
            {
                await Application.Current.MainPage.DisplayAlert(AppResources.Error, "Incorrect QR code format", AppResources.AcceptButton);

                Device.BeginInvokeOnMainThread(async () =>
                {
                    await Navigation.PopAsync();
                });
                return;
            }

            Device.BeginInvokeOnMainThread(async () =>
            {
                int indexOfChar = ScanResult.Text.IndexOf(':');
                if (indexOfChar == -1)
                    To = ScanResult.Text;
                else
                    To = ScanResult.Text.Substring(indexOfChar + 1);

                await Navigation.PopAsync();
            });
        }

        private async Task OnScanButtonClicked()
        {
            PermissionStatus permissions = await Permissions.CheckStatusAsync<Permissions.Camera>();

            if (permissions != PermissionStatus.Granted)
                permissions = await Permissions.RequestAsync<Permissions.Camera>();
            if (permissions != PermissionStatus.Granted)
                return;

            IsScanning = true;
            IsAnalyzing = true;
            this.RaisePropertyChanged(nameof(IsScanning));
            this.RaisePropertyChanged(nameof(IsAnalyzing));

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

        protected virtual async Task OnNextButtonClicked()
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
                await Navigation.PushAsync(new SendingConfirmationPage(this));
            else
                await Application.Current.MainPage.DisplayAlert(AppResources.Error, Warning, AppResources.AcceptButton);
        }

        private async Task SendClicked()
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

        protected void OnQuotesUpdatedEventHandler(object sender, EventArgs args)
        {
            if (sender is not ICurrencyQuotesProvider quotesProvider)
                return;

            var quote = quotesProvider.GetQuote(CurrencyCode, BaseCurrencyCode);

            AmountInBase = Amount * (quote?.Bid ?? 0m);
            FeeInBase = Fee * (quote?.Bid ?? 0m);
        }
    }
}

