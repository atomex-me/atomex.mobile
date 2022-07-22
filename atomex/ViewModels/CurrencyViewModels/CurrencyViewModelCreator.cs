using System;
using System.Collections.Concurrent;

using Atomex.Core;

namespace atomex.ViewModels.CurrencyViewModels
{
    public enum Currencies
    {
        BTC,
        LTC,
        USDT,
        TBTC,
        WBTC,
        ETH,
        TZBTC,
        KUSD,
        USDT_XTZ,
        XTZ,
    }

    public class CurrencyViewModelCreator
    {
        private readonly ConcurrentDictionary<Currencies, CurrencyViewModel> Instances = new();

        public CurrencyViewModel CreateOrGet(
            CurrencyConfig currencyConfig,
            INavigationService navigationService,
            bool subscribeToUpdates)
        {
            var parsed = Enum.TryParse(currencyConfig.Name, out Currencies currency);
            if (!parsed) throw NotSupported(currencyConfig.Name);

            if (subscribeToUpdates && Instances.TryGetValue(currency, out var cachedCurrencyViewModel))
                return cachedCurrencyViewModel;

            var currencyViewModel = currency switch
            {
                Currencies.BTC => new CurrencyViewModel(App.AtomexApp, currencyConfig, navigationService),
                Currencies.LTC => new CurrencyViewModel(App.AtomexApp, currencyConfig, navigationService),
                Currencies.USDT => new CurrencyViewModel(App.AtomexApp, currencyConfig, navigationService),
                Currencies.TBTC => new CurrencyViewModel(App.AtomexApp, currencyConfig, navigationService),
                Currencies.WBTC => new CurrencyViewModel(App.AtomexApp, currencyConfig, navigationService),
                Currencies.ETH => new CurrencyViewModel(App.AtomexApp, currencyConfig, navigationService),
                Currencies.TZBTC => new Fa12CurrencyViewModel(App.AtomexApp, currencyConfig, navigationService),
                Currencies.KUSD => new Fa12CurrencyViewModel(App.AtomexApp, currencyConfig, navigationService),
                Currencies.USDT_XTZ => new Fa2CurrencyViewModel(App.AtomexApp, currencyConfig, navigationService),
                Currencies.XTZ => new TezosCurrencyViewModel(App.AtomexApp, currencyConfig, navigationService),
                _ => throw NotSupported(currencyConfig.Name)
            };

            if (!subscribeToUpdates) return currencyViewModel;

            currencyViewModel.SubscribeToServices();
            currencyViewModel.SubscribeToRatesProvider(App.AtomexApp.QuotesProvider);
            Instances.TryAdd(currency, currencyViewModel);

            return currencyViewModel;
        }

        public void Reset()
        {
            foreach (var currencyViewModel in Instances.Values)
            {
                currencyViewModel.Dispose();
            }

            Instances.Clear();
        }

        private NotSupportedException NotSupported(string currencyName)
        {
            return new NotSupportedException(
                $"Can't create currency view model for {currencyName}. This currency is not supported.");
        }
    }
}

