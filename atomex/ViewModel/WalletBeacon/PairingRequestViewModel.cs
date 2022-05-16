using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Input;
using atomex.Views.WalletBeacon;
using Atomex;
using Beacon.Sdk;
using Beacon.Sdk.Beacon;
using Beacon.Sdk.Beacon.Permission;
using Xamarin.Forms;

namespace atomex.ViewModel.WalletBeacon
{
    public class PairingRequestViewModel : BaseViewModel
    {
        private readonly IAtomexApp _app;

        private readonly IWalletBeaconClient _walletBeaconClient;

        public PairingRequestViewModel(
            IAtomexApp app,
            IWalletBeaconClient walletBeaconClient,
            INavigation navigation,
            P2PPairingRequest pairingRequest)
        {
            _app = app ?? throw new ArgumentNullException(nameof(app));
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

        public ICommand CancelCommand { get; }

        public void OnDisappearing()
        {
            _walletBeaconClient.OnBeaconMessageReceived -= OnBeaconMessageReceived;
        }

        private void OnBeaconMessageReceived(object sender, BeaconMessageEventArgs args)
        {
            BaseBeaconMessage message = args.Request;

            if (message.Type == BeaconMessageType.permission_request)
            {
                var request = message as PermissionRequest;

                Device.BeginInvokeOnMainThread(async () =>
                {
                    await Navigation.PushAsync(
                        new ConnectDappPage(
                            new ConnectDappViewModel(
                                _app,
                                _walletBeaconClient,
                                args.SenderId,
                                Navigation,
                                request)));
                });

                _walletBeaconClient.OnBeaconMessageReceived -= OnBeaconMessageReceived;
            }
        }

        private async Task ConnectAsync() => await _walletBeaconClient.AddPeerAsync(PairingRequest).ConfigureAwait(false);

        private async Task CancelAsync() => await Navigation.PopAsync();
    }
}