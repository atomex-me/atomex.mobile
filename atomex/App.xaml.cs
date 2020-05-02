using System;
using System.Linq;
using Atomex;
using Atomex.Common.Configuration;
using Atomex.Core;
using Atomex.MarketData.Bitfinex;
using Microsoft.Extensions.Configuration;
using Xamarin.Forms;

namespace atomex
{
    public partial class App : Application
    {

        private IAtomexApp AtomexApp;

        public App()
        {
            InitializeComponent();

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
            var symbolsProvider = new SymbolsProvider(symbolsConfiguration);

            //var walletHex = "0183667a2fe8ae1f6eb5dafca5cfe87b817a33bb59a9b8d1d59300583152fadc9643497a05b7f5d0cfdfa5123c8864dc405bed2e50fc6788ba2ac38796883e03e55fb01dc88ea5bb3d5853740306714151e4f07d1e0f057f1bad901db5f00ce4993d01c02bd8499f8172f027a8f46d04ad9f8c9be7416ccb6cc91353278e20385044ac69e514a1a5b1dd3b87cc0e281ad0186bcc9670e8e4623fc8d3cc697173e5e8c3753998447a4e3f51c7e8c3072c831191db4511c4d3ae3687988a202b8e75e919572c3d390dc247641f6d86be34cd83a0aa9a9d95a37765fde834652cc173f7c58c88082d9837452752a0443c6701d17f726be8619a0acd3a41df34ed23602d2ae0898ca9247f8387b158930d18b5b0cedb3799028aa0793efe4e80803d8b48f873ae7f884a07023a671add7b1b18";

            //using (var sandboxWalletStream = new FileStream(pathToWallet, FileMode.Create))
            //{
            //    var walletBytes = Hex.FromString(walletHex);

            //    sandboxWalletStream.Write(walletBytes, 0, walletBytes.Length);
            //}

            //var documents = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            //var library = Path.Combine(documents, "..", "Library");
            //var pathToWallet = Path.Combine(library, "atomex.wallet");

            //var account = Account.LoadFromFile(pathToWallet, "12345678".ToSecureString(), currenciesProvider, symbolsProvider);

            AtomexApp = new AtomexApp()
                .UseCurrenciesProvider(currenciesProvider)
                .UseSymbolsProvider(symbolsProvider)
                .UseQuotesProvider(new BitfinexQuotesProvider(
                    currencies: currenciesProvider.GetCurrencies(Network.TestNet),
                    baseCurrency: BitfinexQuotesProvider.Usd));
            //.UseTerminal(new Terminal(configuration, account));

            AtomexApp.Start();

            StartViewModel startViewModel = new StartViewModel(AtomexApp);
            MainPage = new NavigationPage(new StartPage(startViewModel));
            ((NavigationPage)MainPage).BarBackgroundColor = Color.FromHex("#2B5286");
            ((NavigationPage)MainPage).BarTextColor = Color.White;
            //var test = Color.FromHex(Application.Current.Resources["AdditionalBackgroundColor1"].ToString());
        }

        protected override void OnStart()
        {
            // Handle when your app starts
        }

        protected override void OnSleep()
        {
            // Handle when your app sleeps
        }

        protected override void OnResume()
        {
            // Handle when your app resumes
        }
    }
}
