using System;
using System.IO;
using System.Linq;
using atomex.Services;
using atomex.Styles;
using Atomex;
using Atomex.Common.Configuration;
using Atomex.Core;
using Atomex.MarketData.Bitfinex;
using Microsoft.Extensions.Configuration;
using Xamarin.Essentials;
using Xamarin.Forms;

namespace atomex
{
    public partial class App : Application
    {

        private IAtomexApp AtomexApp;

        const int smallWightResolution = 768;
        const int smallHeightResolution = 1280;

        public App()
        {
            InitializeComponent();
            LoadStyles();

            // use the dependency service to get a platform-specific implementation and initialize it
            DependencyService.Get<INotificationManager>().Initialize();

            var coreAssembly = AppDomain.CurrentDomain
                .GetAssemblies()
                .FirstOrDefault(a => a.GetName().Name == "Atomex.Client.Core");

            var currenciesConfiguration = new ConfigurationBuilder()
                .AddEmbeddedJsonFile(coreAssembly, "currencies.json")
                .Build();

            var symbolsConfiguration = new ConfigurationBuilder()
                .AddEmbeddedJsonFile(coreAssembly, "symbols.json")
                .Build();

            //var configuration = new ConfigurationBuilder()
            //    //.SetBasePath(Environment.CurrentDirectory)
            //    .SetBasePath(Directory.GetCurrentDirectory())
            //    .AddJsonFile("configuration.json")
            //    .Build();


            //UseContentRoot

            // iOS impl
            //var configuration = new ConfigurationBuilder()
            //    .AddJsonFile("configuration.json")
            //    .Build();

            //var configuration = new ConfigurationBuilder()
            //    .AddJsonFile("configuration.json")
            //    .Build();

            var currenciesProvider = new CurrenciesProvider(currenciesConfiguration);
            var symbolsProvider = new SymbolsProvider(symbolsConfiguration);

            AtomexApp = new AtomexApp()
                .UseCurrenciesProvider(currenciesProvider)
                .UseSymbolsProvider(symbolsProvider)
                .UseQuotesProvider(new BitfinexQuotesProvider(
                    currencies: currenciesProvider.GetCurrencies(Network.MainNet),
                    baseCurrency: BitfinexQuotesProvider.Usd));

            AtomexApp.Start();

            StartViewModel startViewModel = new StartViewModel(AtomexApp);
            MainPage = new NavigationPage(new StartPage(startViewModel));

            if (Resources.TryGetValue("NavigationBarBackgroundColor", out var navBarColor))
                ((NavigationPage)MainPage).BarBackgroundColor = (Color)navBarColor;
            if (Resources.TryGetValue("NavigationBarTextColor", out var navBarTextColor))
                ((NavigationPage)MainPage).BarTextColor = (Color)navBarTextColor;

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
            if (IsASmallDevice())
                ResDictionary.MergedDictionaries.Add(SmallDevicesStyle.SharedInstance);
            else
                ResDictionary.MergedDictionaries.Add(GeneralDevicesStyle.SharedInstance);
        }

        public static bool IsASmallDevice()
        {
            // Get Metrics
            var mainDisplayInfo = DeviceDisplay.MainDisplayInfo;

            // Width (in pixels)
            var width = mainDisplayInfo.Width;

            // Height (in pixels)
            var height = mainDisplayInfo.Height;
            return (width <= smallWightResolution && height <= smallHeightResolution);
        }
    }
}
