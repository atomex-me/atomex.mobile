using System;
using Atomex;
using Microsoft.Extensions.Configuration;
using Atomex.Wallet.Abstract;
using Atomex.Common.Configuration;
using System.Linq;
using atomex.Services;
using Atomex.Services;
using atomex.ViewModel.CurrencyViewModels;
using Atomex.Common;
using Xamarin.Forms;
using Atomex.Client.Common;
using Atomex.Client.Abstract;
using Atomex.Client.V1.Entities;

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

            ClientType clientType;

            switch (Device.RuntimePlatform)
            {
                case Device.iOS:
                    clientType = ClientType.iOS;
                    break;
                case Device.Android:
                    clientType = ClientType.Android;
                    break;
                default:
                    clientType = ClientType.Unknown;
                    break;
            }

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
        }

        private void SubscribeToServices()
        {
            AtomexApp.AtomexClientChanged += OnAtomexClientChangedEventHandler;
        }

        private void OnAtomexClientChangedEventHandler(object sender, AtomexClientChangedEventArgs args)
        {
            if (AtomexApp?.Account == null)
            {
                CurrencyViewModelCreator.Reset();
                TezosTokenViewModelCreator.Reset();
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
