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

namespace atomex.ViewModel
{
    public class MainViewModel : BaseViewModel
    {
        public SettingsViewModel SettingsViewModel { get; set; }
        public ConversionViewModel ConversionViewModel { get; set; }
        public PortfolioViewModel PortfolioViewModel { get; set; }
        public BuyViewModel BuyViewModel { get; set; }

        public IAtomexApp AtomexApp { get; private set; }

        public EventHandler Locked;

        public MainViewModel(
            IAtomexApp app,
            IAccount account)
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
                symbolsProvider: AtomexApp.SymbolsProvider);

            AtomexApp.UseAtomexClient(atomexClient, restart: true);

            PortfolioViewModel = new PortfolioViewModel(AtomexApp);
            ConversionViewModel = new ConversionViewModel(AtomexApp);
            BuyViewModel = new BuyViewModel(AtomexApp);
            SettingsViewModel = new SettingsViewModel(AtomexApp, this);

            _ = TokenDeviceService.SendTokenToServerAsync(App.DeviceToken, App.FileSystem, AtomexApp);
        }

        public void InitCurrenciesScan(string[] currenciesArr = null)
        {
            PortfolioViewModel?.InitCurrenciesScan(currenciesArr);
        }

        public void SignOut()
        {
            AtomexApp.UseAtomexClient(null);
        }

        private void SubscribeToServices()
        {
            AtomexApp.AtomexClientChanged += OnAtomexClientChangedEventHandler;
        }

        private void OnAtomexClientChangedEventHandler(object sender, AtomexClientChangedEventArgs args)
        {
            var atomexClient = args.AtomexClient;

            if (atomexClient?.Account == null)
                return;

            atomexClient.ServiceConnected += OnTerminalServiceStateChangedEventHandler;
            atomexClient.ServiceDisconnected += OnTerminalServiceStateChangedEventHandler;
        }

        private void OnTerminalServiceStateChangedEventHandler(object sender, AtomexClientServiceEventArgs args)
        {
            if (!(sender is IAtomexClient terminal))
                return;

            // subscribe to symbols updates
            if (args.Service == AtomexClientService.MarketData && terminal.IsServiceConnected(AtomexClientService.MarketData))
            {
                terminal.SubscribeToMarketData(SubscriptionType.TopOfBook);
                terminal.SubscribeToMarketData(SubscriptionType.DepthTwenty);
            }
        }
    }
}
