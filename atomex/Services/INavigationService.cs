using Atomex.Core;
using static atomex.Models.SnackbarMessage;

namespace atomex
{
    public interface INavigationService
    {
        void ConvertCurrency(CurrencyConfig currencyConfig);
        void BuyCurrency(CurrencyConfig currencyConfig);
        void DisplaySnackBar(MessageType messageType, string text, string btnTxt = "OK", int duration = 3000);
    }
}
