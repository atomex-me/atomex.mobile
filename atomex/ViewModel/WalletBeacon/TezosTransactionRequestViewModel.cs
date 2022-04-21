using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Atomex;
using Atomex.Wallet.Tezos;
using Beacon.Sdk;
using Beacon.Sdk.Beacon.Operation;
using Netezos.Forging;
using Netezos.Forging.Models;
using Netezos.Keys;
using Netezos.Rpc;
using Xamarin.Forms;

namespace atomex.ViewModel.WalletBeacon
{
    public class TezosTransactionRequestViewModel : BaseViewModel
    {
        private readonly IAtomexApp _app;

        private readonly IWalletBeaconClient _walletBeaconClient;

        private readonly string _receiverId;

        public TezosTransactionRequestViewModel(
            IAtomexApp app,
            IWalletBeaconClient walletBeaconClient,
            string receiverId,
            INavigation navigation,
            OperationRequest operationRequest,
            PartialTezosTransactionOperation transactionOperation)
        {
            _app = app ?? throw new ArgumentNullException(nameof(app));
            _walletBeaconClient = walletBeaconClient ?? throw new ArgumentNullException(nameof(walletBeaconClient));
            _receiverId = receiverId ?? throw new ArgumentNullException(nameof(receiverId));

            Navigation = navigation ?? throw new ArgumentNullException(nameof(navigation));
            OperationRequest = operationRequest ?? throw new ArgumentNullException(nameof(operationRequest));
            TransactionOperation = transactionOperation ?? throw new ArgumentNullException(nameof(transactionOperation));

            SignCommand = new Command(async () => await SignAsync());
            CancelCommand = new Command(async () => await CancelAsync());
        }

        public INavigation Navigation { get; }

        public OperationRequest OperationRequest { get; }

        public PartialTezosTransactionOperation TransactionOperation { get; }

        public ICommand SignCommand { get; }

        public ICommand CancelCommand { get; }

        private async Task SignAsync()
        {
            if (!decimal.TryParse(TransactionOperation.Amount, out var amount))
                return;

            var account = _app.Account.GetCurrencyAccount<TezosAccount>(TezosConfig.Xtz);
         
            var permissionInfo = await _walletBeaconClient.TryReadPermissionInfo(OperationRequest.SourceAddress, OperationRequest.Network);

            if (permissionInfo != null)
            {
                await account.SendAsync(permissionInfo.PublicKey, TransactionOperation.Destination, amount, 1000);

                var response = new OperationResponse(
                            id: OperationRequest!.Id,
                            senderId: _walletBeaconClient.SenderId,
                            transactionHash: "transactionHash");

                await _walletBeaconClient.SendResponseAsync(receiverId: _receiverId, response);
            }
        }

        private async Task CancelAsync() => await Navigation.PopAsync();
    }
}

//private static async Task<string> MakeTransactionAsync(Key key, string destination, long amount)
//{
//    using var rpc = new TezosRpc("https://hangzhounet.api.tez.ie/");

//    // get a head block
//    string head = await rpc.Blocks.Head.Hash.GetAsync<string>();

//    // get account's counter
//    int counter = await rpc.Blocks.Head.Context.Contracts[key.Address].Counter.GetAsync<int>();

//    var content = new ManagerOperationContent[]
//    {
//        new TransactionContent
//        {
//            Source = key.PubKey.Address,
//            Counter = ++counter,
//            Amount = amount,
//            Destination = destination,
//            GasLimit = 1500,
//            Fee = 1000 // 0.001 tez
//        }
//    };

//    byte[] bytes = await new LocalForge().ForgeOperationGroupAsync(head, content);

//    // sign the operation bytes
//    byte[] signature = key.SignOperation(bytes);

//    // inject the operation and get its id (operation hash)
//    return await rpc.Inject.Operation.PostAsync(bytes.Concat(signature));
//}