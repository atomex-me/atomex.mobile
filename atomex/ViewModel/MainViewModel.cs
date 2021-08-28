using System;
using Atomex;
using Microsoft.Extensions.Configuration;
using Atomex.MarketData;
using Atomex.Wallet.Abstract;
using Atomex.Common.Configuration;
using System.Linq;
using atomex.Services;
using Atomex.Services;
using Atomex.Services.Abstract;
using atomex.ViewModel.CurrencyViewModels;

namespace atomex.ViewModel
{
    public class MainViewModel : BaseViewModel
    {
        public CurrenciesViewModel CurrenciesViewModel { get; set; }
        public SettingsViewModel SettingsViewModel { get; set; }
        public ConversionViewModel ConversionViewModel { get; set; }
        public PortfolioViewModel PortfolioViewModel { get; set; }
        public BuyViewModel BuyViewModel { get; set; }

        public IAtomexApp AtomexApp { get; private set; }

        public EventHandler Locked;

        public MainViewModel(IAtomexApp app, IAccount account, string walletName, string appTheme = "light", bool restore = false)
        {
            var assembly = AppDomain.CurrentDomain
                .GetAssemblies()
                .First(a => a.GetName().Name == "atomex");

            var configuration = new ConfigurationBuilder()
                .AddEmbeddedJsonFile(assembly, "configuration.json")
                .Build();

            AtomexApp = app ?? throw new ArgumentNullException(nameof(AtomexApp));

            SubscribeToServices();

            var atomexClient = new WebSocketAtomexClient(
                configuration: configuration,
                account: account,
                symbolsProvider: AtomexApp.SymbolsProvider,
                quotesProvider: AtomexApp.QuotesProvider);

            AtomexApp.UseAtomexClient(atomexClient, restart: true);

            CurrenciesViewModel = new CurrenciesViewModel(AtomexApp, restore);
            SettingsViewModel = new SettingsViewModel(AtomexApp, this, walletName);
            ConversionViewModel = new ConversionViewModel(AtomexApp);
            PortfolioViewModel = new PortfolioViewModel(CurrenciesViewModel, appTheme);
            BuyViewModel = new BuyViewModel(AtomexApp);

            _ = TokenDeviceService.SendTokenToServerAsync(App.DeviceToken, App.FileSystem, AtomexApp);
        }

        public void SignOut()
        {
            AtomexApp.UseAtomexClient(null);
        }

        private void SubscribeToServices()
        {
            AtomexApp.AtomexClientChanged += OnTerminalChangedEventHandler;
        }

        private void OnTerminalChangedEventHandler(object sender, AtomexClientChangedEventArgs args)
        {
            var terminal = args.AtomexClient;

            if (terminal?.Account == null)
                return;

            terminal.ServiceConnected += OnTerminalServiceStateChangedEventHandler;
            terminal.ServiceDisconnected += OnTerminalServiceStateChangedEventHandler;
        }

        private void OnTerminalServiceStateChangedEventHandler(object sender, TerminalServiceEventArgs args)
        {
            if (!(sender is IAtomexClient terminal))
                return;

            // subscribe to symbols updates
            if (args.Service == TerminalService.MarketData && terminal.IsServiceConnected(TerminalService.MarketData))
            {
                terminal.SubscribeToMarketData(SubscriptionType.TopOfBook);
                terminal.SubscribeToMarketData(SubscriptionType.DepthTwenty);
            }
        }
    }
}
