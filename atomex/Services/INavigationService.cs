using System.Threading.Tasks;
using atomex.ViewModel;

namespace atomex
{
    public interface INavigationService
    {
        Task ConvertCurrency(string currencyCode);
        Task ShowCurrency(CurrencyViewModel currencyViewModel);
    }
}
