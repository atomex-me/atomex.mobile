using System;
using System.Collections.ObjectModel;
using System.Reactive;
using System.Text;
using System.Threading.Tasks;
using Atomex;
using Atomex.Blockchain.Tezos;
using Atomex.Client.Common;
using Atomex.Common;
using Atomex.Cryptography;
using Atomex.ViewModels;
using Atomex.Wallet;
using Beacon.Sdk;
using Beacon.Sdk.Beacon;
using Beacon.Sdk.Beacon.Operation;
using Beacon.Sdk.Beacon.Permission;
using Beacon.Sdk.Beacon.Sign;
using Matrix.Sdk;
using Microsoft.Extensions.DependencyInjection;
using Netezos.Keys;
using Newtonsoft.Json;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Serilog;

namespace atomex.ViewModels
{
    public class DappViewModel : BaseViewModel
    {
        public string Logo => "BTC";
        public string Name { get; set; }
        public string ConnectedAddress { get; set; }
        public DateTime ConnectTime { get; set; }


        private ReactiveCommand<Unit, Unit> _openInExplorerCommand;

        public ReactiveCommand<Unit, Unit> OpenInExplorerCommand => _openInExplorerCommand ??= ReactiveCommand.Create(
            () => { Log.Information(ConnectedAddress); });

        private ReactiveCommand<Unit, Unit> _copyCommand;

        public ReactiveCommand<Unit, Unit> CopyCommand => _copyCommand ??= ReactiveCommand.Create(() =>
        {
            try
            {
                // App.Clipboard.SetTextAsync(ConnectedAddress);
            }
            catch (Exception e)
            {
                Log.Error(e, "Copy to clipboard error");
            }
        });
    }

    public class DappsViewModel : BaseViewModel
    {
        private readonly IAtomexApp _app;
        protected INavigationService _navigationService;
        private IWalletBeaconClient _beaconWalletClient;
        private ServiceProvider _beaconServicesProvider;

        [Reactive] public ObservableCollection<DappViewModel> Dapps { get; set; }
        public TezosConfig Tezos { get; set; }
        public SelectAddressViewModel SelectAddressViewModel { get; set; }
        public WalletAddressViewModel AddressToConnect { get; set; }
        // public ConnectDappViewModel ConnectDappViewModel { get; set; }

        public DappsViewModel(
            IAtomexApp atomexApp,
            INavigationService navigationService)
        {
            _app = atomexApp ?? throw new ArgumentNullException(nameof(atomexApp));
            _navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));

            var beaconServices = new ServiceCollection();
            beaconServices.AddLogging(loggingBuilder => loggingBuilder.AddSerilog(dispose: true));
            beaconServices.AddMatrixClient();
            beaconServices.AddBeaconClient();
            _beaconServicesProvider = beaconServices.BuildServiceProvider();

            _app.AtomexClientChanged += OnAtomexClientChangedEventHandler;

            Tezos = (TezosConfig) _app.Account.Currencies.GetByName(TezosConfig.Xtz);
            
            SelectAddressViewModel =
                new SelectAddressViewModel(
                    account: _app.Account,
                    currency: Tezos,
                    navigationService: _navigationService,
                    mode: SelectAddressMode.Connect)
                {
                    ConfirmAction = (selectAddressViewModel, walletAddressViewModel) =>
                    {
                        AddressToConnect = walletAddressViewModel;
                        // ConnectDappViewModel!.AddressToConnect = AddressToConnect.Address;
                        //     App.DialogService.Show(ConnectDappViewModel!);
                    }
                };

            // ConnectDappViewModel = new ConnectDappViewModel
            // {
            //     OnBack = () => App.DialogService.Show(SelectAddressViewModel),
            //     OnConnect = Connect
            // };
            //
            _ = Task.Run(async () =>
            {
                if (_app.Account == null) return;
            
                _beaconWalletClient = _beaconServicesProvider.GetRequiredService<IWalletBeaconClient>();
                _beaconWalletClient.OnBeaconMessageReceived += OnBeaconWalletClientMessageReceived;
                await _beaconWalletClient.InitAsync();
                _beaconWalletClient.Connect();

                Log.Debug("{@Sender}: WalletClient connected {@Connected}", "Beacon", _beaconWalletClient.Connected);
                Log.Debug("{@Sender}: WalletClient logged in {@LoggedIn}", "Beacon", _beaconWalletClient.LoggedIn);
            });
        }

        private async void Connect(string qrCodeString)
        {
            if (_app.Account == null) return;

            Log.Debug("{@Sender}: WalletClient connected {@Connected}", "Beacon", _beaconWalletClient.Connected);
            Log.Debug("{@Sender}: WalletClient logged in {@LoggedIn}", "Beacon", _beaconWalletClient.LoggedIn);

            var pairingRequest = ConnectToPeer();
            await _beaconWalletClient.AddPeerAsync(pairingRequest);

            P2PPairingRequest ConnectToPeer()
            {
                var decodedQr = Base58Check.Decode(qrCodeString);
                var message = Encoding.UTF8.GetString(decodedQr.ToArray());
                return JsonConvert.DeserializeObject<P2PPairingRequest>(message);
            }
        }

        private void OnBeaconWalletClientMessageReceived(object? sender, BeaconMessageEventArgs e)
        {
            var message = e.Request;

            Log.Debug("{@Sender}: msg with type {@Type}, id {@Id} received",
                "Beacon",
                message.Type,
                message.Id);

            switch (message.Type)
            {
                case BeaconMessageType.permission_request:
                {
                    if (message is not PermissionRequest permissionRequest) return;

                    if (string.IsNullOrEmpty(permissionRequest.Network.RpcUrl))
                        permissionRequest.Network.RpcUrl = $"https://rpc.tzkt.io/{permissionRequest.Network.Type}";

                    if (string.IsNullOrEmpty(permissionRequest.Network.Name))
                        permissionRequest.Network.Name = permissionRequest.Network.Type.ToString();

                    // swap sign permission to encrypt
                    // var scopes = request?.Scopes
                    //     .Select(s => s == PermissionScope.sign ? PermissionScope.encrypt : s)
                    //     .ToList();
                    //
                    // if (request!.Scopes.Any(s => s == PermissionScope.sign))
                    //     request.Scopes.Add(PermissionScope.encrypt);

                    var hdWallet = _app.Account.Wallet as HdWallet;

                    using var privateKey = hdWallet!.KeyStorage.GetPrivateKey(
                        currency: Tezos,
                        keyIndex: AddressToConnect.WalletAddress.KeyIndex,
                        keyType: AddressToConnect.WalletAddress.KeyType);

                    var unsecuredPrivateKey = privateKey.ToUnsecuredBytes();

                    var walletKey = Key.FromBytes(unsecuredPrivateKey);

                    var response = new PermissionResponse(
                        id: permissionRequest!.Id,
                        senderId: _beaconWalletClient.SenderId,
                        appMetadata: _beaconWalletClient.Metadata,
                        network: permissionRequest.Network,
                        scopes: permissionRequest.Scopes,
                        publicKey: walletKey.PubKey.ToString(),
                        address: walletKey.PubKey.Address,
                        version: permissionRequest.Version);

                    _ = _beaconWalletClient.SendResponseAsync(receiverId: e.SenderId, response);

                    Log.Fatal($"Permission response vs addr {walletKey.PubKey.Address}");
                    break;
                }
                case BeaconMessageType.operation_request:
                {
                    if (message is not OperationRequest operationRequest) return;

                    // todo: return error response;
                    if (operationRequest.OperationDetails.Count <= 0) return;

                    var operation = operationRequest.OperationDetails[0];

                    if (long.TryParse(operation?.Amount, out var amount))
                    {
                        // string transactionHash =
                        //     await MakeTransactionAsync(walletKey, operation.Destination, amount);

                        var response = new OperationResponse(
                            id: operationRequest.Id,
                            senderId: _beaconWalletClient.SenderId,
                            transactionHash: "txHash",
                            operationRequest.Version);

                        _ = _beaconWalletClient.SendResponseAsync(receiverId: e.SenderId, response);
                    }

                    break;
                }
                case BeaconMessageType.sign_payload_request:
                {
                    if (message is not SignPayloadRequest signRequest) return;

                    byte[] dataToSign;

                    try
                    {
                        dataToSign = Hex.FromString(signRequest.Payload);
                    }
                    catch (Exception)
                    {
                        // data is not in HEX format
                        dataToSign = Encoding.UTF8.GetBytes(signRequest.Payload);
                    }

                    var hdWallet = _app.Account.Wallet as HdWallet;

                    using var privateKey = hdWallet!.KeyStorage.GetPrivateKey(
                        currency: Tezos,
                        keyIndex: AddressToConnect.WalletAddress.KeyIndex,
                        keyType: AddressToConnect.WalletAddress.KeyType);

                    var signedMessage = TezosSigner.SignHash(
                        data: dataToSign,
                        privateKey: privateKey.ToUnsecuredBytes(),
                        watermark: null,
                        isExtendedKey: privateKey.Length == 64);

                    var response = new SignPayloadResponse(
                        signature: signedMessage.EncodedSignature,
                        version: signRequest.Version,
                        id: signRequest.Id,
                        senderId: _beaconWalletClient.SenderId);

                    _ = _beaconWalletClient.SendResponseAsync(receiverId: e.SenderId, response);
                    Log.Fatal($"Payload: {signRequest.Payload}");
                    Log.Fatal($"SIGNATURE: {signedMessage.EncodedSignature}");
                    break;
                }
                case BeaconMessageType.broadcast_request:
                    break;
                case BeaconMessageType.permission_response:
                    break;
                case BeaconMessageType.sign_payload_response:
                    break;
                case BeaconMessageType.operation_response:
                    break;
                case BeaconMessageType.broadcast_response:
                    break;
                case BeaconMessageType.acknowledge:
                    break;
                case BeaconMessageType.disconnect:
                    break;
                case BeaconMessageType.error:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }


        private void OnAtomexClientChangedEventHandler(object? sender, AtomexClientChangedEventArgs args)
        {
            if (args.AtomexClient == null || _app.Account == null) return;

            var a = 5;
        }
    }
}