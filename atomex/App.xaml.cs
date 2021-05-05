using System;
using System.Globalization;
using System.Linq;
using System.Threading;
using atomex.CustomElements;
using atomex.Resources;
using atomex.Services;
using atomex.Styles;
using Atomex;
using Atomex.Common.Configuration;
using Atomex.Core;
using Atomex.MarketData.Bitfinex;
using Atomex.Subsystems;
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

        const int smallWightResolution = 768;
        const int smallHeightResolution = 1280;

        public CultureInfo CurrentCulture => AppResources.Culture ?? Thread.CurrentThread.CurrentUICulture;

        public App()
        {
            InitializeComponent();
            LoadStyles();

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

            var currenciesProvider = new CurrenciesProvider(currenciesConfiguration);
            var symbolsProvider = new SymbolsProvider(symbolsConfiguration);

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

            Current.RequestedThemeChanged += (s, a) =>
            {
                Device.BeginInvokeOnMainThread(SetAppTheme);
            };
            SetAppTheme();
        }

        public void SetAppTheme()
        {
            if (MainPage is NavigationPage)
            {
                string navBarBackgroundColorName = "NavigationBarBackgroundColor";
                string navBarTextColorName = "NavigationBarTextColor";
                if (Current.RequestedTheme == OSAppTheme.Dark)
                {
                    navBarBackgroundColorName = "NavigationBarBackgroundColorDark";
                    navBarTextColorName = "NavigationBarTextColorDark";
                }
                if (Current.Resources.TryGetValue(navBarBackgroundColorName, out var navBarColor))
                    ((NavigationPage)MainPage).BarBackgroundColor = (Color)navBarColor;
                if (Current.Resources.TryGetValue(navBarTextColorName, out var navBarTextColor))
                    ((NavigationPage)MainPage).BarTextColor = (Color)navBarTextColor;
            }
            else if (MainPage is CustomTabbedPage)
            {
                
                var tabs = ((CustomTabbedPage)MainPage).Children;
             
                string navBarBackgroundColorName = "NavigationBarBackgroundColor";
                string navBarTextColorName = "NavigationBarTextColor";
                string tabBarBackgroundColorName = "TabBarBackgroundColor";

                if (Current.RequestedTheme == OSAppTheme.Dark)
                {
                    navBarBackgroundColorName = "NavigationBarBackgroundColorDark";
                    navBarTextColorName = "NavigationBarTextColorDark";
                    tabBarBackgroundColorName = "TabBarBackgroundColorDark";
                }

                Current.Resources.TryGetValue(navBarBackgroundColorName, out var navBarColor);
                Current.Resources.TryGetValue(navBarTextColorName, out var navBarTextColor);
                Current.Resources.TryGetValue(tabBarBackgroundColorName, out var tabBarBackgroundColor);

                ((CustomTabbedPage)MainPage).BarBackgroundColor = (Color)tabBarBackgroundColor;
                ((CustomTabbedPage)MainPage).BackgroundColor = (Color)navBarColor;

                foreach (NavigationPage tab in tabs)
                {
                    tab.BarBackgroundColor = (Color)navBarColor;
                    tab.BarTextColor = (Color)navBarTextColor;
                }
            }
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
