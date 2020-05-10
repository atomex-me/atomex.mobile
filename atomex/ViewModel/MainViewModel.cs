using System;
using Atomex;
using Microsoft.Extensions.Configuration;
using Atomex.Subsystems;
using Atomex.Subsystems.Abstract;
using Atomex.MarketData;
using Atomex.Wallet.Abstract;

namespace atomex.ViewModel
{
    public class MainViewModel : BaseViewModel
    {
        public CurrenciesViewModel CurrenciesViewModel { get; set; }
        public SettingsViewModel SettingsViewModel { get; set; }
        public ConversionViewModel ConversionViewModel { get; set; }
        public DelegateViewModel DelegateViewModel { get; set; }

        public IAtomexApp AtomexApp { get; private set; }

        public EventHandler Locked;

        public MainViewModel(IAtomexApp app, IAccount account)
        {
            var configuration = new ConfigurationBuilder()
                .AddJsonFile("configuration.json")
                .Build();

            AtomexApp = app ?? throw new ArgumentNullException(nameof(AtomexApp));

            AtomexApp.Start();

            SubscribeToServices();

            AtomexApp.UseTerminal(new WebSocketAtomexClient(configuration, account), restart: true);

            CurrenciesViewModel = new CurrenciesViewModel(AtomexApp);
            SettingsViewModel = new SettingsViewModel(AtomexApp);
            ConversionViewModel = new ConversionViewModel(AtomexApp);
            DelegateViewModel = new DelegateViewModel(AtomexApp);

            //ActivityObserver().FireAndForget();
        }

        //private async Task ActivityObserver()
        //{
        //    while (true)
        //    {
        //        await Task.Delay(TimeSpan.FromMinutes(SettingsViewModel.PeriodOfInactivityInMin));
        //        Locked?.Invoke(this, EventArgs.Empty);
        //        return;
        //    }
        //}

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
