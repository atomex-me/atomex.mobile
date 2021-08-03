using System;
using Atomex;
using Atomex.Core;
using Atomex.EthereumTokens;
using Atomex.TezosTokens;

namespace atomex.ViewModel.CurrencyViewModels
{
    public static class CurrencyViewModelCreator
    {
        public static CurrencyViewModel CreateViewModel(
            IAtomexApp app,
            CurrencyConfig currency)
        {
            return currency switch
            {
                BitcoinBasedConfig _ or
                Erc20Config _ or
                EthereumConfig _ => new CurrencyViewModel(app, currency),

                Fa12Config _ => new Fa12CurrencyViewModel(app, currency),

                TezosConfig _ => new TezosCurrencyViewModel(app, currency),

                _ => throw new NotSupportedException($"Can't create currency view model for {currency.Name}. This currency is not supported."),
            };
        }
    }
}

