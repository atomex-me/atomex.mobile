using System;
using System.Threading;
using System.Threading.Tasks;
using atomex.Resources;
using atomex.ViewModel.CurrencyViewModels;
using Atomex;
using Atomex.Blockchain.Abstract;
using Atomex.Core;
using Atomex.Wallet.Abstract;
using Atomex.Wallet.Tezos;

namespace atomex.ViewModel.SendViewModels
{
    public class TezosSendViewModel : SendViewModel
    {
        //public string From { get; set; }

        public TezosSendViewModel(
            IAtomexApp app,
            CurrencyViewModel currencyViewModel)
            : base(app, currencyViewModel)
        {
        }

        protected override Task UpdateAmount(decimal amount)
        {
            throw new NotImplementedException();
        }

        protected override Task UpdateFee(decimal fee)
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

        protected override Task FromClick()
        {
            throw new NotImplementedException();
        }

        protected override Task ToClick()
        {
            throw new NotImplementedException();
        }

        //protected override async void UpdateAmount(decimal amount)
        //{
        //    if (IsAmountUpdating)
        //        return;

        //    IsAmountUpdating = true;

        //    Warning = string.Empty;

        //    _amount = amount;

        //    try
        //    {
        //        if (AtomexApp.Account.GetCurrencyAccount(Currency.Name) is not IEstimatable account)
        //            return; // todo: error?

        //        var maxAmountEstimation = await account.EstimateMaxAmountToSendAsync(
        //            from: new FromAddress(From),
        //            to: _to,
        //            type: BlockchainTransactionType.Output,
        //            fee: UseDefaultFee ? 0 : _fee,
        //            feePrice: 0,
        //            reserve: false);

        //        if (UseDefaultFee)
        //        {
        //            if (_amount > maxAmountEstimation.Amount)
        //            {
        //                Warning = AppResources.InsufficientFunds;
        //                IsAmountUpdating = false;
        //                return;
        //            }

        //            var estimatedFeeAmount = _amount != 0
        //                ? await account.EstimateFeeAsync(
        //                    from: new FromAddress(From),
        //                    to: To,
        //                    amount: _amount,
        //                    type: BlockchainTransactionType.Output)
        //                : 0;

        //            OnPropertyChanged(nameof(AmountString));

        //            _fee = estimatedFeeAmount ?? Currency.GetDefaultFee();
        //            OnPropertyChanged(nameof(FeeString));
        //        }
        //        else
        //        {
        //            var availableAmount = maxAmountEstimation.Amount + maxAmountEstimation.Fee;

        //            if (_amount > maxAmountEstimation.Amount || _amount + _fee > availableAmount)
        //            {
        //                Warning = AppResources.InsufficientFunds;
        //                IsAmountUpdating = false;
        //                return;
        //            }

        //            OnPropertyChanged(nameof(AmountString));

        //            Fee = _fee;
        //        }

        //        OnQuotesUpdatedEventHandler(AtomexApp.QuotesProvider, EventArgs.Empty);
        //    }
        //    finally
        //    {
        //        IsAmountUpdating = false;
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
        //            if (_fee > CurrencyViewModel.AvailableAmount)
        //                Warning = AppResources.InsufficientFunds;

        //            return;
        //        }

        //        if (!UseDefaultFee)
        //        {
        //            if (AtomexApp.Account.GetCurrencyAccount(Currency.Name) is not IEstimatable account)
        //                return; // todo: error?

        //            var estimatedFeeAmount = _amount != 0
        //                ? await account.EstimateFeeAsync(
        //                    from: new FromAddress(From),
        //                    to: To,
        //                    amount: _amount,
        //                    type: BlockchainTransactionType.Output)
        //                : 0;

        //            var maxAmountEstimation = await account
        //                .EstimateMaxAmountToSendAsync(
        //                    from: new FromAddress(From),
        //                    to: To,
        //                    type: BlockchainTransactionType.Output,
        //                    fee: 0,
        //                    feePrice: 0,
        //                    reserve: false);

        //            var availableAmount = Currency is BitcoinBasedConfig
        //                ? CurrencyViewModel.AvailableAmount
        //                : maxAmountEstimation.Amount + maxAmountEstimation.Fee;

        //            if (_amount + _fee > availableAmount)
        //            {
        //                Warning = AppResources.InsufficientFunds;
        //                IsAmountUpdating = false;
        //                return;
        //            }
        //            else if (estimatedFeeAmount == null || _fee < estimatedFeeAmount.Value)
        //            {
        //                Warning = AppResources.LowFees;
        //            }

        //            OnPropertyChanged(nameof(FeeString));
        //        }

        //        OnQuotesUpdatedEventHandler(AtomexApp.QuotesProvider, EventArgs.Empty);
        //    }
        //    finally
        //    {
        //        IsFeeUpdating = false;
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
        //        if (CurrencyViewModel.AvailableAmount == 0)
        //            return;

        //        if (AtomexApp.Account.GetCurrencyAccount(Currency.Name) is not IEstimatable account)
        //            return; // todo: error?

        //        var maxAmountEstimation = await account.EstimateMaxAmountToSendAsync(
        //            from: new FromAddress(From),
        //            to: _to,
        //            type: BlockchainTransactionType.Output,
        //            fee: 0,
        //            feePrice: 0,
        //            reserve: UseDefaultFee);

        //        if (UseDefaultFee)
        //        {
        //            if (maxAmountEstimation.Amount > 0)
        //                _amount = maxAmountEstimation.Amount;

        //            OnPropertyChanged(nameof(AmountString));

        //            _fee = maxAmountEstimation.Fee;
        //            OnPropertyChanged(nameof(FeeString));
        //        }
        //        else
        //        {
        //            var availableAmount = maxAmountEstimation.Amount + maxAmountEstimation.Fee;

        //            if (availableAmount - _fee > 0)
        //            {
        //                _amount = availableAmount - _fee;

        //                var estimatedFeeAmount = _amount != 0
        //                    ? await account.EstimateFeeAsync(
        //                        from: new FromAddress(From),
        //                        to: To,
        //                        amount: _amount,
        //                        type: BlockchainTransactionType.Output)
        //                    : 0;

        //                if (estimatedFeeAmount == null || _fee < estimatedFeeAmount.Value)
        //                {
        //                    Warning = AppResources.LowFees;
        //                    if (_fee == 0)
        //                    {
        //                        _amount = 0;
        //                        OnPropertyChanged(nameof(AmountString));
        //                        return;
        //                    }
        //                }
        //            }
        //            else
        //            {
        //                _amount = 0;

        //                Warning = AppResources.InsufficientFunds;
        //            }

        //            OnPropertyChanged(nameof(AmountString));
        //            OnPropertyChanged(nameof(FeeString));
        //        }

        //        OnQuotesUpdatedEventHandler(AtomexApp.QuotesProvider, EventArgs.Empty);
        //    }
        //    finally
        //    {
        //        IsAmountUpdating = false;
        //    }
        //}

        //protected override Task<Error> SendAsync(CancellationToken cancellationToken = default)
        //{
        //    var account = AtomexApp.Account.GetCurrencyAccount<TezosAccount>(Currency.Name);

        //    return account.SendAsync(
        //        from: From,
        //        to: To,
        //        amount: Amount,
        //        fee: Fee,
        //        useDefaultFee: UseDefaultFee,
        //        cancellationToken: cancellationToken);
        //}
    }
}