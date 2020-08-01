using atomex.ViewModel;

namespace atomex
{
    public interface INavigationService
    {
        void ConvertCurrency(string currencyCode);
        void ShowCurrency(CurrencyViewModel currencyViewModel);
    }
}
