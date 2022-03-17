using atomex.ViewModel.CurrencyViewModels;
using Atomex.Core;

namespace atomex
{
    public interface INavigationService
    {
        void ConvertCurrency(CurrencyConfig currencyConfig);
        void ShowCurrency(CurrencyViewModel currencyViewModel);
        void ShowTezosTokens(TezosTokensViewModel tezosTokensViewModel);
    }
}
