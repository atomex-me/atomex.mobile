using System;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using atomex.Resources;
using atomex.ViewModel.CurrencyViewModels;
using Atomex;
using Atomex.Blockchain.Abstract;
using Atomex.Core;
using Atomex.MarketData.Abstract;
using Atomex.TezosTokens;
using Atomex.Wallet.Abstract;
using Atomex.Wallet.Tezos;

namespace atomex.ViewModel.SendViewModels
{
    public class Fa12SendViewModel : SendViewModel
    {

        public Fa12SendViewModel(
            IAtomexApp app,
            CurrencyViewModel currencyViewModel)
            : base(app, currencyViewModel)
        {
        }

        public string From { get; set; }

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

        protected override async void UpdateAmount(decimal amount)
        {
            IsAmountUpdating = true;

            var availableAmount = CurrencyViewModel.AvailableAmount;
            _amount = amount;

            Warning = string.Empty;

            try
            {
                if (AtomexApp.Account.GetCurrencyAccount(Currency.Name) is not IEstimatable account)
                    return; // todo: error?

                var (maxAmount, _, _) = await account.EstimateMaxAmountToSendAsync(
                    from: new FromAddress(From),
                    to: _to,
                    type: BlockchainTransactionType.Output,
                    fee: 0,
                    feePrice: 0,
                    reserve: false);

                if (UseDefaultFee)
                {
                    if (_amount > maxAmount)
                    {
                        if (_amount <= availableAmount)
                            Warning = string.Format(CultureInfo.InvariantCulture, AppResources.InsufficientChainFunds, Currency.FeeCurrencyName);
                        else
                            Warning = AppResources.InsufficientFunds;

                        IsAmountUpdating = false;
                        return;
                    }

                    var estimatedFeeAmount = _amount != 0
                        ? await account.EstimateFeeAsync(
                            from: new FromAddress(From),
                            to: To,
                            amount: _amount,
                            type: BlockchainTransactionType.Output)
                        : 0;

                    OnPropertyChanged(nameof(AmountString));

                    _fee = estimatedFeeAmount ?? Currency.GetDefaultFee();
                    OnPropertyChanged(nameof(FeeString));
                }
                else
                {
                    if (_amount > maxAmount)
                    {
                        if (_amount <= availableAmount)
                            Warning = string.Format(CultureInfo.InvariantCulture, AppResources.InsufficientChainFunds, Currency.FeeCurrencyName);
                        else
                            Warning = AppResources.InsufficientFunds;

                        IsAmountUpdating = false;
                        return;
                    }

                    OnPropertyChanged(nameof(AmountString));

                    Fee = _fee;
                }

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
                if (_amount == 0)
                {
                    if (_fee > CurrencyViewModel.AvailableAmount)
                        Warning = AppResources.InsufficientFunds;

                    return;
                }

                if (!UseDefaultFee)
                {
                    var availableAmount = CurrencyViewModel.AvailableAmount;

                    if (AtomexApp.Account.GetCurrencyAccount(Currency.Name) is not IEstimatable account)
                        return; // todo: error?

                    var (maxAmount, maxAvailableFee, _) = await account.EstimateMaxAmountToSendAsync(
                        from: new FromAddress(From),
                        to: _to,
                        type: BlockchainTransactionType.Output,
                        fee: 0,
                        feePrice: 0,
                        reserve: false);

                    var estimatedFeeAmount = _amount != 0
                        ? await account.EstimateFeeAsync(
                            from: new FromAddress(From),
                            to: To,
                            amount: _amount,
                            type: BlockchainTransactionType.Output)
                        : 0;

                    if (_amount > maxAmount)
                    {
                        if (_amount <= availableAmount)
                            Warning = string.Format(CultureInfo.InvariantCulture, AppResources.InsufficientChainFunds, Currency.FeeCurrencyName);
                        else
                            Warning = AppResources.InsufficientFunds;

                        return;
                    }
                    else if (estimatedFeeAmount == null || _fee < estimatedFeeAmount.Value)
                    {
                        Warning = AppResources.LowFees;
                    }

                    if (_fee > maxAvailableFee)
                        Warning = string.Format(CultureInfo.InvariantCulture, AppResources.InsufficientChainFunds, Currency.FeeCurrencyName);

                    OnPropertyChanged(nameof(FeeString));
                }

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
                var availableAmount = CurrencyViewModel.AvailableAmount;

                if (availableAmount == 0)
                    return;

                if (AtomexApp.Account.GetCurrencyAccount(Currency.Name) is not IEstimatable account)
                    return; // todo: error?

                var (maxAmount, maxFeeAmount, _) = await account.EstimateMaxAmountToSendAsync(
                    from: new FromAddress(From),
                    to: _to,
                    type: BlockchainTransactionType.Output,
                    fee: 0,
                    feePrice: 0,
                    reserve: UseDefaultFee);

                if (UseDefaultFee)
                {
                    if (maxAmount > 0)
                        _amount = maxAmount;
                    else
                        Warning = string.Format(CultureInfo.InvariantCulture, AppResources.InsufficientChainFunds, Currency.FeeCurrencyName);

                    OnPropertyChanged(nameof(AmountString));

                    _fee = maxFeeAmount;
                    OnPropertyChanged(nameof(FeeString));
                }
                else
                {
                    if (_fee < maxFeeAmount)
                    {
                        Warning = AppResources.LowFees;
                        if (_fee == 0)
                        {
                            _amount = 0;
                            OnPropertyChanged(nameof(AmountString));
                            return;
                        }
                    }

                    _amount = maxAmount;

                    if (maxAmount < availableAmount || _fee > maxFeeAmount)
                        Warning = string.Format(CultureInfo.InvariantCulture, AppResources.InsufficientChainFunds, Currency.FeeCurrencyName);

                    OnPropertyChanged(nameof(AmountString));
                    OnPropertyChanged(nameof(FeeString));
                }

                OnQuotesUpdatedEventHandler(AtomexApp.QuotesProvider, EventArgs.Empty);
            }
            finally
            {
                IsAmountUpdating = false;
            }
        }

        protected override void OnQuotesUpdatedEventHandler(object sender, EventArgs args)
        {
            if (sender is not ICurrencyQuotesProvider quotesProvider)
                return;

            var quote = quotesProvider.GetQuote(CurrencyCode, BaseCurrencyCode);
            var xtzQuote = quotesProvider.GetQuote("XTZ", BaseCurrencyCode);

            AmountInBase = Amount * (quote?.Bid ?? 0m);
            FeeInBase = Fee * (xtzQuote?.Bid ?? 0m);
        }

        protected override async Task<Error> SendAsync(CancellationToken cancellationToken = default)
        {
            var tokenConfig = (Fa12Config)Currency;
            var tokenContract = tokenConfig.TokenContractAddress;
            var tokenId = 0;
            var tokenType = "FA12";

            var tokenAddress = await TezosTokensSendViewModel.GetTokenAddressAsync(
                account: AtomexApp.Account,
                address: From,
                tokenContract: tokenContract,
                tokenId: tokenId,
                tokenType: tokenType);

            var currencyName = AtomexApp.Account.Currencies
                .FirstOrDefault(c => c is Fa12Config fa12 && fa12.TokenContractAddress == tokenContract)
                ?.Name ?? "FA12";

            var tokenAccount = AtomexApp.Account.GetTezosTokenAccount<Fa12Account>(
                currency: currencyName,
                tokenContract: tokenContract,
                tokenId: tokenId);

            return await tokenAccount.SendAsync(
                from: tokenAddress.Address,
                to: To,
                amount: Amount,
                fee: Fee,
                useDefaultFee: UseDefaultFee);
        }
    }
}
