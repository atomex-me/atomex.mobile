using Atomex.Core;

namespace atomex
{
    public interface INavigationService
    {
        void ConvertCurrency(CurrencyConfig currencyConfig);
        void BuyCurrency(CurrencyConfig currencyConfig);
    }
}
