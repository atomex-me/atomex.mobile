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
    public class Erc20SendViewModel : EthereumSendViewModel
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

        public override string TotalFeeCurrencyCode => Currency.FeeCurrencyName;

        public override decimal FeePrice
        {
            get => _feePrice;
            set { _ = UpdateFeePrice(value); }
        }

        public Erc20SendViewModel(
            CurrencyViewModel currencyViewModel)
            : base(currencyViewModel)
        {
        }

        public override async Task UpdateFeePrice(decimal value)
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
                        var availableAmount = CurrencyViewModel.AvailableAmount;

                        if (_amount <= availableAmount)
                            Warning = string.Format(CultureInfo.InvariantCulture, AppResources.InsufficientChainFunds, Currency.FeeCurrencyName);
                        else
                        {
                            Warning = string.Format(CultureInfo.InvariantCulture, AppResources.InsufficientFunds);
                        }
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
                Log.Error(e, "Erc20 update fee price error");
            }
        }
            
        public override bool UseDefaultFee
        {
            get => _useDefaultFee;
            set
            {
                Warning = string.Empty;

                _useDefaultFee = value;
                OnPropertyChanged(nameof(UseDefaultFee));

                _ = UpdateAmount(_amount);
            }
        }

        public override async Task UpdateAmount(decimal amount, bool raiseOnPropertyChanged = true)
        {
            Warning = string.Empty;

            try
            {

                if (amount == 0)
                {
                    ResetSendValues(raiseOnPropertyChanged);
                    return;
                }

                var availableAmount = CurrencyViewModel.AvailableAmount;
                _amount = amount;

                var account = AtomexApp.Account
                  .GetCurrencyAccount<ILegacyCurrencyAccount>(Currency.Name);

                if (UseDefaultFee)
                {
                    var (maxAmount, _, _) = await account
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
                    OnPropertyChanged(nameof(FeePrice));

                    if (_amount > maxAmount)
                    {
                        if (_amount <= availableAmount)
                            Warning = string.Format(CultureInfo.InvariantCulture, AppResources.InsufficientChainFunds, Currency.FeeCurrencyName);
                        else
                        {
                            Warning = string.Format(CultureInfo.InvariantCulture, AppResources.InsufficientFunds);
                        }
                        return;
                    }

                    if (raiseOnPropertyChanged)
                        OnPropertyChanged(nameof(AmountString));

                    UpdateTotalFeeString();

                    OnPropertyChanged(nameof(TotalFeeString));
                }
                else
                {
                    var (maxAmount, _, _) = await account
                         .EstimateMaxAmountToSendAsync(
                            to: To,
                            type: BlockchainTransactionType.Output,
                            fee: _fee,
                            feePrice: _feePrice,
                            reserve: false);

                    if (_amount > maxAmount)
                    {
                        if (_amount <= availableAmount)
                            Warning = string.Format(CultureInfo.InvariantCulture, AppResources.InsufficientChainFunds, Currency.FeeCurrencyName);
                        else
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
            catch (Exception e)
            {
                Log.Error(e, "Erc20 update amount error");
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

                    var (maxAmount, _, _) = await account
                        .EstimateMaxAmountToSendAsync(
                            to: To,
                            type: BlockchainTransactionType.Output,
                            fee: _fee,
                            feePrice: _feePrice,
                            reserve: false);

                    if (_amount > maxAmount)
                    {
                        var availableAmount = CurrencyViewModel.AvailableAmount;

                        if (_amount <= availableAmount)
                            Warning = string.Format(CultureInfo.InvariantCulture, AppResources.InsufficientChainFunds, Currency.FeeCurrencyName);
                        else
                            Warning = string.Format(CultureInfo.InvariantCulture, AppResources.InsufficientFunds);
                        return;
                    }

                    OnPropertyChanged(nameof(FeePriceString));

                    UpdateTotalFeeString();
                    OnPropertyChanged(nameof(TotalFeeString));

                }
                OnQuotesUpdatedEventHandler(AtomexApp.QuotesProvider, EventArgs.Empty);
            }
            catch (Exception e)
            {
                Log.Error(e, "Erc20 update fee error");
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
                    else if (CurrencyViewModel.AvailableAmount > 0)
                        Warning = string.Format(CultureInfo.InvariantCulture, AppResources.InsufficientChainFunds, Currency.FeeCurrencyName);

                    OnPropertyChanged(nameof(AmountString));

                    _fee = Currency.GetDefaultFee();
                    OnPropertyChanged(nameof(GasString));

                    _feePrice = await Currency.GetDefaultFeePriceAsync();
                    OnPropertyChanged(nameof(FeePriceString));

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
                        .EstimateMaxAmountToSendAsync(
                        to: To,
                        type: BlockchainTransactionType.Output,
                        fee:_fee,
                        feePrice: _feePrice,
                        reserve: false);

                    _amount = maxAmount;

                    if (maxAmount < availableAmount)
                        Warning = string.Format(CultureInfo.InvariantCulture, AppResources.InsufficientChainFunds, Currency.FeeCurrencyName);

                    OnPropertyChanged(nameof(AmountString));

                    UpdateTotalFeeString(maxFeeAmount);
                    OnPropertyChanged(nameof(TotalFeeString));
                }

                OnQuotesUpdatedEventHandler(AtomexApp.QuotesProvider, EventArgs.Empty);
            }
            catch (Exception e)
            {
                Log.Error(e, "Erc20 max click error");
            }
        }
         

        protected override void OnQuotesUpdatedEventHandler(object sender, EventArgs args)
        {
            if (!(sender is ICurrencyQuotesProvider quotesProvider))
                return;

            var quote = quotesProvider.GetQuote(CurrencyCode, BaseCurrencyCode);
            var ethQuote = quotesProvider.GetQuote(Currency.FeeCurrencyName, BaseCurrencyCode);

            AmountInBase = Amount * (quote?.Bid ?? 0m);
            FeeInBase = Currency.GetFeeAmount(Fee, FeePrice) * (ethQuote?.Bid ?? 0m);
        }
    }
}
