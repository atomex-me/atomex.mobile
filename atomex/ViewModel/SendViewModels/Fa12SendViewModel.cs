using System;
using System.Threading.Tasks;
using Atomex;
using Atomex.Blockchain.Abstract;
using Atomex.MarketData.Abstract;

namespace atomex.ViewModel.SendViewModels
{
    public class Fa12SendViewModel : SendViewModel
    {

        public Fa12SendViewModel(
            CurrencyViewModel currencyViewModel)
            : base(currencyViewModel)
        {
        }

        public override async Task UpdateAmount(decimal amount)
        {
            var availableAmount = CurrencyViewModel.AvailableAmount;
            _amount = amount;


            if (UseDefaultFee)
            {
                var (maxAmount, maxFeeAmount, _) = await App.Account
                     .EstimateMaxAmountToSendAsync(Currency.Name, To, BlockchainTransactionType.Output, 0, 0, false);

                if (_amount > maxAmount)
                {
                    if (_amount <= availableAmount)
                        Console.WriteLine("!!");
                    //!! Warning = string.Format(CultureInfo.InvariantCulture, Resources.CvInsufficientChainFunds, Currency.FeeCurrencyName);
                    else
                        Console.WriteLine("!!");
                    //!! Warning = Resources.CvInsufficientFunds;
                }

                var estimatedFeeAmount = _amount != 0
                        ? await App.Account.EstimateFeeAsync(Currency.Name, To, _amount, BlockchainTransactionType.Output)
                        : 0;

                OnPropertyChanged(nameof(AmountString));

                _fee = Currency.GetFeeFromFeeAmount(estimatedFeeAmount ?? Currency.GetDefaultFee(), Currency.GetDefaultFeePrice());
                OnPropertyChanged(nameof(FeeString));

            }
            else
            {
                var (maxAmount, _, _) = await App.Account
                       .EstimateMaxAmountToSendAsync(Currency.Name, To, BlockchainTransactionType.Output, 0, 0, false);

                if (_amount > maxAmount)
                {
                    if (_amount <= availableAmount)
                        Console.WriteLine("!!");
                    //Warning = string.Format(CultureInfo.InvariantCulture, Resources.CvInsufficientChainFunds, Currency.FeeCurrencyName);
                    else
                        Console.WriteLine("!!");
                    //Warning = Resources.CvInsufficientFunds;

                    OnPropertyChanged(nameof(AmountString));

                }
                Fee = _fee;
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

                var feeAmount = _fee;

                if (feeAmount < estimatedFeeAmount.Value)
                    _fee = estimatedFeeAmount.Value;

                if (_amount == 0)
                    _fee = 0;

                OnPropertyChanged(nameof(AmountString));

                OnPropertyChanged(nameof(FeeString));
            }
            OnQuotesUpdatedEventHandler(App.QuotesProvider, EventArgs.Empty);
        }

        protected override void OnQuotesUpdatedEventHandler(object sender, EventArgs args)
        {
            if (!(sender is ICurrencyQuotesProvider quotesProvider))
                return;

            var quote = quotesProvider.GetQuote(CurrencyCode, BaseCurrencyCode);
            var xtzQuote = quotesProvider.GetQuote("XTZ", BaseCurrencyCode);

            AmountInBase = Amount * (quote?.Bid ?? 0m);
            FeeInBase = Fee * (xtzQuote?.Bid ?? 0m);
        }
    }
}

