using System;
using atomex.ViewModel;
using Atomex;

namespace atomex
{
    public interface INavigationService
    {
        void ShowConversionPage(IAtomexApp app, CurrencyViewModel currency);
    }
}
