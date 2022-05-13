using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Atomex;
using Atomex.Common;
using Atomex.Core;
using Atomex.Wallet.Tezos;
using Beacon.Sdk;
using Beacon.Sdk.Beacon;
using Beacon.Sdk.Beacon.Permission;
using Netezos.Keys;
using Serilog;
using Xamarin.Forms;
using Network = Beacon.Sdk.Beacon.Permission.Network;

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

        private readonly string _receiverId;

        public ConnectDappViewModel(
            IAtomexApp app,
            IWalletBeaconClient walletBeaconClient,
            string receiverId,
            INavigation navigation,
            PermissionRequest permissionRequest)
        {
            _app = app ?? throw new ArgumentNullException(nameof(app));
            _walletBeaconClient = walletBeaconClient ?? throw new ArgumentNullException(nameof(walletBeaconClient));
            _receiverId = receiverId ?? throw new ArgumentNullException(nameof(receiverId));

            PermissionRequest = permissionRequest ?? throw new ArgumentNullException(nameof(permissionRequest));
            Navigation = navigation ?? throw new ArgumentNullException(nameof(navigation));

            ConnectCommand = new Command(async () => await ConnectAsync());
            CancelCommand = new Command(async () => await CancelAsync());

            Permissions = new List<PermissionModel>();

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

        public PermissionRequest PermissionRequest { get; }

        public List<PermissionModel> Permissions { get; }

        public ICommand ConnectCommand { get; }

        public ICommand CancelCommand { get; }

        private async Task ConnectAsync()
        {

            var account = _app.Account.GetCurrencyAccount<TezosAccount>(TezosConfig.Xtz);
            var addresses = (await account.GetAddressesAsync()).ToList();

            if (addresses.Count == 0)
            {
                Log.Error("No adresses");
                return;
            }

            addresses.Sort((a1, a2) =>
            {
                var typeResult = a1.KeyType.CompareTo(a2.KeyType);

                if (typeResult != 0)
                    return typeResult;

                var accountResult = a1.KeyIndex.Account.CompareTo(a2.KeyIndex.Account);

                if (accountResult != 0)
                    return accountResult;

                var chainResult = a1.KeyIndex.Chain.CompareTo(a2.KeyIndex.Chain);

                return chainResult != 0
                   ? chainResult
                   : a1.KeyIndex.Index.CompareTo(a2.KeyIndex.Index);
            });

            var responseAddress = addresses[0].ResolvePublicKey(account.Currencies, account.Wallet); ;

            var network2 = account.Wallet.Network;
            
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

            var publicKey = PubKey.FromBase64(responseAddress.PublicKey);
            string address = publicKey.Address;

            var response = new PermissionResponse(
                id: PermissionRequest!.Id,
                senderId: _walletBeaconClient.SenderId,
                network: network,
                scopes: PermissionRequest.Scopes,
                publicKey: publicKey.ToString(),
                address: address,
                appMetadata: _walletBeaconClient.Metadata);

            await _walletBeaconClient.SendResponseAsync(receiverId: _receiverId, response);//.ConfigureAwait(false);

            Device.BeginInvokeOnMainThread(async () =>
            {
                await Navigation.PopAsync();
                await Navigation.PopAsync();
                await Navigation.PopAsync();
            });
        }

        private async Task CancelAsync()
        {
            await Navigation.PopAsync();
            await Navigation.PopAsync();
            await Navigation.PopAsync();
        }
    }
}
