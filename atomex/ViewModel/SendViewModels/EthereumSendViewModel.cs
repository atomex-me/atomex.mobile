using System;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using atomex.Resources;
using atomex.ViewModel.CurrencyViewModels;
using Atomex;
using Atomex.Blockchain.Abstract;
using Atomex.Core;
using Atomex.MarketData.Abstract;
using Atomex.Wallet.Abstract;
using Atomex.Wallet.Ethereum;

namespace atomex.ViewModel.SendViewModels
{
    public class EthereumSendViewModel : SendViewModel
    {

        public EthereumSendViewModel(
            IAtomexApp app,
            CurrencyViewModel currencyViewModel)
            : base(app, currencyViewModel)
        {
        }

        //public string From { get; set; }
        //protected override Task UpdateAmount(decimal amount);
        //protected abstract Task UpdateFee(decimal fee);
        //protected abstract Task OnMaxClick();
        //protected abstract Task<Error> Send(CancellationToken cancellationToken = default);

        protected override Task UpdateFee()
        {
            throw new NotImplementedException();
        }

        protected override Task OnMaxClick()
        {
            throw new NotImplementedException();
        }

        protected override Task<Error> Send(CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        protected override Task UpdateAmount()
        {
            throw new NotImplementedException();
        }

        protected override Task FromClick()
        {
            throw new NotImplementedException();
        }

        protected override Task ToClick()
        {
            throw new NotImplementedException();
        }

        //public override CurrencyConfig Currency
        //{
        //    get => _currency;
        //    set
        //    {

        //        _currency = value;
        //        OnPropertyChanged(nameof(Currency));

        //        _amount = 0;
        //        OnPropertyChanged(nameof(AmountString));

        //        _fee = 0;
        //        OnPropertyChanged(nameof(FeeString));

        //        _feePrice = 0;
        //        OnPropertyChanged(nameof(FeePriceString));

        //        OnPropertyChanged(nameof(TotalFeeString));

        //        FeePriceCode = _currency.FeePriceCode;

        //        Warning = string.Empty;
        //    }
        //}

        //public virtual string TotalFeeCurrencyCode => CurrencyCode;

        //public virtual string GasCode => "GAS";

        //public virtual string GasString
        //{
        //    get => Fee.ToString(CultureInfo.InvariantCulture);
        //    set
        //    {
        //        if (!decimal.TryParse(value, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var fee))
        //        {
        //            if (fee == 0)
        //                Fee = fee;

        //            OnPropertyChanged(nameof(GasString));
        //            return;
        //        }

        //        Fee = fee;
        //        OnPropertyChanged(nameof(GasString));
        //    }
        //}

        //protected decimal _feePrice;
        //public virtual decimal FeePrice
        //{
        //    get => _feePrice;
        //    set { UpdateFeePrice(value); }
        //}

        //private bool _isFeePriceUpdating;
        //public bool IsFeePriceUpdating
        //{
        //    get => _isFeePriceUpdating;
        //    set { _isFeePriceUpdating = value; OnPropertyChanged(nameof(IsFeePriceUpdating)); }
        //}

        //public virtual string FeePriceString
        //{
        //    get => FeePrice.ToString(CultureInfo.InvariantCulture);
        //    set
        //    {
        //        if (!decimal.TryParse(value, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture,
        //            out var gasPrice))
        //        {
        //            if (gasPrice == 0)
        //                FeePrice = gasPrice;

        //            OnPropertyChanged(nameof(FeePriceString));
        //            return;
        //        }

        //        FeePrice = gasPrice;
        //        OnPropertyChanged(nameof(FeePriceString));
        //    }
        //}

        //protected decimal _totalFee;

        //private bool _isTotalFeeUpdating;
        //public bool IsTotalFeeUpdating
        //{
        //    get => _isTotalFeeUpdating;
        //    set { _isTotalFeeUpdating = value; OnPropertyChanged(nameof(IsTotalFeeUpdating)); }
        //}

        //public virtual string TotalFeeString
        //{
        //    get => _totalFee.ToString(CultureInfo.InvariantCulture);
        //    set { UpdateTotalFeeString(); }
        //}

        //public override bool UseDefaultFee
        //{
        //    get => _useDefaultFee;
        //    set
        //    {
        //        Warning = string.Empty;

        //        _useDefaultFee = value;
        //        OnPropertyChanged(nameof(UseDefaultFee));

        //        if (_useDefaultFee)
        //            Amount = _amount; // recalculate amount and fee using default fee
        //    }
        //}

        //protected string _feePriceCode;
        //public string FeePriceCode
        //{
        //    get => _feePriceCode;
        //    set { _feePriceCode = value; OnPropertyChanged(nameof(FeePriceCode)); }
        //}

        //protected override async void UpdateAmount(decimal amount)
        //{
        //    if (IsAmountUpdating)
        //        return;

        //    IsAmountUpdating = true;

        //    _amount = amount;

        //    Warning = string.Empty;

        //    try
        //    {
        //        if (AtomexApp.Account.GetCurrencyAccount(Currency.Name) is not IEstimatable account)
        //            return; // todo: error?

        //        var maxAmountEstimation = await account.EstimateMaxAmountToSendAsync(
        //            from: new FromAddress(From),
        //            to: _to,
        //            type: BlockchainTransactionType.Output,
        //            fee: UseDefaultFee ? 0 : _fee,
        //            feePrice: UseDefaultFee ? 0 : _feePrice,
        //            reserve: false);

        //        if (UseDefaultFee)
        //        {
        //            _fee = Currency.GetDefaultFee();
        //            OnPropertyChanged(nameof(GasString));

        //            _feePrice = await Currency.GetDefaultFeePriceAsync();
        //            OnPropertyChanged(nameof(FeePriceString));

        //            if (_amount > maxAmountEstimation.Amount)
        //            {
        //                Warning = AppResources.InsufficientFunds;
        //                IsAmountUpdating = false;
        //                return;
        //            }

        //            OnPropertyChanged(nameof(AmountString));

        //            UpdateTotalFeeString();
        //            OnPropertyChanged(nameof(TotalFeeString));
        //        }
        //        else
        //        {
        //            if (_amount > maxAmountEstimation.Amount)
        //            {
        //                Warning = AppResources.InsufficientFunds;
        //                IsAmountUpdating = false;
        //                return;
        //            }

        //            OnPropertyChanged(nameof(AmountString));

        //            if (_fee < Currency.GetDefaultFee() || _feePrice == 0)
        //                Warning = AppResources.LowFees;
        //        }

        //        OnQuotesUpdatedEventHandler(AtomexApp.QuotesProvider, EventArgs.Empty);
        //    }
        //    finally
        //    {
        //        IsAmountUpdating = false;
        //    }
        //}

        //private async void UpdateFeePrice(decimal value)
        //{
        //    if (IsFeeUpdating)
        //        return;

        //    IsFeeUpdating = true;

        //    _feePrice = value;

        //    Warning = string.Empty;

        //    try
        //    {
        //        if (_amount == 0)
        //        {
        //            if (Currency.GetFeeAmount(_fee, _feePrice) > CurrencyViewModel.AvailableAmount)
        //                Warning = AppResources.InsufficientFunds;
        //            return;
        //        }

        //        if (value == 0)
        //        {
        //            Warning = AppResources.LowFees;
        //            UpdateTotalFeeString();
        //            OnPropertyChanged(nameof(TotalFeeString));
        //            return;
        //        }

        //        if (!UseDefaultFee)
        //        {
        //            if (AtomexApp.Account.GetCurrencyAccount(Currency.Name) is not IEstimatable account)
        //                return; // todo: error?

        //            var maxAmountEstimation = await account.EstimateMaxAmountToSendAsync(
        //                from: new FromAddress(From),
        //                to: _to,
        //                type: BlockchainTransactionType.Output,
        //                fee: _fee,
        //                feePrice: _feePrice,
        //                reserve: false);

        //            if (_amount > maxAmountEstimation.Amount)
        //            {
        //                Warning = AppResources.InsufficientFunds;
        //                return;
        //            }

        //            OnPropertyChanged(nameof(FeePriceString));

        //            UpdateTotalFeeString();
        //            OnPropertyChanged(nameof(TotalFeeString));
        //        }

        //        OnQuotesUpdatedEventHandler(AtomexApp.QuotesProvider, EventArgs.Empty);
        //    }
        //    finally
        //    {
        //        IsFeeUpdating = false;
        //    }
        //}

        //protected override async void UpdateFee(decimal fee)
        //{
        //    if (IsFeeUpdating)
        //        return;

        //    IsFeeUpdating = true;

        //    _fee = Math.Min(fee, Currency.GetMaximumFee());

        //    Warning = string.Empty;

        //    try
        //    {
        //        if (_amount == 0)
        //        {
        //            if (Currency.GetFeeAmount(_fee, _feePrice) > CurrencyViewModel.AvailableAmount)
        //                Warning = AppResources.InsufficientFunds;
        //            return;
        //        }

        //        if (_fee < Currency.GetDefaultFee())
        //        {
        //            Warning = AppResources.LowFees;
        //            if (fee == 0)
        //            {
        //                UpdateTotalFeeString();
        //                OnPropertyChanged(nameof(TotalFeeString));
        //                return;
        //            }
        //        }

        //        if (!UseDefaultFee)
        //        {
        //            if (AtomexApp.Account.GetCurrencyAccount(Currency.Name) is not IEstimatable account)
        //                return; // todo: error?

        //            var maxAmountEstimation = await account.EstimateMaxAmountToSendAsync(
        //                from: new FromAddress(From),
        //                to: _to,
        //                type: BlockchainTransactionType.Output,
        //                fee: _fee,
        //                feePrice: _feePrice,
        //                reserve: false);

        //            if (_amount > maxAmountEstimation.Amount)
        //            {
        //                Warning = AppResources.InsufficientFunds;
        //                return;
        //            }

        //            UpdateTotalFeeString();
        //            OnPropertyChanged(nameof(TotalFeeString));

        //            OnPropertyChanged(nameof(GasString));
        //        }

        //        OnQuotesUpdatedEventHandler(AtomexApp.QuotesProvider, EventArgs.Empty);
        //    }
        //    finally
        //    {
        //        IsFeeUpdating = false;
        //    }
        //}

        //protected async void UpdateTotalFeeString(decimal totalFeeAmount = 0)
        //{
        //    IsTotalFeeUpdating = true;

        //    try
        //    {
        //        if (AtomexApp.Account.GetCurrencyAccount(Currency.Name) is not IEstimatable account)
        //            return; // todo: error?

        //        var feeAmount = totalFeeAmount > 0
        //            ? totalFeeAmount
        //            : Currency.GetFeeAmount(_fee, _feePrice) > 0
        //                ? await account
        //                    .EstimateFeeAsync(new FromAddress(From), To, _amount, BlockchainTransactionType.Output)
        //                : 0;

        //        if (feeAmount != null)
        //            _totalFee = feeAmount.Value;
        //    }
        //    finally
        //    {
        //        IsTotalFeeUpdating = false;
        //    }
        //}

        //protected override async void OnMaxClick()
        //{
        //    if (IsAmountUpdating)
        //        return;

        //    IsAmountUpdating = true;

        //    Warning = string.Empty;

        //    try
        //    {
        //        var availableAmount = CurrencyViewModel.AvailableAmount;

        //        if (availableAmount == 0)
        //            return;

        //        if (AtomexApp.Account.GetCurrencyAccount(Currency.Name) is not IEstimatable account)
        //            return; // todo: error?

        //        if (UseDefaultFee)
        //        {
        //            var maxAmountEstimation = await account
        //                .EstimateMaxAmountToSendAsync(
        //                    from: new FromAddress(From),
        //                    to: To,
        //                    type: BlockchainTransactionType.Output,
        //                    fee: 0,
        //                    feePrice: 0,
        //                    reserve: false);

        //            if (maxAmountEstimation.Amount > 0)
        //                _amount = maxAmountEstimation.Amount;

        //            OnPropertyChanged(nameof(AmountString));

        //            _fee = Currency.GetDefaultFee();
        //            OnPropertyChanged(nameof(GasString));

        //            _feePrice = await Currency.GetDefaultFeePriceAsync();
        //            OnPropertyChanged(nameof(FeePriceString));

        //            UpdateTotalFeeString(maxAmountEstimation.Fee);
        //            OnPropertyChanged(nameof(TotalFeeString));
        //        }
        //        else
        //        {
        //            if (_fee < Currency.GetDefaultFee() || _feePrice == 0)
        //            {
        //                Warning = AppResources.LowFees;
        //                if (_fee == 0 || _feePrice == 0)
        //                {
        //                    _amount = 0;
        //                    OnPropertyChanged(nameof(AmountString));
        //                    return;
        //                }
        //            }

        //            var maxAmountEstimation = await account
        //                .EstimateMaxAmountToSendAsync(
        //                    from: new FromAddress(From),
        //                    to: To,
        //                    type: BlockchainTransactionType.Output,
        //                    fee: _fee,
        //                    feePrice: _feePrice,
        //                    reserve: false);

        //            _amount = maxAmountEstimation.Amount;

        //            if (maxAmountEstimation.Amount == 0 && availableAmount > 0)
        //                Warning = AppResources.InsufficientFunds;

        //            OnPropertyChanged(nameof(AmountString));

        //            UpdateTotalFeeString(maxAmountEstimation.Fee);
        //            OnPropertyChanged(nameof(TotalFeeString));
        //        }

        //        OnQuotesUpdatedEventHandler(AtomexApp.QuotesProvider, EventArgs.Empty);
        //    }
        //    finally
        //    {
        //        IsAmountUpdating = false;
        //    }
        //}

        //protected async override Task OnNextButtonClicked()
        //{
        //    if (string.IsNullOrEmpty(To))
        //    {
        //        Warning = AppResources.EmptyAddressError;
        //        return;
        //    }

        //    if (!Currency.IsValidAddress(To))
        //    {
        //        Warning = AppResources.InvalidAddressError;
        //        return;
        //    }

        //    if (Amount <= 0)
        //    {
        //        Warning = AppResources.AmountLessThanZeroError;
        //        return;
        //    }

        //    if (Fee <= 0)
        //    {
        //        Warning = AppResources.CommissionLessThanZeroError;
        //        return;
        //    }

        //    var isToken = Currency.FeeCurrencyName != Currency.Name;

        //    var feeAmount = !isToken ? Currency.GetFeeAmount(Fee, FeePrice) : 0;

        //    if (Amount + feeAmount > CurrencyViewModel.AvailableAmount)
        //    {
        //        Warning = AppResources.AvailableFundsError;
        //        return;
        //    }

        //    if (string.IsNullOrEmpty(Warning))
        //        await Navigation.PushAsync(new SendingConfirmationPage(this));
        //    else
        //        await App.Current.MainPage.DisplayAlert(AppResources.Error, Warning, AppResources.AcceptButton);
        //}

        //protected override void OnQuotesUpdatedEventHandler(object sender, EventArgs args)
        //{
        //    if (sender is not ICurrencyQuotesProvider quotesProvider)
        //        return;

        //    var quote = quotesProvider.GetQuote(CurrencyCode, BaseCurrencyCode);

        //    AmountInBase = Amount * (quote?.Bid ?? 0m);
        //    FeeInBase = Currency.GetFeeAmount(Fee, FeePrice) * (quote?.Bid ?? 0m);
        //}

        //protected override Task<Error> SendAsync(CancellationToken cancellationToken = default)
        //{
        //    var account = AtomexApp.Account.GetCurrencyAccount<EthereumAccount>(Currency.Name);

        //    return account.SendAsync(
        //        from: From,
        //        to: To,
        //        amount: Amount,
        //        gasLimit: Fee,
        //        gasPrice: FeePrice,
        //        useDefaultFee: UseDefaultFee,
        //        cancellationToken: cancellationToken);
        //}
    }
}

