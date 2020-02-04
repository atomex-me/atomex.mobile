using System;
using System.Linq;
using Atomex;
using Atomex.Core;
using Atomex.Common.Configuration;
using Atomex.MarketData.Bitfinex;
using Microsoft.Extensions.Configuration;
using Atomex.Wallet;
using Atomex.Subsystems;
using Atomex.Common;
using System.Threading.Tasks;

namespace atomex.ViewModel
{
    public class MainViewModel : BaseViewModel
    {
        public CurrenciesViewModel CurrenciesViewModel { get; set; }
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

            CurrenciesViewModel = new CurrenciesViewModel(AtomexApp);
            SettingsViewModel = new SettingsViewModel(account);
            ConversionViewModel = new ConversionViewModel(AtomexApp);

            AtomexApp.Start();

            //Test(account).FireAndForget();

            //AtomexApp.Terminal.SubscribeToMarketData(new SubscriptionType})
            // AtomexApp.Stop();
        }


        //private async Task Test(Account account)
        //{
        //    await new HdWalletScanner(account).ScanAsync("XTZ");
        //    //await account.UpdateBalanceAsync("XTZ");
        //    //var balance = await account.GetBalanceAsync("XTZ");
        //    var tx = await account.GetTransactionsAsync("XTZ");
        //    Console.WriteLine(tx);
        //}

        public IAtomexApp GetAtomexApp() {
            return AtomexApp;
        }
    }
}
