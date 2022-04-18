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

        public TezosTransactionRequestViewModel(
            IAtomexApp app,
            IWalletBeaconClient walletBeaconClient,
            INavigation navigation,
            PartialTezosTransactionOperation operation)
        {
            _app = app ?? throw new ArgumentNullException(nameof(app));
            _walletBeaconClient = walletBeaconClient ?? throw new ArgumentNullException(nameof(walletBeaconClient));
            Navigation = navigation ?? throw new ArgumentNullException(nameof(navigation));
            Operation = operation ?? throw new ArgumentNullException(nameof(operation));

            SignCommand = new Command(async () => await SignAsync());
            CancelCommand = new Command(async () => await CancelAsync());
        }

        public INavigation Navigation { get; }

        public PartialTezosTransactionOperation Operation { get; }

        public ICommand SignCommand { get; }
        private async Task SignAsync()
        {
            var account = _app.Account.GetCurrencyAccount<TezosAccount>("XTZ");//.Account.GetCurrencyAccount<TezosAccount>(Currency.Name);
            //Atomex.Wallet.Abstract.ICurrencyAccount account =_app.Account.GetCurrencyAccount(TezosConfig.Xtz);
        }

        public ICommand CancelCommand { get; }
        private async Task CancelAsync() => await Navigation.PopAsync();


        private static async Task<string> MakeTransactionAsync(Key key, string destination, long amount)
        {
            using var rpc = new TezosRpc("https://hangzhounet.api.tez.ie/");

            // get a head block
            string head = await rpc.Blocks.Head.Hash.GetAsync<string>();

            // get account's counter
            int counter = await rpc.Blocks.Head.Context.Contracts[key.Address].Counter.GetAsync<int>();

            var content = new ManagerOperationContent[]
            {
                new TransactionContent
                {
                    Source = key.PubKey.Address,
                    Counter = ++counter,
                    Amount = amount,
                    Destination = destination,
                    GasLimit = 1500,
                    Fee = 1000 // 0.001 tez
                }
            };

            byte[] bytes = await new LocalForge().ForgeOperationGroupAsync(head, content);

            // sign the operation bytes
            byte[] signature = key.SignOperation(bytes);

            // inject the operation and get its id (operation hash)
            return await rpc.Inject.Operation.PostAsync(bytes.Concat(signature));
        }
    }
}
