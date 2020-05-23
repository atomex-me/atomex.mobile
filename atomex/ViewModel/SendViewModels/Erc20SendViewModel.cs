using System;
using System.Linq;
using System.Threading.Tasks;
using Atomex;
using Atomex.Blockchain.Abstract;
using Atomex.Common;
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
            _feePrice = value;

            if (!UseDefaultFee)
            {
                var feeAmount = Currency.GetFeeAmount(_fee, _feePrice);

                var ethAddress = (await App.Account
                    .GetUnspentAddressesAsync(Currency.FeeCurrencyName))
                    .ToList()
                    ?.MaxBy(w => w.AvailableBalance());

                if (ethAddress != null)
                {
                    feeAmount = Math.Min(ethAddress.AvailableBalance(), feeAmount);

                    _feePrice = Currency.GetFeePriceFromFeeAmount(feeAmount, _fee);
                }

                OnPropertyChanged(nameof(FeePriceString));
                OnPropertyChanged(nameof(TotalFeeString));
            }

            OnQuotesUpdatedEventHandler(App.QuotesProvider, EventArgs.Empty);
        }

        public override async Task UpdateAmount(decimal amount)
        {

            var previousAmount = _amount;
            _amount = amount;

            if (UseDefaultFee)
            {
                var (maxAmount, maxFeeAmount, _) = await App.Account
                    .EstimateMaxAmountToSendAsync(Currency.Name, To, BlockchainTransactionType.Output, true);

                var availableAmount = Currency is BitcoinBasedCurrency
                    ? CurrencyViewModel.AvailableAmount
                    : maxAmount;

                var estimatedFeeAmount = _amount != 0
                    ? (_amount < availableAmount
                        ? await App.Account.EstimateFeeAsync(Currency.Name, To, _amount, BlockchainTransactionType.Output)
                        : null)
                    : 0;

                if (estimatedFeeAmount == null)
                {
                    if (maxAmount > 0)
                    {
                        _amount = maxAmount;
                        estimatedFeeAmount = maxFeeAmount;
                    }
                    else
                    {
                        _amount = previousAmount;
                        return;
                    }
                }

                if (_amount > availableAmount)
                    _amount = Math.Max(availableAmount, 0);

                if (_amount == 0)
                    estimatedFeeAmount = 0;

                OnPropertyChanged(nameof(AmountString));

                _fee = Currency.GetFeeFromFeeAmount(estimatedFeeAmount.Value, Currency.GetDefaultFeePrice());
                OnPropertyChanged(nameof(GasString));

                _feePrice = Currency.GetDefaultFeePrice();
                OnPropertyChanged(nameof(FeePriceString));

                OnPropertyChanged(nameof(TotalFeeString));

            }
            else
            {
                var (maxAmount, _, _) = await App.Account
                    .EstimateMaxAmountToSendAsync(Currency.Name, To, BlockchainTransactionType.Output, false);

                var availableAmount = Currency is BitcoinBasedCurrency
                    ? CurrencyViewModel.AvailableAmount
                    : maxAmount;

                if (_amount > availableAmount)
                    _amount = Math.Max(availableAmount, 0);

                OnPropertyChanged(nameof(AmountString));

                if (_fee != 0)
                    Fee = _fee;

                OnPropertyChanged(nameof(TotalFeeString));

            }

            OnQuotesUpdatedEventHandler(App.QuotesProvider, EventArgs.Empty);
        }

        public override async Task UpdateFee(decimal fee)
        {
            if (_amount == 0)
            {
                _fee = 0;
                return;
            }

            _fee = Math.Min(fee, Currency.GetMaximumFee());

            if (!UseDefaultFee)
            {
                var estimatedFeeAmount = _amount != 0
                    ? await App.Account.EstimateFeeAsync(Currency.Name, To, _amount, BlockchainTransactionType.Output)
                    : 0;

                var feeAmount = Currency.GetFeeAmount(_fee, _feePrice);

                if (feeAmount < estimatedFeeAmount.Value)
                    _fee = Currency.GetFeeFromFeeAmount(estimatedFeeAmount.Value, Currency.GetDefaultFeePrice());

                if (_amount == 0)
                    _fee = 0;

                OnPropertyChanged(nameof(AmountString));
                OnPropertyChanged(nameof(GasString));
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
