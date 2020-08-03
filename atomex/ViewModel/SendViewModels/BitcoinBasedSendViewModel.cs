using System;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using atomex.Resources;
using Atomex;
using Atomex.Blockchain.Abstract;
using Atomex.Core;
using Atomex.Wallet.BitcoinBased;

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
            Warning = string.Empty;

            _amount = amount;

            try
            {
                if (UseDefaultFee)
                {
                    var (maxAmount, _, _) = await App.Account
                        .EstimateMaxAmountToSendAsync(Currency.Name, To, BlockchainTransactionType.Output);

                    if (_amount > maxAmount)
                    {
                        Warning = string.Format(CultureInfo.InvariantCulture, AppResources.InsufficientFunds);
                        return;
                    }

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
                    var availableAmount = CurrencyViewModel.AvailableAmount;

                    if (_amount + _fee > availableAmount)
                    {
                        Warning = string.Format(CultureInfo.InvariantCulture, AppResources.InsufficientFunds);
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
            Warning = string.Empty;

            _fee = Math.Min(fee, Currency.GetMaximumFee());

            try
            {
                var availableAmount = CurrencyViewModel.AvailableAmount;

                if (_amount == 0)
                {
                    if (Currency.GetFeeAmount(_fee, Currency.GetDefaultFeePrice()) > availableAmount)
                        Warning = string.Format(CultureInfo.InvariantCulture, AppResources.InsufficientFunds);
                    return;
                }
                else if (_amount + _fee > availableAmount)
                {
                    Warning = string.Format(CultureInfo.InvariantCulture, AppResources.InsufficientFunds);
                    return;
                }
                
                var estimatedTxSize = await EstimateTxSizeAsync(_amount, _fee);

                if (estimatedTxSize == null || estimatedTxSize.Value == 0)
                {
                    Warning = string.Format(CultureInfo.InvariantCulture, AppResources.InsufficientFunds);
                    return;
                }

                if (!UseDefaultFee)
                {
                    var minimumFeeSatoshi = BtcBased.GetMinimumFee(estimatedTxSize.Value);
                    var minimumFee = BtcBased.SatoshiToCoin(minimumFeeSatoshi);

                    if (_fee < minimumFee)
                        Warning = string.Format(CultureInfo.InvariantCulture, AppResources.LowFees);
                }

                FeeRate = BtcBased.CoinToSatoshi(_fee) / estimatedTxSize.Value;

                OnPropertyChanged(nameof(AmountString));
                OnPropertyChanged(nameof(FeeString));

                OnQuotesUpdatedEventHandler(App.QuotesProvider, EventArgs.Empty);
            }
            catch { }
        }

        public override async Task OnMaxClick()
        {
            Warning = string.Empty;

            try
            {
                if (CurrencyViewModel.AvailableAmount == 0)
                    return;

                if (UseDefaultFee) // auto fee
                {
                    var (maxAmount, maxFeeAmount, _) = await App.Account
                        .EstimateMaxAmountToSendAsync(Currency.Name, To, BlockchainTransactionType.Output);

                    if (maxAmount > 0)
                        _amount = maxAmount;

                    OnPropertyChanged(nameof(AmountString));

                    _fee = Currency.GetFeeFromFeeAmount(maxFeeAmount, Currency.GetDefaultFeePrice());
                    OnPropertyChanged(nameof(FeeString));

                    FeeRate = BtcBased.FeeRate;
                }
                else // manual fee
                {
                    var availableAmount = CurrencyViewModel.AvailableAmount;

                    if (availableAmount - _fee > 0)
                    {
                        _amount = availableAmount - _fee;
                    }
                    else
                    {
                        _amount = 0;
                        Warning = string.Format(CultureInfo.InvariantCulture, AppResources.InsufficientFunds);

                        OnPropertyChanged(nameof(AmountString));

                        return;
                    }

                    var estimatedTxSize = await EstimateTxSizeAsync(_amount, _fee);

                    if (estimatedTxSize == null || estimatedTxSize.Value == 0)
                    {
                        Warning = string.Format(CultureInfo.InvariantCulture, AppResources.InsufficientFunds);
                        return;
                    }

                    FeeRate = BtcBased.CoinToSatoshi(_fee) / estimatedTxSize.Value;
                }

                OnPropertyChanged(nameof(AmountString));
                OnPropertyChanged(nameof(FeeString));

                OnQuotesUpdatedEventHandler(App.QuotesProvider, EventArgs.Empty);
            }
            catch { }
        }

        private async Task<int?> EstimateTxSizeAsync(
            decimal amount,
            decimal fee,
            CancellationToken cancellationToken = default)
        {
            return await App.Account
                .GetCurrencyAccount<BitcoinBasedAccount>(Currency.Name)
                .EstimateTxSizeAsync(amount, fee, cancellationToken);
        }
    }
}