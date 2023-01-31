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
using System.Threading.Tasks;
using atomex.Common;
using atomex.ViewModels.ConversionViewModels;
using atomex.ViewModels.CurrencyViewModels;
using atomex.ViewModels.DappsViewModels;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Xamarin.Essentials;
using Xamarin.Forms;
using Log = Serilog.Log;

namespace atomex.ViewModels
{
    public class MainViewModel : BaseViewModel
    {
        public IAtomexApp AtomexApp { get; }
        public SettingsViewModel SettingsViewModel { get; }
        public ConversionViewModel ConversionViewModel { get; }
        public PortfolioViewModel PortfolioViewModel { get; }
        public BuyViewModel BuyViewModel { get; }
        [Reactive] public ConnectDappViewModel ConnectDappViewModel { get; set; }

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
            PortfolioViewModel.CurrenciesLoaded += OnCurrenciesLoadedEventHandler;

            _ = TokenDeviceService.SendTokenToServerAsync(App.DeviceToken, App.FileSystem, AtomexApp);

            this.WhenAnyValue(vm => vm.ConnectDappViewModel)
                .WhereNotNull()
                .SubscribeInMainThread(async _ =>
                {
                    string deepLink = await SecureStorage.GetAsync("DappDeepLink");
                    if (string.IsNullOrEmpty(deepLink)) 
                        return;
                    await ConnectDappViewModel.OnDeepLinkResult(deepLink);
                    await SecureStorage.SetAsync("DappDeepLink", string.Empty);
                });
        }
        
        private void OnCurrenciesLoadedEventHandler(object sender, EventArgs args)
        {
            try
            {
                var tezosViewModel = PortfolioViewModel.AllCurrencies!
                    .First(c => c.CurrencyViewModel.CurrencyCode == TezosConfig.Xtz)
                    .CurrencyViewModel as TezosCurrencyViewModel;
                
                if (tezosViewModel == null) return;
                ConnectDappViewModel = tezosViewModel.DappsViewModel.ConnectDappViewModel;
            }
            catch (Exception e)
            {
                Log.Error(e, "Portfolio amount error");
            }
        }

        public void ScanCurrencies(string[] currencies = null)
        {
            try
            {
                PortfolioViewModel.CurrenciesForScan = currencies;
            }
            catch (Exception e)
            {
                Log.Error(e, "Set currencies for scan error");
            }
        }
        
        public void RestoreWallet()
        {
            try
            {
                PortfolioViewModel.Restore = true;
            }
            catch (Exception e)
            {
                Log.Error(e, "Set restore wallet error");
            }
        }

        public void SignOut()
        {
            AtomexApp?.ChangeAtomexClient(atomexClient: null, account: null);

            ConversionViewModel?.Reset();
            CurrencyViewModelCreator.Reset();
            TezosTokenViewModelCreator.Reset();

            if (AtomexApp == null) return;
            AtomexApp.AtomexClientChanged -= OnAtomexClientChangedEventHandler;
        }
        
        public void AllowCamera()
        {
            ConnectDappViewModel?.AllowCamera();
        }
        
        public async Task ConnectDappByDeepLink(string qrCodeString)
        {
            if (ConnectDappViewModel != null)
                await ConnectDappViewModel.OnDeepLinkResult(qrCodeString);
        }

        private void SubscribeToServices()
        {
            AtomexApp.AtomexClientChanged += OnAtomexClientChangedEventHandler;
        }

        private void OnAtomexClientChangedEventHandler(object sender, AtomexClientChangedEventArgs args)
        {
            if (AtomexApp?.Account == null)
            {
                if (args?.OldAtomexClient != null)
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
