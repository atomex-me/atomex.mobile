using System;
using atomex.ViewModel;

namespace atomex
{
    public interface INavigationService
    {
        void ShowConversionPage(CurrencyViewModel wallet);
    }
}
