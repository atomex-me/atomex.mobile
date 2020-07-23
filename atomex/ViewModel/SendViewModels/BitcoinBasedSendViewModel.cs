using System;
using System.Threading.Tasks;
using Atomex;
using Atomex.Blockchain.Abstract;
using Atomex.Core;

namespace atomex.ViewModel.SendViewModels
{
    public class BitcoinBasedSendViewModel : SendViewModel
    {

        protected decimal _feeRate;
        public decimal FeeRate
        {
            get => _feeRate;
            set { _feeRate = value; OnPropertyChanged(nameof(FeeRate)); }
        }

        private BitcoinBasedCurrency BtcBased => Currency as BitcoinBasedCurrency;

        public BitcoinBasedSendViewModel(
            CurrencyViewModel currencyViewModel)
            : base(currencyViewModel)
        {
        }

        public override async Task UpdateAmount(decimal amount)
        {
            var previousAmount = _amount;
            _amount = amount;

            try
            {
                if (UseDefaultFee)
                {
                    var (maxAmount, maxFeeAmount, _) = await App.Account
                        .EstimateMaxAmountToSendAsync(Currency.Name, To, BlockchainTransactionType.Output);

                    if (_amount > maxAmount)
                    {
                        //!! Warning = Resources.CvInsufficientFunds;
                        return;
                    }

                    var availableAmount = CurrencyViewModel.AvailableAmount;

                    var estimatedFeeAmount = _amount != 0
                        ? await App.Account.EstimateFeeAsync(Currency.Name, To, _amount, BlockchainTransactionType.Output)
                        : 0;

                    OnPropertyChanged(nameof(AmountString));

                    _fee = estimatedFeeAmount ?? Currency.GetDefaultFee();
                    OnPropertyChanged(nameof(FeeString));

                    FeeRate = BtcBased.FeeRate;
                }
                else
                {
                    var (maxAmount, maxFeeAmount, _) = await App.Account
                         .EstimateMaxAmountToSendAsync(Currency.Name, To, BlockchainTransactionType.Output);

                    var availableAmount = Currency is BitcoinBasedCurrency
                        ? CurrencyViewModel.AvailableAmount
                        : maxAmount + maxFeeAmount;

                    var feeAmount = Currency.GetFeeAmount(_fee, Currency.GetDefaultFeePrice());

                    if (_amount + feeAmount > availableAmount)
                    {
                        //!!Warning = Resources.CvInsufficientFunds;
                        return;
                    }

                    OnPropertyChanged(nameof(AmountString));

                    Fee = _fee;
                }

                OnQuotesUpdatedEventHandler(App.QuotesProvider, EventArgs.Empty);
            }
            catch { }
        }

        public override async Task UpdateFee(decimal fee)
        {
            if (_amount == 0)
            {
                _fee = 0;
                return;
            }

            try
            {
                _fee = Math.Min(fee, Currency.GetMaximumFee());

                var estimatedTxSize = await EstimateTxSizeAsync();

                if (!UseDefaultFee)
                {
                    var minimumFeeSatoshi = BtcBased.GetMinimumFee(estimatedTxSize);
                    var minimumFee = BtcBased.SatoshiToCoin(minimumFeeSatoshi);

                    if (_fee < minimumFee)
                        _fee = minimumFee;

                    var availableAmount = CurrencyViewModel.AvailableAmount;

                    if (_amount + _fee > availableAmount)
                        _amount = Math.Max(availableAmount - _fee, 0);

                    if (_amount == 0)
                        _fee = 0;

                    OnPropertyChanged(nameof(AmountString));
                    OnPropertyChanged(nameof(FeeString));
                }

                FeeRate = BtcBased.CoinToSatoshi(_fee) / estimatedTxSize;

                OnQuotesUpdatedEventHandler(App.QuotesProvider, EventArgs.Empty);
            }
            catch { }
        }

        private async Task<int> EstimateTxSizeAsync()
        {
            var estimatedFee = await App.Account
                .EstimateFeeAsync(Currency.Name, To, _amount, BlockchainTransactionType.Output);

            if (estimatedFee == null)
                return 0;

            return (int)(BtcBased.CoinToSatoshi(estimatedFee.Value) / BtcBased.FeeRate);
        }
    }
}