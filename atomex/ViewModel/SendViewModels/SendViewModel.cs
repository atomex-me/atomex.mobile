﻿using System;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using atomex.Resources;
using atomex.ViewModel.CurrencyViewModels;
using atomex.Views;
using atomex.Views.Popup;
using Atomex;
using Atomex.Core;
using Atomex.MarketData.Abstract;
using Rg.Plugins.Popup.Services;
using Serilog;
using Xamarin.Essentials;
using Xamarin.Forms;
using ZXing;

namespace atomex.ViewModel.SendViewModels
{
    public abstract class SendViewModel : BaseViewModel
    {
        protected IAtomexApp AtomexApp { get; set; }

        protected INavigation Navigation { get; set; }

        protected CurrencyConfig _currency;
        public virtual CurrencyConfig Currency
        {
            get => _currency;
            set
            {
                _currency = value;
                OnPropertyChanged(nameof(Currency));

                _amount = 0;
                OnPropertyChanged(nameof(AmountString));

                _fee = 0;
                OnPropertyChanged(nameof(FeeString));
            }
        }

        protected CurrencyViewModel _currencyViewModel;
        public virtual CurrencyViewModel CurrencyViewModel
        {
            get => _currencyViewModel;
            set
            {
                _currencyViewModel = value;

                CurrencyCode = _currencyViewModel?.CurrencyCode;
                FeeCurrencyCode = _currencyViewModel?.FeeCurrencyCode;
                BaseCurrencyCode = _currencyViewModel?.BaseCurrencyCode;
            }
        }

        protected string _to;
        public virtual string To
        {
            get => _to;
            set
            {
                _to = value;
                OnPropertyChanged(nameof(To));

                Warning = string.Empty;
            }
        }

        protected decimal _amount;
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

        protected decimal _fee;
        public decimal Fee
        {
            get => _fee;
            set { UpdateFee(value); }
        }

        public virtual string FeeString
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

        protected bool _useDefaultFee;
        public virtual bool UseDefaultFee
        {
            get => _useDefaultFee;
            set
            {
                _useDefaultFee = value;
                OnPropertyChanged(nameof(UseDefaultFee));

                if (_useDefaultFee)
                {
                    Warning = string.Empty;
                    Amount = _amount;
                }
            }
        }

        protected string _warning = string.Empty;
        public string Warning
        {
            get => _warning;
            set { _warning = value; OnPropertyChanged(nameof(Warning)); }
        }

        protected decimal _amountInBase;
        public decimal AmountInBase
        {
            get => _amountInBase;
            set { _amountInBase = value; OnPropertyChanged(nameof(AmountInBase)); }
        }

        protected decimal _feeInBase;
        public decimal FeeInBase
        {
            get => _feeInBase;
            set { _feeInBase = value; OnPropertyChanged(nameof(FeeInBase)); }
        }

        protected string _currencyCode;
        public string CurrencyCode
        {
            get => _currencyCode;
            set { _currencyCode = value; OnPropertyChanged(nameof(CurrencyCode)); }
        }

        protected string _feeCurrencyCode;
        public string FeeCurrencyCode
        {
            get => _feeCurrencyCode;
            set { _feeCurrencyCode = value; OnPropertyChanged(nameof(FeeCurrencyCode)); }
        }

        protected string _baseCurrencyCode = "USD";
        public string BaseCurrencyCode
        {
            get => _baseCurrencyCode;
            set { _baseCurrencyCode = value; OnPropertyChanged(nameof(BaseCurrencyCode)); }
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

        private ICommand _showAddressesCommand;
        public ICommand ShowAddressesCommand => _showAddressesCommand ??= new Command(async () => await OnFromAddressClicked());
        
        private ICommand _pasteCommand;
        public ICommand PasteCommand => _pasteCommand ??= new Command(async () => await OnPasteButtonClicked());

        private ICommand _scanCommand;
        public ICommand ScanCommand => _scanCommand ??= new Command(async () => await OnScanButtonClicked());

        private ICommand _nextCommand;
        public ICommand NextCommand => _nextCommand ??= new Command(async () => await OnNextButtonClicked());

        private async Task OnFromAddressClicked()
        {
            await Navigation.PushAsync(new AddressesListPage(this));
        }
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

        async Task OnPasteButtonClicked()
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

        private ICommand _sendCommand;
        public ICommand SendCommand => _sendCommand ??= new Command(async () => await Send());

        private async Task Send()
        {
            IsLoading = true;

            try
            {
                var error = await SendAsync();

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
            }
            catch (Exception e)
            {
                Log.Error(e, "Transaction send error.");
                IsLoading = false;
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


        public SendViewModel(IAtomexApp app, CurrencyViewModel currencyViewModel)
        {
            AtomexApp = app ?? throw new ArgumentNullException(nameof(AtomexApp));

            CurrencyViewModel = currencyViewModel ?? throw new ArgumentNullException(nameof(CurrencyViewModel));
            Currency = currencyViewModel?.Currency;

            Navigation = currencyViewModel?.Navigation;

            UseDefaultFee = true;

            SubscribeToServices();
        }

        private void SubscribeToServices()
        {
            if (AtomexApp.HasQuotesProvider)
                AtomexApp.QuotesProvider.QuotesUpdated += OnQuotesUpdatedEventHandler;
        }

        //protected virtual void ResetSendValues(bool raiseOnPropertyChanged = true)
        //{
        //    _amount = 0;
        //    OnPropertyChanged(nameof(Amount));

        //    if (raiseOnPropertyChanged)
        //        OnPropertyChanged(nameof(AmountString));

        //    AmountInBase = 0;

        //    Fee = 0;

        //    OnPropertyChanged(nameof(FeeString));

        //    FeeInBase = 0;
        //}

        protected abstract void UpdateAmount(decimal amount);
        protected abstract void UpdateFee(decimal fee);
        protected abstract void OnMaxClick();
        protected abstract Task<Error> SendAsync(CancellationToken cancellationToken = default);
        //public virtual async Task UpdateAmount(decimal amount, bool raiseOnPropertyChanged = true)
        //{
        //    Warning = string.Empty;

        //    if (amount == 0)
        //    {
        //        ResetSendValues(raiseOnPropertyChanged);
        //        return;
        //    }

        //    _amount = amount;

        //    try
        //    {
        //        var defaultFeePrice = await Currency.GetDefaultFeePriceAsync();

        //        var account = AtomexApp.Account
        //           .GetCurrencyAccount<ILegacyCurrencyAccount>(Currency.Name);

        //        if (UseDefaultFee)
        //        {
        //            var (maxAmount, _, _) = await account
        //                .EstimateMaxAmountToSendAsync(
        //                    to: To,
        //                    type: BlockchainTransactionType.Output,
        //                    fee: 0,
        //                    feePrice: 0,
        //                    reserve: true);

        //            if (_amount > maxAmount)
        //            {
        //                Warning = string.Format(CultureInfo.InvariantCulture, AppResources.InsufficientFunds);
        //                return;
        //            }

        //            var estimatedFeeAmount = _amount != 0
        //                    ? await account.EstimateFeeAsync(To, _amount, BlockchainTransactionType.Output)
        //                    : 0;

        //            if (raiseOnPropertyChanged)
        //                OnPropertyChanged(nameof(AmountString));

        //            _fee = Currency.GetFeeFromFeeAmount(estimatedFeeAmount ?? Currency.GetDefaultFee(), defaultFeePrice);
        //            OnPropertyChanged(nameof(FeeString));
        //        }
        //        else
        //        {
        //            var (maxAmount, maxFeeAmount, _) = await account
        //                .EstimateMaxAmountToSendAsync(
        //                    to: To,
        //                    type: BlockchainTransactionType.Output,
        //                    fee: 0,
        //                    feePrice: 0,
        //                    reserve: false);

        //            var availableAmount = Currency is BitcoinBasedConfig
        //                ? CurrencyViewModel.AvailableAmount
        //                : maxAmount + maxFeeAmount;

        //            var feeAmount = Currency.GetFeeAmount(_fee, defaultFeePrice);

        //            if (_amount > maxAmount || _amount + feeAmount > availableAmount)
        //            {
        //                Warning = string.Format(CultureInfo.InvariantCulture, AppResources.InsufficientFunds);
        //                return;
        //            }

        //            if (raiseOnPropertyChanged)
        //                OnPropertyChanged(nameof(AmountString));

        //            Fee = _fee;
        //        }

        //        OnQuotesUpdatedEventHandler(AtomexApp.QuotesProvider, EventArgs.Empty);
        //    }

        //    catch (Exception e)
        //    {
        //        Log.Error(e, "Update amount error");
        //    }
        //}

        //public virtual async Task UpdateFee(decimal fee)
        //{
        //    Warning = string.Empty;

        //    _fee = Math.Min(fee, Currency.GetMaximumFee());

        //    try
        //    {

        //        var defaultFeePrice = await Currency.GetDefaultFeePriceAsync();

        //        if (_amount == 0)
        //        {
        //            if (Currency.GetFeeAmount(_fee, defaultFeePrice) > CurrencyViewModel.AvailableAmount)
        //                Warning = string.Format(CultureInfo.InvariantCulture, AppResources.InsufficientFunds);

        //            return;
        //        }

        //        if (!UseDefaultFee)
        //        {
        //            var account = AtomexApp.Account
        //                .GetCurrencyAccount<ILegacyCurrencyAccount>(Currency.Name);

        //            var estimatedFeeAmount = _amount != 0
        //                ? await account.EstimateFeeAsync(To, _amount, BlockchainTransactionType.Output)
        //                : 0;

        //            var (maxAmount, maxFeeAmount, _) = await account
        //                .EstimateMaxAmountToSendAsync(
        //                    to: To,
        //                    type: BlockchainTransactionType.Output,
        //                    fee: 0,
        //                    feePrice: 0,
        //                    reserve: false);

        //            var availableAmount = Currency is BitcoinBasedConfig
        //                ? CurrencyViewModel.AvailableAmount
        //                : maxAmount + maxFeeAmount;

        //            var feeAmount = Currency.GetFeeAmount(_fee, defaultFeePrice);

        //            if (_amount + feeAmount > availableAmount)
        //            {
        //                Warning = string.Format(CultureInfo.InvariantCulture, AppResources.InsufficientFunds);
        //                return;
        //            }
        //            else if (estimatedFeeAmount == null || feeAmount < estimatedFeeAmount.Value)
        //            {
        //                Warning = string.Format(CultureInfo.InvariantCulture, AppResources.LowFees);
        //                return;
        //            }

        //            Warning = string.Empty;

        //            OnPropertyChanged(nameof(FeeString));
        //        }

        //        OnQuotesUpdatedEventHandler(AtomexApp.QuotesProvider, EventArgs.Empty);
        //    }
        //    catch (Exception e)
        //    {
        //        Log.Error(e, "Update fee error");
        //    }
        //}

        private ICommand _maxAmountCommand;
        public  ICommand MaxAmountCommand => _maxAmountCommand ??= new Command(() => OnMaxClick());

        protected virtual void OnQuotesUpdatedEventHandler(object sender, EventArgs args)
        {
            if (!(sender is ICurrencyQuotesProvider quotesProvider))
                return;

            var quote = quotesProvider.GetQuote(CurrencyCode, BaseCurrencyCode);

            AmountInBase = Amount * (quote?.Bid ?? 0m);
            FeeInBase = Fee * (quote?.Bid ?? 0m);
        }
    }
}

