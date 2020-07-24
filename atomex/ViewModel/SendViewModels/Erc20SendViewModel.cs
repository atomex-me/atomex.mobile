using System;
using System.Globalization;
using System.Threading.Tasks;
using atomex.Resources;
using Atomex.Blockchain.Abstract;
using Atomex.Core;
using Atomex.MarketData.Abstract;

namespace atomex.ViewModel.SendViewModels
{
    public class Erc20SendViewModel : EthereumSendViewModel
    {
        public override Currency Currency
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
            set { UpdateFeePrice(value); }
        }

        public Erc20SendViewModel(
            CurrencyViewModel currencyViewModel)
            : base(currencyViewModel)
        {
        }

        protected virtual async void UpdateFeePrice(decimal value)
        {
            Warning = string.Empty;

            _feePrice = value;

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
                var (maxAmount, maxFee, _) = await App.Account
                    .EstimateMaxAmountToSendAsync(Currency.Name, To, BlockchainTransactionType.Output, _fee, _feePrice, false);

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

                OnPropertyChanged(nameof(FeePriceString));

                UpdateTotalFeeString();
                OnPropertyChanged(nameof(TotalFeeString));
            }

            OnQuotesUpdatedEventHandler(App.QuotesProvider, EventArgs.Empty);
        }
            
        public override bool UseDefaultFee
        {
            get => _useDefaultFee;
            set
            {
                Warning = string.Empty;

                _useDefaultFee = value;
                OnPropertyChanged(nameof(UseDefaultFee));

                Amount = _amount; // recalculate amount
            }
        }

        public override async Task UpdateAmount(decimal amount)
        {
            Warning = string.Empty;

            var availableAmount = CurrencyViewModel.AvailableAmount;
            _amount = amount;

            if (UseDefaultFee)
            {
                var (maxAmount, maxFeeAmount, _) = await App.Account
                     .EstimateMaxAmountToSendAsync(Currency.Name, To, BlockchainTransactionType.Output, 0, 0, false);

                _fee = Currency.GetDefaultFee();
                OnPropertyChanged(nameof(GasString));

                _feePrice = Currency.GetDefaultFeePrice();
                OnPropertyChanged(nameof(FeePriceString));

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

                OnPropertyChanged(nameof(AmountString));

                UpdateTotalFeeString();

                OnPropertyChanged(nameof(TotalFeeString));
            }
            else
            {
                var (maxAmount, _, _) = await App.Account
                     .EstimateMaxAmountToSendAsync(Currency.Name, To, BlockchainTransactionType.Output, _fee, _feePrice, false);

                if (_amount > maxAmount)
                {
                    if (_amount <= availableAmount)
                        Warning = string.Format(CultureInfo.InvariantCulture, AppResources.InsufficientChainFunds, Currency.FeeCurrencyName);
                    else
                        Warning = string.Format(CultureInfo.InvariantCulture, AppResources.InsufficientFunds);

                    return;
                }

                OnPropertyChanged(nameof(AmountString));

                if (_fee < Currency.GetDefaultFee() || _feePrice == 0)
                    Warning = string.Format(CultureInfo.InvariantCulture, AppResources.LowFees);
            }

            OnQuotesUpdatedEventHandler(App.QuotesProvider, EventArgs.Empty);
        }

        public override async Task UpdateFee(decimal fee)
        {
            Warning = string.Empty;

            _fee = Math.Min(fee, Currency.GetMaximumFee());

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
                var (maxAmount, maxFee, _) = await App.Account
                        .EstimateMaxAmountToSendAsync(Currency.Name, To, BlockchainTransactionType.Output, _fee, _feePrice, false);

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
            OnQuotesUpdatedEventHandler(App.QuotesProvider, EventArgs.Empty);
        }

        public override async Task OnMaxClick()
        {
            Warning = string.Empty;

            var availableAmount = CurrencyViewModel.AvailableAmount;
            if (availableAmount == 0)
                return;
            if (UseDefaultFee)
            {
                var (maxAmount, maxFeeAmount, _) = await App.Account
                    .EstimateMaxAmountToSendAsync(Currency.Name, To, BlockchainTransactionType.Output, 0, 0, false);
                if (maxAmount > 0)
                    _amount = maxAmount;
                else if (CurrencyViewModel.AvailableAmount > 0)
                    Warning = string.Format(CultureInfo.InvariantCulture, AppResources.InsufficientChainFunds, Currency.FeeCurrencyName);

                OnPropertyChanged(nameof(AmountString));

                _fee = Currency.GetDefaultFee();
                OnPropertyChanged(nameof(GasString));

                _feePrice = Currency.GetDefaultFeePrice();
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
                var (maxAmount, maxFeeAmount, _) = await App.Account
                    .EstimateMaxAmountToSendAsync(Currency.Name, To, BlockchainTransactionType.Output, _fee, _feePrice, false);

                _amount = maxAmount;

                if (maxAmount < availableAmount)
                    Warning = string.Format(CultureInfo.InvariantCulture, AppResources.InsufficientChainFunds, Currency.FeeCurrencyName);

                OnPropertyChanged(nameof(AmountString));

                UpdateTotalFeeString(maxFeeAmount);
                OnPropertyChanged(nameof(TotalFeeString));
            }

            OnQuotesUpdatedEventHandler(App.QuotesProvider, EventArgs.Empty);
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
