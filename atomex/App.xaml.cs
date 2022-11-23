using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using atomex.Resources;
using atomex.Services;
using atomex.Styles;
using Atomex;
using Atomex.Common.Configuration;
using Atomex.Core;
using Atomex.MarketData;
using Atomex.MarketData.Abstract;
using Atomex.MarketData.Bitfinex;
using Atomex.MarketData.TezTools;
using Atomex.Services;
using atomex.ViewModels;
using atomex.Views;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Serilog;
using Xamarin.Essentials;
using Xamarin.Forms;

namespace atomex
{
    public partial class App : Application
    {
        public static string DeviceToken;
        public static string FileSystem;

        public static IAtomexApp AtomexApp;

        public static double ScreenWidth { get; } =
            DeviceDisplay.MainDisplayInfo.Width / DeviceDisplay.MainDisplayInfo.Density;

        public static double ScreenHeight { get; } =
            DeviceDisplay.MainDisplayInfo.Height / DeviceDisplay.MainDisplayInfo.Density;

        public static bool IsSmallScreen { get; } = ScreenWidth <= 360;

        public CultureInfo CurrentCulture => AppResources.Culture ?? Thread.CurrentThread.CurrentUICulture;

        private static Assembly CoreAssembly { get; } = AppDomain.CurrentDomain
            .GetAssemblies()
            .FirstOrDefault(a => a.GetName().Name == "Atomex.Client.Core");

        private static string CurrenciesConfigurationJson
        {
            get
            {
                var coreAssembly = CoreAssembly;
                var resourceName = "currencies.json";
                var resourceNames = coreAssembly.GetManifestResourceNames();
                var fullFileName = resourceNames.FirstOrDefault(n => n.EndsWith(resourceName));
                var stream = coreAssembly.GetManifestResourceStream(fullFileName!);

                using StreamReader reader = new(stream!);
                return reader.ReadToEnd();
            }
        }

        private static IConfiguration SymbolsConfiguration { get; } = new ConfigurationBuilder()
            .AddEmbeddedJsonFile(CoreAssembly, "symbols.json")
            .Build();

        public App()
        {
            InitializeComponent();
            LoadStyles();
            var loggerFactory = InitLogger();

            DependencyService.Get<INotificationManager>().Initialize();

            var currenciesProvider = new CurrenciesProvider(CurrenciesConfigurationJson);
            var symbolsProvider = new SymbolsProvider(SymbolsConfiguration);

            var bitfinexQuotesProvider = new BitfinexQuotesProvider(
                currencies: currenciesProvider.GetCurrencies(Network.MainNet).Select(c => c.Name),
                baseCurrency: QuotesProvider.Usd,
                log: loggerFactory.CreateLogger<BitfinexQuotesProvider>());
            var tezToolsQuotesProvider = new TezToolsQuotesProvider(
                loggerFactory.CreateLogger<TezToolsQuotesProvider>());

            var quotesProvider = new MultiSourceQuotesProvider(
                log: loggerFactory.CreateLogger<MultiSourceQuotesProvider>(),
                bitfinexQuotesProvider,
                tezToolsQuotesProvider);

            AtomexApp = new AtomexApp()
                .UseCurrenciesProvider(currenciesProvider)
                .UseSymbolsProvider(symbolsProvider)
                .UseCurrenciesUpdater(new CurrenciesUpdater(currenciesProvider))
                .UseSymbolsUpdater(new SymbolsUpdater(symbolsProvider))
                .UseQuotesProvider(quotesProvider);

            AtomexApp.Start();

            StartViewModel startViewModel = new StartViewModel(AtomexApp);
            var mainPage = new StartPage(startViewModel);
            MainPage = new NavigationPage(mainPage);
            startViewModel.SetNavigationService(mainPage);
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

        public void OnDeepLinkReceived(string value)
        {
            var mainPage = MainPage;
            if (MainPage is NavigationPage)
            {
                var navigation = mainPage as NavigationPage;
                var root = navigation?.RootPage as INavigationService;
                root?.ConnectDappByDeepLink(value);
                
                return;
            }

            var tabbedPage = mainPage as INavigationService;
            tabbedPage?.ConnectDappByDeepLink(value);
        }

        private void LoadStyles()
        {
            if (IsSmallScreen)
                ResDictionary.MergedDictionaries.Add(SmallDevicesStyle.SharedInstance);
            else
                ResDictionary.MergedDictionaries.Add(GeneralDevicesStyle.SharedInstance);
        }

        private ILoggerFactory InitLogger()
        {
            return new LoggerFactory()
                .AddSerilog();
        }
    }
}