using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Input;
using Atomex;
using Beacon.Sdk;
using Beacon.Sdk.Beacon;
using Beacon.Sdk.Beacon.Permission;
using Netezos.Keys;
using Xamarin.Forms;

namespace atomex.ViewModel.WalletBeacon
{
    public class PermissionModel
    {
        public PermissionScope Scope { get; set; }
        public bool IsChecked { get; set; }
    }

    public class ConnectDappViewModel : BaseViewModel
    {
        private readonly IAtomexApp _app;
        private readonly IWalletBeaconClient _walletBeaconClient;
        private readonly PermissionRequest _permissionRequest;
        private readonly string _receiverId;

        public ConnectDappViewModel(INavigation navigation, IWalletBeaconClient walletBeaconClient,
            PermissionRequest permissionRequest, string receiverId)
        {
            Navigation = navigation ?? throw new ArgumentNullException(nameof(navigation));
            _walletBeaconClient = walletBeaconClient ?? throw new ArgumentNullException(nameof(walletBeaconClient));
            _permissionRequest = permissionRequest ?? throw new ArgumentNullException(nameof(permissionRequest));
            _receiverId = receiverId ?? throw new ArgumentNullException(nameof(receiverId));
            Permissions = new List<PermissionModel>();

            ConnectCommand = new Command(async () => await ConnectAsync());
            CancelCommand = new Command(async () => await CancelAsync());

            foreach (var scope in permissionRequest.Scopes)
            {
                Permissions.Add(new PermissionModel
                {
                    Scope = scope,
                    IsChecked = true
                });
            }
        }

        public INavigation Navigation { get; set; }

        public List<PermissionModel> Permissions { get; }


        public ICommand ConnectCommand { get; }
        private async Task ConnectAsync()
        {
            //_app.Account.Wallet.

            // ToDo: refactor
            var walletKey = Key.FromBase58("edsk35n2ruX2r92SdtWzP87mEUqxWSwM14hG6GRhEvU6kfdH8Ut6SW");

            var network = new Network
            {
                Type = NetworkType.hangzhounet,
                Name = "Hangzhounet",
                RpcUrl = "https://hangzhounet.tezblock.io"
            };

            var scopes = new List<PermissionScope>();
            foreach (var permission in Permissions)
                if (permission.IsChecked)
                    scopes.Add(permission.Scope);

            var response = new PermissionResponse(
                id: _permissionRequest!.Id,
                senderId: _walletBeaconClient.SenderId,
                network: network,
                scopes: _permissionRequest.Scopes,
                publicKey: walletKey.PubKey.ToString(),
                appMetadata: _walletBeaconClient.Metadata);

            await _walletBeaconClient.SendResponseAsync(receiverId: _receiverId, response);//.ConfigureAwait(false);

            Device.BeginInvokeOnMainThread(async () =>
            {
                await Navigation.PopAsync();
                await Navigation.PopAsync();
            });
        }

        public ICommand CancelCommand { get; }
        private async Task CancelAsync()
        {
            await Navigation.PopAsync();
            await Navigation.PopAsync();
        }
    }
}
