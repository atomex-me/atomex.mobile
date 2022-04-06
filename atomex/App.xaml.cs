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
using Atomex.MarketData.Bitfinex;
using Atomex.Services;
using Microsoft.Extensions.Configuration;
using Xamarin.Essentials;
using Xamarin.Forms;

namespace atomex
{
    public partial class App : Application
    {
        public static string DeviceToken;
        public static string FileSystem;

        private IAtomexApp AtomexApp;

        public static double ScreenWidth { get; } = DeviceDisplay.MainDisplayInfo.Width / DeviceDisplay.MainDisplayInfo.Density;
        public static double ScreenHeight { get; } = DeviceDisplay.MainDisplayInfo.Height / DeviceDisplay.MainDisplayInfo.Density;

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

            DependencyService.Get<INotificationManager>().Initialize();

            var currenciesProvider = new CurrenciesProvider(CurrenciesConfigurationJson);
            var symbolsProvider = new SymbolsProvider(SymbolsConfiguration);

            AtomexApp = new AtomexApp()
                .UseCurrenciesProvider(currenciesProvider)
                .UseSymbolsProvider(symbolsProvider)
                .UseCurrenciesUpdater(new CurrenciesUpdater(currenciesProvider))
                .UseSymbolsUpdater(new SymbolsUpdater(symbolsProvider))
                .UseQuotesProvider(new BitfinexQuotesProvider(
                    currencies: currenciesProvider.GetCurrencies(Network.MainNet),
                    baseCurrency: BitfinexQuotesProvider.Usd));

            AtomexApp.Start();

            StartViewModel startViewModel = new StartViewModel(AtomexApp);
            MainPage = new NavigationPage(new StartPage(startViewModel));
            startViewModel.Navigation = MainPage.Navigation;
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

        private void LoadStyles()
        {
            if (IsSmallScreen)
                ResDictionary.MergedDictionaries.Add(SmallDevicesStyle.SharedInstance);
            else
                ResDictionary.MergedDictionaries.Add(GeneralDevicesStyle.SharedInstance);
        }
    }
}
