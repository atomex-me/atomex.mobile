using System;
using System.Globalization;
using System.Threading.Tasks;
using System.Windows.Input;
using atomex.Resources;
using Atomex.Blockchain.Abstract;
using Atomex.Core;
using Atomex.MarketData.Abstract;
using Atomex.Wallet.Abstract;
using Serilog;
using Xamarin.Forms;

namespace atomex.ViewModel.SendViewModels
{
    public class EthereumSendViewModel : SendViewModel
    {
        public override CurrencyConfig Currency
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

                _feePrice = 0;
                OnPropertyChanged(nameof(FeePriceString));

                OnPropertyChanged(nameof(TotalFeeString));

                FeePriceFormat = _currency.FeePriceFormat;
                FeePriceCode = _currency.FeePriceCode;

            }
        }

        protected string FeePriceFormat { get; set; }
        public virtual string TotalFeeCurrencyCode => CurrencyCode;

        public virtual string GasCode => "GAS";
        public virtual string GasFormat => "F0";

        public virtual string GasString
        {
            get => Fee.ToString(GasFormat, CultureInfo.InvariantCulture);
            set
            {
                string temp = value.Replace(",", ".");
                if (!decimal.TryParse(temp, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var fee))
                    return;

                Fee = fee;
            }
        }

        public override decimal FeePrice
        {
            get => _feePrice;
            set { _ = UpdateFeePrice(value); }
            
        }

        public virtual string FeePriceString
        {
            get => FeePrice.ToString(FeePriceFormat, CultureInfo.InvariantCulture);
            set
            {
                string temp = value.Replace(",", ".");
                if (!decimal.TryParse(temp, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var gasPrice))
                    return;

                FeePrice = gasPrice;
            }
        }

        protected decimal _totalFee;

        private bool _isTotalFeeUpdating;
        public bool IsTotalFeeUpdating
        {
            get => _isTotalFeeUpdating;
            set { _isTotalFeeUpdating = value; OnPropertyChanged(nameof(IsTotalFeeUpdating)); }
        }

        public virtual string TotalFeeString
        {
            get => _totalFee
               .ToString(FeeCurrencyFormat, CultureInfo.InvariantCulture);
            set { UpdateTotalFeeString(); }
        }

        public override bool UseDefaultFee
        {
            get => _useDefaultFee;
            set
            {
                Warning = string.Empty;

                _useDefaultFee = value;
                OnPropertyChanged(nameof(UseDefaultFee));

                if (_useDefaultFee)
                    _ = UpdateAmount(_amount);
            }
        }

        protected string _feePriceCode;
        public string FeePriceCode
        {
            get => _feePriceCode;
            set { _feePriceCode = value; OnPropertyChanged(nameof(FeePriceCode)); }
        }


        public EthereumSendViewModel(CurrencyViewModel currencyViewModel) : base(currencyViewModel)
        {
        }

        protected override void ResetSendValues(bool raiseOnPropertyChanged = true)
        {
            _amount = 0;
            OnPropertyChanged(nameof(Amount));

            if (raiseOnPropertyChanged)
                OnPropertyChanged(nameof(AmountString));

            AmountInBase = 0;

            Fee = 0;
            OnPropertyChanged(nameof(FeeString));

            FeeInBase = 0;

            _totalFee = 0;
            OnPropertyChanged(nameof(TotalFeeString));
        }

        public override async Task UpdateAmount(decimal amount, bool raiseOnPropertyChanged = true)
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
                var account = AtomexApp.Account
                    .GetCurrencyAccount<ILegacyCurrencyAccount>(Currency.Name);

                if (UseDefaultFee)
                {
                    var (maxAmount, maxFeeAmount, _) = await account
                        .EstimateMaxAmountToSendAsync(
                            to: To,
                            type: BlockchainTransactionType.Output,
                            fee: 0,
                            feePrice: 0,
                            reserve: false);

                    _fee = Currency.GetDefaultFee();
                    OnPropertyChanged(nameof(GasString));

                    _feePrice = await Currency.GetDefaultFeePriceAsync();
                    OnPropertyChanged(nameof(FeePriceString));

                    if (_amount > maxAmount)
                    {
                        Warning = string.Format(CultureInfo.InvariantCulture, AppResources.InsufficientFunds);
                        return;
                    }

                    if (raiseOnPropertyChanged)
                        OnPropertyChanged(nameof(AmountString));

                    UpdateTotalFeeString();

                    OnPropertyChanged(nameof(TotalFeeString));
                }
                else
                {
                    var (maxAmount, maxFeeAmount, _) = await account
                          .EstimateMaxAmountToSendAsync(
                            to: To,
                            type: BlockchainTransactionType.Output,
                            fee: _fee,
                            feePrice: _feePrice,
                            reserve: false);

                    if (_amount > maxAmount)
                    {
                        Warning = string.Format(CultureInfo.InvariantCulture, AppResources.InsufficientFunds);
                        return;
                    }

                    if (raiseOnPropertyChanged)
                        OnPropertyChanged(nameof(AmountString));

                    if (_fee < Currency.GetDefaultFee() || _feePrice == 0)
                        Warning = string.Format(CultureInfo.InvariantCulture, AppResources.LowFees);
                }

                OnQuotesUpdatedEventHandler(AtomexApp.QuotesProvider, EventArgs.Empty);
            }
            catch(Exception e)
            {
                Log.Error(e, "ETH update amount error");
            }
        }

        public virtual async Task UpdateFeePrice(decimal value)
        {
            Warning = string.Empty;

            _feePrice = value;

            try
            {
                if (_amount == 0)
                {
                    if (Currency.GetFeeAmount(_fee, _feePrice) > CurrencyViewModel.AvailableAmount)
                        Warning = string.Format(CultureInfo.InvariantCulture, AppResources.InsufficientFunds);
                    return;
                }

                if (value == 0)
                {
                    Warning = string.Format(CultureInfo.InvariantCulture, AppResources.LowFees);
                    UpdateTotalFeeString();
                    OnPropertyChanged(nameof(TotalFeeString));
                    return;
                }

                if (!UseDefaultFee)
                {
                    var account = AtomexApp.Account
                        .GetCurrencyAccount<ILegacyCurrencyAccount>(Currency.Name);

                    var (maxAmount, maxFee, _) = await account
                        .EstimateMaxAmountToSendAsync(
                        to: To,
                        type: BlockchainTransactionType.Output,
                        fee: _fee,
                        feePrice: _feePrice,
                        reserve: false);

                    if (_amount > maxAmount)
                    {
                        Warning = string.Format(CultureInfo.InvariantCulture, AppResources.InsufficientFunds);
                        return;
                    }

                    OnPropertyChanged(nameof(FeePrice));
                    OnPropertyChanged(nameof(FeePriceString));
                    UpdateTotalFeeString();
                    OnPropertyChanged(nameof(TotalFeeString));
                }

                OnQuotesUpdatedEventHandler(AtomexApp.QuotesProvider, EventArgs.Empty);
            }
            catch (Exception e)
            {
                Log.Error(e, "ETH update fee price error");
            }
        }

        public override async Task UpdateFee(decimal fee)
        {
            Warning = string.Empty;

            _fee = Math.Min(fee, Currency.GetMaximumFee());

            try
            {

                if (_amount == 0)
                {
                    if (Currency.GetFeeAmount(_fee, _feePrice) > CurrencyViewModel.AvailableAmount)
                        Warning = string.Format(CultureInfo.InvariantCulture, AppResources.InsufficientFunds);
                    return;
                }

                if (_fee < Currency.GetDefaultFee())
                {
                    Warning = string.Format(CultureInfo.InvariantCulture, AppResources.LowFees);
                    if (fee == 0)
                    {
                        UpdateTotalFeeString();
                        OnPropertyChanged(nameof(TotalFeeString));
                        return;
                    }
                }

                if (!UseDefaultFee)
                {
                    var account = AtomexApp.Account
                        .GetCurrencyAccount<ILegacyCurrencyAccount>(Currency.Name);

                    var (maxAmount, maxFee, _) = await account
                        .EstimateMaxAmountToSendAsync(
                            to: To,
                            type: BlockchainTransactionType.Output,
                            fee: _fee,
                            feePrice: _feePrice,
                            reserve: false);

                    if (_amount > maxAmount)
                    {
                        Warning = string.Format(CultureInfo.InvariantCulture, AppResources.InsufficientFunds);
                        return;
                    }

                    UpdateTotalFeeString();
                    OnPropertyChanged(nameof(TotalFeeString));

                    OnPropertyChanged(nameof(GasString));
                }

                OnQuotesUpdatedEventHandler(AtomexApp.QuotesProvider, EventArgs.Empty);
            }
            catch (Exception e)
            {
                Log.Error(e, "ETH update fee error");
            }
        }

        protected async void UpdateTotalFeeString(decimal totalFeeAmount = 0)
        {
            IsTotalFeeUpdating = true;

            try
            {
                var account = AtomexApp.Account
                    .GetCurrencyAccount<ILegacyCurrencyAccount>(Currency.Name);

                var feeAmount = totalFeeAmount > 0
                    ? totalFeeAmount
                    : Currency.GetFeeAmount(_fee, _feePrice) > 0
                        ? await account.EstimateFeeAsync(To, _amount, BlockchainTransactionType.Output, _fee, _feePrice)
                        : 0;

                if (feeAmount != null)
                    _totalFee = feeAmount.Value;
            }
            finally
            {
                IsTotalFeeUpdating = false;
            }
        }

        private ICommand _maxAmountCommand;
        public override ICommand MaxAmountCommand => _maxAmountCommand ??= new Command(async () => await OnMaxClick());

        protected override async Task OnMaxClick()
        {
            Warning = string.Empty;

            try
            {
                var availableAmount = CurrencyViewModel.AvailableAmount;

                if (availableAmount == 0)
                    return;

                var account = AtomexApp.Account
                    .GetCurrencyAccount<ILegacyCurrencyAccount>(Currency.Name);

                if (UseDefaultFee)
                {
                    var (maxAmount, maxFeeAmount, _) = await account
                        .EstimateMaxAmountToSendAsync(
                            to: To,
                            type: BlockchainTransactionType.Output,
                            fee: 0,
                            feePrice: 0,
                            reserve: false);

                    if (maxAmount > 0)
                        _amount = maxAmount;

                    OnPropertyChanged(nameof(AmountString));

                    _fee = Currency.GetDefaultFee();
                    OnPropertyChanged(nameof(GasString));

                    _feePrice = await Currency.GetDefaultFeePriceAsync();
                    OnPropertyChanged(nameof(FeePriceString));
                    OnPropertyChanged(nameof(FeePrice));

                    UpdateTotalFeeString(maxFeeAmount);
                    OnPropertyChanged(nameof(TotalFeeString));
                }
                else
                {
                    if (_fee < Currency.GetDefaultFee() || _feePrice == 0)
                    {
                        Warning = string.Format(CultureInfo.InvariantCulture, AppResources.LowFees);
                        if (_fee == 0 || _feePrice == 0)
                        {
                            _amount = 0;
                            OnPropertyChanged(nameof(AmountString));
                            return;
                        }
                    }

                    var (maxAmount, maxFeeAmount, _) = await account
                        .EstimateMaxAmountToSendAsync(To, BlockchainTransactionType.Output, _fee, _feePrice, false);

                    _amount = maxAmount;

                    if (maxAmount == 0 && availableAmount > 0)
                        Warning = string.Format(CultureInfo.InvariantCulture, AppResources.InsufficientFunds);

                    OnPropertyChanged(nameof(AmountString));
                    UpdateTotalFeeString(maxFeeAmount);
                    OnPropertyChanged(nameof(TotalFeeString));
                }

                OnQuotesUpdatedEventHandler(AtomexApp.QuotesProvider, EventArgs.Empty);
            }
            catch (Exception e)
            {
                Log.Error(e, "ETH max click error");
            }
        }

        protected async override Task OnNextButtonClicked()
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

            var feeAmount = !isToken ? Currency.GetFeeAmount(Fee, FeePrice) : 0;

            if (Amount + feeAmount > CurrencyViewModel.AvailableAmount)
            {
                Warning = AppResources.AvailableFundsError;
                return;
            }

            if (Amount + feeAmount > CurrencyViewModel.AvailableAmount)
            {
                Warning = AppResources.AvailableFundsError;
                return;
            }

            if (string.IsNullOrEmpty(Warning))
                await Navigation.PushAsync(new SendingConfirmationPage(this));
            else
                await App.Current.MainPage.DisplayAlert(AppResources.Error, Warning, AppResources.AcceptButton);
        }

        protected override void OnQuotesUpdatedEventHandler(object sender, EventArgs args)
        {
            if (!(sender is ICurrencyQuotesProvider quotesProvider))
                return;

            var quote = quotesProvider.GetQuote(CurrencyCode, BaseCurrencyCode);

            AmountInBase = Amount * (quote?.Bid ?? 0m);
            FeeInBase = Currency.GetFeeAmount(Fee, FeePrice) * (quote?.Bid ?? 0m);
        }
    }
}

