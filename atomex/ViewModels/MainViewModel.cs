using atomex.Services;
using Atomex;
using Atomex.Client.Abstract;
using Atomex.Client.Common;
using Atomex.Client.V1.Entities;
using Atomex.Common;
using Atomex.Common.Configuration;
using Atomex.Services;
using Atomex.Wallet.Abstract;
using Microsoft.Extensions.Configuration;
using System;
using System.Linq;
using atomex.ViewModels.ConversionViewModels;
using atomex.ViewModels.CurrencyViewModels;
using Xamarin.Forms;

namespace atomex.ViewModels
{
    public class MainViewModel : BaseViewModel
    {
        public IAtomexApp AtomexApp { get; }
        public SettingsViewModel SettingsViewModel { get; }
        public ConversionViewModel ConversionViewModel { get; }
        public PortfolioViewModel PortfolioViewModel { get; }
        public BuyViewModel BuyViewModel { get; }

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

            AtomexApp = app ?? throw new ArgumentNullException(nameof(app));

            SubscribeToServices();

            var clientType = Device.RuntimePlatform switch
            {
                Device.iOS => ClientType.iOS,
                Device.Android => ClientType.Android,
                _ => ClientType.Unknown,
            };

            var atomexClient = new WebSocketAtomexClientLegacy(
                exchangeUrl: configuration[$"Services:{account?.Network}:Exchange:Url"],
                marketDataUrl: configuration[$"Services:{account?.Network}:MarketData:Url"],
                clientType: clientType,
                authMessageSigner: account.DefaultAuthMessageSigner());

            AtomexApp.ChangeAtomexClient(atomexClient, account, restart: true);

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
            AtomexApp.ChangeAtomexClient(atomexClient: null, account: null);

            ConversionViewModel?.Reset();
            CurrencyViewModelCreator.Reset();
            TezosTokenViewModelCreator.Reset();

            AtomexApp.AtomexClientChanged -= OnAtomexClientChangedEventHandler;

        }

        private void SubscribeToServices()
        {
            AtomexApp.AtomexClientChanged += OnAtomexClientChangedEventHandler;
        }

        private void OnAtomexClientChangedEventHandler(object sender, AtomexClientChangedEventArgs args)
        {
            if (AtomexApp?.Account == null)
            {
                if (args.OldAtomexClient != null)
                    args.OldAtomexClient.ServiceStatusChanged -= OnAtomexClientServiceStatusChangedEventHandler;

                return;
            }

            args.AtomexClient.ServiceStatusChanged += OnAtomexClientServiceStatusChangedEventHandler;
        }

        private void OnAtomexClientServiceStatusChangedEventHandler(object sender, ServiceEventArgs args)
        {
            if (sender is not IAtomexClient atomexClient)
                return;

            // subscribe to symbols updates
            if (args.Service == Service.MarketData && args.Status == ServiceStatus.Connected)
            {
                atomexClient.SubscribeToMarketData(SubscriptionType.TopOfBook);
                atomexClient.SubscribeToMarketData(SubscriptionType.DepthTwenty);
            }
        }
    }
}
