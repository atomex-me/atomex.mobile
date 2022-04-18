using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Input;
using atomex.Views.WalletBeacon;
using Beacon.Sdk;
using Beacon.Sdk.Beacon;
using Beacon.Sdk.Beacon.Permission;
using Xamarin.Forms;

namespace atomex.ViewModel.WalletBeacon
{
    public class PairingRequestViewModel : BaseViewModel, IDisposable
    {
        private readonly IWalletBeaconClient _walletBeaconClient;

        private bool disposedValue;

        public PairingRequestViewModel(
            IWalletBeaconClient walletBeaconClient,
            INavigation navigation,
            P2PPairingRequest pairingRequest)
        {
            _walletBeaconClient = walletBeaconClient ?? throw new ArgumentNullException(nameof(walletBeaconClient));
            Navigation = navigation ?? throw new ArgumentNullException(nameof(navigation));
            PairingRequest = pairingRequest ?? throw new ArgumentNullException(nameof(pairingRequest));

            ConnectCommand = new Command(async () => await ConnectAsync());
            CancelCommand = new Command(async () => await CancelAsync());

            _walletBeaconClient.OnBeaconMessageReceived += OnBeaconMessageReceived;
        }

        public INavigation Navigation { get; }

        public P2PPairingRequest PairingRequest { get; }

        public ICommand ConnectCommand { get; }
        private async Task ConnectAsync() => await _walletBeaconClient.AddPeerAsync(PairingRequest).ConfigureAwait(false);

        public ICommand CancelCommand { get; }
        private async Task CancelAsync() => await Navigation.PopAsync();

        private void OnBeaconMessageReceived(object sender, BeaconMessageEventArgs args)
        {
            BaseBeaconMessage message = args.Request;

            if (message.Type == BeaconMessageType.permission_request)
            {
                var request = message as PermissionRequest;

                Device.BeginInvokeOnMainThread(async () =>
                {
                    await Navigation.PushAsync(new ConnectDappPage(new ConnectDappViewModel(Navigation, _walletBeaconClient, request, args.SenderId)));
                });

                _walletBeaconClient.OnBeaconMessageReceived -= OnBeaconMessageReceived;
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects)

                    _walletBeaconClient.OnBeaconMessageReceived -= OnBeaconMessageReceived;
                }

                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                // TODO: set large fields to null
                disposedValue = true;
            }
        }

        void IDisposable.Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }



        //void LoadDapp()
        //{
        //    Dapp = new DappInfo()
        //    {
        //        Name = "Dapp",
        //        Network = Network.TestNet,
        //        ImageUrl = "BTC",
        //        DappDeviceType = DappType.Mobile,
        //        Permissions = new List<Permission>()
        //        {
        //            new Permission() { Name = "Ты можешь сидеть?" },
        //            new Permission() { Name = "Тебе еще что-то надо?" }
        //        }
        //    };
        //}
    }
}
