using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using atomex.Resources;
using atomex.ViewModel.CurrencyViewModels;
using Atomex;
using Atomex.Blockchain.Abstract;
using Atomex.Blockchain.BitcoinBased;
using Atomex.Core;
using Atomex.Wallet.Abstract;
using Atomex.Wallet.BitcoinBased;

namespace atomex.ViewModel.SendViewModels
{
    public class BitcoinBasedSendViewModel : SendViewModel
    {
        public bool IsBtcBased => Currency is BitcoinBasedConfig;

        public BitcoinBasedSendViewModel(
            IAtomexApp app,
            CurrencyViewModel currencyViewModel)
            : base(app, currencyViewModel)
        {
        }

        // TODO: select outputs from UI
        public IEnumerable<BitcoinBasedTxOutput> Outputs { get; set; }

        protected decimal _feeRate;
        public decimal FeeRate
        {
            get => _feeRate;
            set { _feeRate = value; OnPropertyChanged(nameof(FeeRate)); }
        }

        private BitcoinBasedConfig Config => (BitcoinBasedConfig)Currency;

        protected override async void UpdateAmount(decimal amount)
        {
            IsAmountUpdating = true;

            _amount = amount;
            Warning = string.Empty;

            try
            {
                var account = AtomexApp.Account.GetCurrencyAccount<BitcoinBasedAccount>(Currency.Name);

                if (UseDefaultFee)
                {
                    FeeRate = await Config.GetFeeRateAsync();

                    var transactionParams = await BitcoinTransactionParams.SelectTransactionParamsByFeeRateAsync(
                        availableOutputs: Outputs,
                        to: _to,
                        amount: _amount,
                        feeRate: _feeRate,
                        account: account);

                    if (transactionParams == null)
                    {
                        Warning = AppResources.InsufficientFunds;
                        IsAmountUpdating = false;
                        return;
                    }

                    _fee = Config.SatoshiToCoin((long)transactionParams.FeeInSatoshi);
                    OnPropertyChanged(nameof(FeeString));
                }
                else
                {
                    var transactionParams = await BitcoinTransactionParams.SelectTransactionParamsByFeeAsync(
                        availableOutputs: Outputs,
                        to: _to,
                        amount: _amount,
                        fee: _fee,
                        account: account);

                    if (transactionParams == null)
                    {
                        Warning = AppResources.InsufficientFunds;
                        IsAmountUpdating = false;
                        return;
                    }

                    FeeRate = transactionParams.FeeRate;
                    //Fee = _fee; // recalculate fee
                }

                OnPropertyChanged(nameof(AmountString));

                OnQuotesUpdatedEventHandler(AtomexApp.QuotesProvider, EventArgs.Empty);
            }
            finally
            {
                IsAmountUpdating = false;
            }
        }

        protected override async void UpdateFee(decimal fee)
        {
            if (IsFeeUpdating)
                return;

            IsFeeUpdating = true;

            _fee = Math.Min(fee, Currency.GetMaximumFee());
            Warning = string.Empty;

            try
            {
                var account = AtomexApp.Account.GetCurrencyAccount<BitcoinBasedAccount>(Currency.Name);

                var transactionParams = await BitcoinTransactionParams.SelectTransactionParamsByFeeAsync(
                    availableOutputs: Outputs,
                    to: _to,
                    amount: _amount,
                    fee: _fee,
                    account: account);

                if (transactionParams == null)
                {
                    Warning = AppResources.InsufficientFunds;
                    IsFeeUpdating = false;
                    return;
                }

                var minimumFeeInSatoshi = Config.GetMinimumFee((int)transactionParams.Size);
                var minimumFee = Config.SatoshiToCoin(minimumFeeInSatoshi);

                if (_fee < minimumFee)
                    Warning = AppResources.LowFees;

                FeeRate = transactionParams.FeeRate;

                //OnPropertyChanged(nameof(AmountString));
                OnPropertyChanged(nameof(FeeString));

                OnQuotesUpdatedEventHandler(AtomexApp.QuotesProvider, EventArgs.Empty);
            }
            finally
            {
                IsFeeUpdating = false;
            }
        }

        protected override async void OnMaxClick()
        {
            if (IsAmountUpdating)
                return;

            IsAmountUpdating = true;
            Warning = string.Empty;

            try
            {
                if (UseDefaultFee) // auto fee
                {
                    if (AtomexApp.Account.GetCurrencyAccount(Currency.Name) is not IEstimatable account)
                        return; // todo: error?

                    FeeRate = await Config.GetFeeRateAsync();

                    var maxAmountEstimation = await account.EstimateMaxAmountToSendAsync(
                        from: new FromOutputs(Outputs),
                        to: _to,
                        type: BlockchainTransactionType.Output,
                        fee: 0,
                        feePrice: _feeRate,
                        reserve: false);

                    if (maxAmountEstimation.Amount > 0)
                        _amount = maxAmountEstimation.Amount;

                    OnPropertyChanged(nameof(AmountString));

                    _fee = maxAmountEstimation.Fee;
                    OnPropertyChanged(nameof(FeeString));
                }
                else // manual fee
                {
                    var availableInSatoshi = Outputs.Sum(o => o.Value);
                    var feeInSatoshi = Config.CoinToSatoshi(_fee);
                    var maxAmountInSatoshi = availableInSatoshi - feeInSatoshi;
                    var maxAmount = Config.SatoshiToCoin(maxAmountInSatoshi);

                    var account = AtomexApp.Account.GetCurrencyAccount<BitcoinBasedAccount>(Currency.Name);

                    var transactionParams = await BitcoinTransactionParams.SelectTransactionParamsByFeeAsync(
                        availableOutputs: Outputs,
                        to: _to,
                        amount: maxAmount,
                        fee: _fee,
                        account: account);

                    if (transactionParams == null)
                    {
                        Warning = AppResources.InsufficientFunds;
                        IsAmountUpdating = false;

                        _amount = 0;
                        OnPropertyChanged(nameof(AmountString));

                        return;
                    }

                    _amount = maxAmount;
                    OnPropertyChanged(nameof(AmountString));

                    FeeRate = transactionParams.FeeRate;
                }

                OnQuotesUpdatedEventHandler(AtomexApp.QuotesProvider, EventArgs.Empty);
            }
            finally
            {
                IsAmountUpdating = false;
            }
        }

        protected override Task<Error> SendAsync(CancellationToken cancellationToken = default)
        {
            var account = AtomexApp.Account.GetCurrencyAccount<BitcoinBasedAccount>(Currency.Name);

            return account.SendAsync(
                from: Outputs.ToList(),
                to: To,
                amount: Amount,
                fee: Fee,
                dustUsagePolicy: DustUsagePolicy.AddToFee,
                cancellationToken: cancellationToken);
        }
    }
}