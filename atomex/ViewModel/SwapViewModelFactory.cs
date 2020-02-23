using Atomex.Common;
using Atomex.Core;

namespace atomex
{
    public static class SwapViewModelFactory
    {
        public static SwapViewModel CreateSwapViewModel(Swap swap)
        {
            var fromAmount = AmountHelper.QtyToAmount(swap.Side, swap.Qty, swap.Price);
            var toAmount = AmountHelper.QtyToAmount(swap.Side.Opposite(), swap.Qty, swap.Price);

            return new SwapViewModel
            {
                Id = swap.Id.ToString(),
                CompactState = CompactStateBySwap(swap),
                Mode = ModeBySwap(swap),
                Time = swap.TimeStamp,

                FromAmount = fromAmount,
                FromCurrencyCode = swap.SoldCurrency.Name,

                ToAmount = toAmount,
                ToCurrencyCode = swap.PurchasedCurrency.Name,

                Price = swap.Price,
                PriceFormat = $"F{swap.Symbol.Quote.Digits}"
            };
        }

        private static SwapMode ModeBySwap(Swap swap)
        {
            return swap.IsInitiator
                ? SwapMode.Initiator
                : SwapMode.CounterParty;
        }

        private static SwapCompactState CompactStateBySwap(Swap swap)
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

