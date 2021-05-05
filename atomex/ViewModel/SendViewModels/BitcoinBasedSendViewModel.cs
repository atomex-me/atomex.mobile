using System;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using atomex.Resources;
using Atomex;
using Atomex.Blockchain.Abstract;
using Atomex.Core;
using Atomex.Wallet.BitcoinBased;
using Xamarin.Forms;

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
                if (UseDefaultFee)
                {
                    var (maxAmount, _, _) = await AtomexApp.Account
                        .EstimateMaxAmountToSendAsync(Currency.Name, To, BlockchainTransactionType.Output);

                    if (_amount > maxAmount)
                    {
                        Warning = string.Format(CultureInfo.InvariantCulture, AppResources.InsufficientFunds);
                        return;
                    }

                    var estimatedFeeAmount = _amount != 0
                        ? await AtomexApp.Account.EstimateFeeAsync(Currency.Name, To, _amount, BlockchainTransactionType.Output)
                        : 0;

                    if (raiseOnPropertyChanged)
                        OnPropertyChanged(nameof(AmountString));

                    _fee = estimatedFeeAmount ?? Currency.GetDefaultFee();
                    OnPropertyChanged(nameof(FeeString));

                    FeeRate = await BtcBased.GetFeeRateAsync();
                }
                else
                {
                    var availableAmount = CurrencyViewModel.AvailableAmount;

                    if (_amount + _fee > availableAmount)
                    {
                        Warning = string.Format(CultureInfo.InvariantCulture, AppResources.InsufficientFunds);
                        return;
                    }

                    if (raiseOnPropertyChanged)
                        OnPropertyChanged(nameof(AmountString));

                    Fee = _fee;
                }

                OnQuotesUpdatedEventHandler(AtomexApp.QuotesProvider, EventArgs.Empty);
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
                    var defaultFeePrice = await Currency.GetDefaultFeePriceAsync();

                    if (Currency.GetFeeAmount(_fee, defaultFeePrice) > availableAmount)
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

                OnQuotesUpdatedEventHandler(AtomexApp.QuotesProvider, EventArgs.Empty);
            }
            catch { }
        }

        private ICommand _maxAmountCommand;
        public override ICommand MaxAmountCommand => _maxAmountCommand ??= new Command(async () => await OnMaxClick());

        protected override async Task OnMaxClick()
        {
            Warning = string.Empty;

            try
            {
                if (CurrencyViewModel.AvailableAmount == 0)
                    return;

                if (UseDefaultFee)
                {
                    var (maxAmount, maxFeeAmount, _) = await AtomexApp.Account
                        .EstimateMaxAmountToSendAsync(Currency.Name, To, BlockchainTransactionType.Output);

                    if (maxAmount > 0)
                        _amount = maxAmount;

                    OnPropertyChanged(nameof(AmountString));

                    var defaultFeePrice = await Currency.GetDefaultFeePriceAsync();

                    _fee = Currency.GetFeeFromFeeAmount(maxFeeAmount, defaultFeePrice);
                    OnPropertyChanged(nameof(FeeString));

                    FeeRate = await BtcBased.GetFeeRateAsync();
                }
                else
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

                OnQuotesUpdatedEventHandler(AtomexApp.QuotesProvider, EventArgs.Empty);
            }
            catch { }
        }

        private async Task<int?> EstimateTxSizeAsync(
            decimal amount,
            decimal fee,
            CancellationToken cancellationToken = default)
        {
            return await AtomexApp.Account
                .GetCurrencyAccount<BitcoinBasedAccount>(Currency.Name)
                .EstimateTxSizeAsync(amount, fee, cancellationToken);
        }
    }
}