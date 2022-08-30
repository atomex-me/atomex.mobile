using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Atomex;
using Atomex.Wallet.Tezos;
using Beacon.Sdk;
using Beacon.Sdk.Beacon.Operation;
using Serilog;
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
            try
            {
                if (!decimal.TryParse(TransactionOperation.Amount, out var amount))
                    return;

                var account = _app.Account.GetCurrencyAccount<TezosAccount>(TezosConfig.Xtz);

                var permissionInfo = await _walletBeaconClient.TryReadPermissionInfo(OperationRequest.SourceAddress, OperationRequest.SenderId, OperationRequest.Network);

                if (permissionInfo != null)
                {
                    var result = await account.SendAsync(permissionInfo.PublicKey, TransactionOperation.Destination, amount, 1000);

                    var response = new OperationResponse(
                                id: OperationRequest!.Id,
                                senderId: _walletBeaconClient.SenderId,
                                transactionHash: "transactionHash",
                                version: "2");

                    await _walletBeaconClient.SendResponseAsync(receiverId: _receiverId, response);
                }
            }
            catch (Exception ex) 
            {
                Log.Error(ex, $"Beacon message error");
            }
        }

        private async Task CancelAsync() => await Navigation.PopAsync();
    }
}