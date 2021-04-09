using System;
using Atomex;
using Microsoft.Extensions.Configuration;
using Atomex.Subsystems;
using Atomex.Subsystems.Abstract;
using Atomex.MarketData;
using Atomex.Wallet.Abstract;
using Atomex.Common.Configuration;
using System.Linq;
using atomex.Services;

namespace atomex.ViewModel
{
    public class MainViewModel : BaseViewModel
    {
        public CurrenciesViewModel CurrenciesViewModel { get; set; }
        public SettingsViewModel SettingsViewModel { get; set; }
        public ConversionViewModel ConversionViewModel { get; set; }

        public IAtomexApp AtomexApp { get; private set; }

        public EventHandler Locked;

        public MainViewModel(IAtomexApp app, IAccount account, string walletName, bool restore = false)
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

            AtomexApp.UseTerminal(atomexClient, restart: true);

            CurrenciesViewModel = new CurrenciesViewModel(AtomexApp, restore);
            SettingsViewModel = new SettingsViewModel(AtomexApp, this, walletName);
            ConversionViewModel = new ConversionViewModel(AtomexApp);

            _ = TokenDeviceService.SendTokenToServerAsync(App.DeviceToken, App.FileSystem, AtomexApp);
        }

        public void SignOut()
        {
            AtomexApp.UseTerminal(null);
        }

        private void SubscribeToServices()
        {
            AtomexApp.TerminalChanged += OnTerminalChangedEventHandler;
        }

        private void OnTerminalChangedEventHandler(object sender, TerminalChangedEventArgs args)
        {
            var terminal = args.Terminal;

            if (terminal?.Account == null)
                return;

            terminal.ServiceConnected += OnTerminalServiceStateChangedEventHandler;
            terminal.ServiceDisconnected += OnTerminalServiceStateChangedEventHandler;

            //var account = terminal.Account;
            //account.Locked += OnAccountLockChangedEventHandler;
            //account.Unlocked += OnAccountLockChangedEventHandler;
            //IsLocked = account.IsLocked;
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
