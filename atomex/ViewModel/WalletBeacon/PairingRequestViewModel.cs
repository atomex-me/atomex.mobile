using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Input;
using Beacon.Sdk;
using Beacon.Sdk.Beacon;
using Xamarin.Forms;

namespace atomex.ViewModel.WalletBeacon
{
    public class PairingRequestViewModel : BaseViewModel
    {
        public INavigation Navigation { get; }

        private readonly IWalletBeaconClient _walletBeaconClient;

        public P2PPairingRequest PairingRequest { get; }

        public PairingRequestViewModel(INavigation navigation, IWalletBeaconClient walletBeaconClient, P2PPairingRequest pairingRequest)
        {
            Navigation = navigation ?? throw new ArgumentNullException(nameof(navigation));
            _walletBeaconClient = walletBeaconClient ?? throw new ArgumentNullException(nameof(walletBeaconClient));
            PairingRequest = pairingRequest ?? throw new ArgumentNullException(nameof(pairingRequest));

            ConnectCommand = new Command(async () => await ConnectAsync());
            CancelCommand = new Command(async () => await CancelAsync());
        }

        public ICommand ConnectCommand { get; }
        private async Task ConnectAsync()
        {
            await _walletBeaconClient.AddPeerAsync(PairingRequest);
        }

        public ICommand CancelCommand { get; }
        private async Task CancelAsync() => await Navigation.PopAsync();


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
