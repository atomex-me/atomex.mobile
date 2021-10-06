using System.Linq;
using Atomex.Abstract;
using Atomex.Common;
using Atomex.Core;
using static Atomex.ViewModels.Helpers;

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

            return new SwapViewModel
            {
                Id               = swap.Id.ToString(),
                CompactState     = CompactStateBySwap(swap),
                Mode             = ModeBySwap(swap),
                Time             = swap.TimeStamp,

                FromAmount       = fromAmount,
                FromCurrencyCode = soldCurrency.Name,

                ToAmount         = toAmount,
                ToCurrencyCode   = purchasedCurrency.Name,

                DetailingInfo    = GetSwapDetailingInfo(swap).ToList(),

                Price            = swap.Price,
                PriceFormat      = $"F{quoteCurrency.Digits}"
            };
        }

        private static SwapMode ModeBySwap(Swap swap)
        {
            return swap.IsInitiator
                ? SwapMode.Initiator
                : SwapMode.CounterParty;
        }

        public static SwapCompactState CompactStateBySwap(Swap swap)
        {
            if (swap.IsComplete)
                return SwapCompactState.Completed;

            if (swap.IsCanceled)
                return SwapCompactState.Canceled;

            if (swap.IsUnsettled)
                return SwapCompactState.Unsettled;

            if (swap.IsRefunded)
                return SwapCompactState.Refunded;

            return SwapCompactState.InProgress;
        }
    }
}

