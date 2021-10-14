using Atomex.Abstract;
using Atomex.Common;
using Atomex.Core;

namespace atomex
{
    public static class SwapViewModelFactory
    {
        public static SwapViewModel CreateSwapViewModel(Swap swap, ICurrencies currencies)
        {
            var soldCurrency = currencies.GetByName(swap.SoldCurrency);
            var purchasedCurrency = currencies.GetByName(swap.PurchasedCurrency);

            var fromAmount = AmountHelper.QtyToAmount(swap.Side, swap.Qty, swap.Price, soldCurrency.DigitsMultiplier);
            var toAmount = AmountHelper.QtyToAmount(swap.Side.Opposite(), swap.Qty, swap.Price, purchasedCurrency.DigitsMultiplier);

            var quoteCurrency = swap.Symbol.QuoteCurrency() == swap.SoldCurrency
                ? soldCurrency
                : purchasedCurrency;

            var swapViewModel = new SwapViewModel
            {
                Id               = swap.Id.ToString(),
                Mode             = ModeBySwap(swap),
                Time             = swap.TimeStamp,

                FromAmount       = fromAmount,
                FromCurrencyCode = soldCurrency.Name,

                ToAmount         = toAmount,
                ToCurrencyCode   = purchasedCurrency.Name,

                Price            = swap.Price,
                PriceFormat      = $"F{quoteCurrency.Digits}"
            };

            swapViewModel.UpdateSwap(swap);

            return swapViewModel;
        }

        private static SwapMode ModeBySwap(Swap swap)
        {
            return swap.IsInitiator
                ? SwapMode.Initiator
                : SwapMode.CounterParty;
        }
    }
}

