using System;
using System.Linq;
using Atomex;
using Atomex.Core;
using Atomex.Common.Configuration;
using Atomex.MarketData.Bitfinex;
using Microsoft.Extensions.Configuration;
using Atomex.MarketData;
using Atomex.Wallet;
using Atomex.Subsystems;
using Atomex.Common;
using System.Security;
using System.Threading;

namespace atomex.ViewModel
{
    public class MainViewModel : BaseViewModel
    {
        public WalletsViewModel WalletsViewModel { get; set; }
        public TransactionsViewModel TransactionsViewModel { get; set; }
        public SettingsViewModel SettingsViewModel { get; set; }
        public ConversionViewModel ConversionViewModel { get; set; }

        private IAtomexApp AtomexApp;

        public MainViewModel()
        {
            var coreAssembly = AppDomain.CurrentDomain
                .GetAssemblies()
                .FirstOrDefault(a => a.GetName().Name == "Atomex.Client.Core");

            var currenciesConfiguration = new ConfigurationBuilder()
                .AddEmbeddedJsonFile(coreAssembly, "currencies.json")
                .Build();

            var symbolsConfiguration = new ConfigurationBuilder()
                .AddEmbeddedJsonFile(coreAssembly, "symbols.json")
                .Build();

            var configuration = new ConfigurationBuilder()
                .AddJsonFile("configuration.json")
                .Build();

            var currenciesProvider = new CurrenciesProvider(currenciesConfiguration);
            var symbolsProvider = new SymbolsProvider(symbolsConfiguration, currenciesProvider);

            //var account = new Account(new HdWallet(Network.TestNet), "", currenciesProvider, symbolsProvider);
            //var account = new Account(new HdWallet("atomex.wallet", ))
            var account = Account.LoadFromFile("atomex.wallet", "12345678".ToSecureString(), currenciesProvider, symbolsProvider);

            AtomexApp = new AtomexApp()
                .UseCurrenciesProvider(currenciesProvider)
                .UseSymbolsProvider(symbolsProvider)
                .UseQuotesProvider(new BitfinexQuotesProvider(
                    currencies: currenciesProvider.GetCurrencies(Network.TestNet),
                    baseCurrency: BitfinexQuotesProvider.Usd))
                .UseTerminal(new Terminal(configuration, account));

            //WalletsViewModel = new WalletsViewModel(AtomexApp);
            WalletsViewModel = new WalletsViewModel();
            TransactionsViewModel = new TransactionsViewModel();
            SettingsViewModel = new SettingsViewModel(account);
            ConversionViewModel = new ConversionViewModel();

            AtomexApp.Start();

            //Thread.Sleep(20000);

            //Console.WriteLine(account.GetBalanceAsync(account.Currencies[0]).WaitForResult());
            //var balance = account.GetBalanceAsync(account.Currencies[1]).WaitForResult();
            //Console.WriteLine(balance);
            //Console.WriteLine(account);

            //var transactions = account.GetTransactionsAsync(account.Currencies[1]).WaitForResult();
            //Console.WriteLine(transactions);


            //AtomexApp.Terminal.SubscribeToMarketData(new SubscriptionType})
            // AtomexApp.Stop();
        }

        public IAtomexApp GetAtomexApp() {
            return AtomexApp;
        }
    }
}
