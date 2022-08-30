using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Reactive;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Atomex;
using Atomex.Blockchain.Tezos;
using Atomex.Client.Common;
using Atomex.Common;
using Atomex.Cryptography;
using Atomex.ViewModels;
using atomex.Views;
using atomex.Views.Dapps;
using Atomex.Wallet;
using Beacon.Sdk;
using Beacon.Sdk.Beacon;
using Beacon.Sdk.Beacon.Error;
using Beacon.Sdk.Beacon.Operation;
using Beacon.Sdk.Beacon.Permission;
using Beacon.Sdk.Beacon.Sign;
using Matrix.Sdk;
using Microsoft.Extensions.DependencyInjection;
using Netezos.Keys;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Serilog;
using Xamarin.Essentials;
using Xamarin.Forms;

namespace atomex.ViewModels.DappsViewModels
{
    public class DappViewModel : BaseViewModel
    {
        public string Logo => "BTC";
        public string Name { get; set; }
        public WalletAddressViewModel ConnectedAddress { get; set; }
        public DateTime ConnectTime { get; set; }


        private ReactiveCommand<Unit, Unit> _openInExplorerCommand;

        public ReactiveCommand<Unit, Unit> OpenInExplorerCommand => _openInExplorerCommand ??= ReactiveCommand.Create(
            () =>
            {
                
            });

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
        private INavigationService _navigationService;
        private IWalletBeaconClient _beaconWalletClient;
        private ServiceProvider _beaconServicesProvider;

        [Reactive] public ObservableCollection<DappViewModel> Dapps { get; set; }
        public TezosConfig Tezos { get; set; }
        public SelectAddressViewModel SelectAddressViewModel { get; set; }
        public WalletAddressViewModel AddressToConnect { get; set; }
        public ConnectDappViewModel ConnectDappViewModel { get; set; }

        public DappsViewModel(
            IAtomexApp atomexApp,
            INavigationService navigationService)
        {
            _app = atomexApp ?? throw new ArgumentNullException(nameof(atomexApp));
            _navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));

            var beaconServices = new ServiceCollection();
            beaconServices.AddLogging(loggingBuilder => loggingBuilder.AddSerilog(dispose: true));
            beaconServices.AddMatrixClient();
            beaconServices.AddBeaconClient(
                pathToDb: Path.GetDirectoryName(_app.Account.Wallet.PathToWallet)!,
                appName: "Atomex mobile");
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
                    ConfirmAction = async (selectAddressViewModel, walletAddressViewModel) =>
                    {
                        AddressToConnect = walletAddressViewModel;
                        ConnectDappViewModel!.AddressToConnect = AddressToConnect.Address;
                        PermissionStatus permissions = await Permissions.CheckStatusAsync<Permissions.Camera>();

                        if (permissions != PermissionStatus.Granted)
                            permissions = await Permissions.RequestAsync<Permissions.Camera>();
                        if (permissions != PermissionStatus.Granted)
                            return;

                        ConnectDappViewModel!.IsScanning = true;
                        ConnectDappViewModel!.IsAnalyzing = true;
                        _navigationService?.ShowPage(new ScanningQrPage(ConnectDappViewModel), TabNavigation.Portfolio);
                    }
                };

            ConnectDappViewModel = new ConnectDappViewModel(_navigationService)
            {
                // OnBack = () => App.DialogService.Show(SelectAddressViewModel),
                OnConnect = Connect
            };
            
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

        private async Task Connect(string qrCodeString, string addressToConnect)
        {
            if (_app.Account == null) return;
           
            try
            {
                var pairingRequest = GetPairingRequest();
                await _beaconWalletClient.AddPeerAsync(pairingRequest, addressToConnect);
                
                P2PPairingRequest GetPairingRequest()
                {
                    var decodedQr = Base58Check.Decode(qrCodeString);
                    var message = Encoding.UTF8.GetString(decodedQr.ToArray());
                    return JsonConvert.DeserializeObject<P2PPairingRequest>(message);
                }
            }
            catch (Exception e)
            {
                Log.Error(e, "Connect dApp error");
            }
        }

        private ReactiveCommand<Unit, Unit> _newDappCommand;
        public ReactiveCommand<Unit, Unit> NewDappCommand => _newDappCommand ??= ReactiveCommand.Create(() =>
        {
            _navigationService?.ShowPage(new SelectAddressPage(SelectAddressViewModel), TabNavigation.Portfolio);
        });

        private async void OnBeaconWalletClientMessageReceived(object? sender, BeaconMessageEventArgs e)
        {
            var message = e.Request;
            var peer = _beaconWalletClient.GetPeer(message.SenderId);

            if (peer == null)
            {
                _ = _beaconWalletClient.SendResponseAsync(receiverId: e.SenderId,
                    new BeaconAbortedError(message.Id, _beaconWalletClient.SenderId));
                return;
            }

            Log.Debug("{@Sender}: msg with type {@Type}, from {@Peer} received",
                "Beacon",
                peer.Name,
                message.Id);

            var connectedWalletAddress = await _app.Account
                .GetAddressAsync(Tezos.Name, peer.ConnectedAddress);

            switch (message.Type)
            {
                case BeaconMessageType.permission_request:
                {
                    if (message is not PermissionRequest permissionRequest) return;

                    if (permissionRequest.Network.Type == NetworkType.mainnet &&
                        _app.Account.Network != Atomex.Core.Network.MainNet)
                    {
                        // todo: change response Error type;
                        _ = _beaconWalletClient.SendResponseAsync(receiverId: e.SenderId,
                            new BeaconAbortedError(permissionRequest.Id, _beaconWalletClient.SenderId));
                        return;
                    }

                    if (string.IsNullOrEmpty(permissionRequest.Network.RpcUrl))
                        permissionRequest.Network.RpcUrl = $"https://rpc.tzkt.io/{permissionRequest.Network.Type}";

                    if (string.IsNullOrEmpty(permissionRequest.Network.Name))
                        permissionRequest.Network.Name = permissionRequest.Network.Type.ToString();

                    var response = new PermissionResponse(
                        id: permissionRequest.Id,
                        senderId: _beaconWalletClient.SenderId,
                        appMetadata: _beaconWalletClient.Metadata,
                        network: permissionRequest.Network,
                        scopes: permissionRequest.Scopes,
                        publicKey: PubKey.FromBase64(connectedWalletAddress.PublicKey).ToString(),
                        address: connectedWalletAddress.Address,
                        version: permissionRequest.Version);

                    // var permissionRequestViewModel = new PermissionRequestViewModel
                    // {
                    //     DappName = permissionRequest.AppMetadata.Name,
                    //     Address = connectedWalletAddress.Address,
                    //     Permissions = permissionRequest.Scopes,
                    //     OnReject = async () =>
                    //     {
                    //         await BeaconWalletClient.SendResponseAsync(receiverId: e.SenderId,
                    //             new BeaconAbortedError(permissionRequest.Id, BeaconWalletClient.SenderId));
                    //         await BeaconWalletClient.RemovePeerAsync(message.SenderId);
                    //         App.DialogService.Close();
                    //         Log.Information(
                    //             "{@Sender}: Rejected permissions [{@PermissionsList}] to dapp {@Dapp} with address {@Address}",
                    //             "Beacon",
                    //             permissionRequest.Scopes.Aggregate(string.Empty,
                    //                 (res, scope) => res + $"{scope.ToString()}, "),
                    //             permissionRequest.AppMetadata.Name, connectedWalletAddress.Address);
                    //     },
                    //     OnAllow = async () =>
                    //     {
                    //         await BeaconWalletClient.SendResponseAsync(receiverId: e.SenderId, response);
                    //         App.DialogService.Close();
                    //         Log.Information(
                    //             "{@Sender}: Issued permissions [{@PermissionsList}] to dapp {@Dapp} with address {@Address}",
                    //             "Beacon",
                    //             permissionRequest.Scopes.Aggregate(string.Empty,
                    //                 (res, scope) => res + $"{scope.ToString()}, "),
                    //             permissionRequest.AppMetadata.Name, connectedWalletAddress.Address);
                    //     }
                    // };

                    await Device.InvokeOnMainThreadAsync(() =>
                    {
                        _navigationService?.ShowBottomSheet(new PermissionRequestPopup());
                    });
                    break;
                }
                case BeaconMessageType.operation_request:
                {
                    if (message is not OperationRequest operationRequest) return;

                    if (operationRequest.OperationDetails.Count != 1)
                    {
                        _ = _beaconWalletClient.SendResponseAsync(receiverId: e.SenderId,
                            new BeaconAbortedError(operationRequest.Id, _beaconWalletClient.SenderId));
                    }

                    var operation = operationRequest.OperationDetails[0];

                    if (long.TryParse(operation?.Amount, out var amount))
                    {
                        var autofillOperation = await RunAutofillOperation(
                            fromAddress: connectedWalletAddress.Address,
                            toAddress: operation.Destination,
                            amount: amount,
                            operationParams: operation.Parameters!);

                        if (autofillOperation.HasError)
                        {
                            Log.Error("Autofill error");
                            _ = _beaconWalletClient.SendResponseAsync(receiverId: e.SenderId,
                                new BeaconAbortedError(operationRequest.Id, _beaconWalletClient.SenderId));
                            return;
                        }

                        var (tx, isSuccess, isRunSuccess) = autofillOperation.Value;

                        if (!isSuccess || !isRunSuccess)
                        {
                            Log.Error("Autofill isSuccess {@IsSuccess} isRunSuccess {@IsRunSuccess}", isSuccess,
                                isRunSuccess);
                            _ = _beaconWalletClient.SendResponseAsync(receiverId: e.SenderId,
                                new BeaconAbortedError(operationRequest.Id, _beaconWalletClient.SenderId));
                            return;
                        }

                        tx.GasLimit = tx.Operations?.Last?["gas_limit"]?.Value<decimal>() ?? Tezos.GasLimit;
                        tx.StorageLimit = tx.Operations?.Last?["storage_limit"]?.Value<decimal>() ?? Tezos.StorageLimit;

                        var wallet = (HdWallet)_app.Account.Wallet;
                        var keyStorage = wallet.KeyStorage;

                        var signResult = await tx
                            .SignAsync(keyStorage, connectedWalletAddress, Tezos);

                        if (!signResult)
                        {
                            Log.Error("Beacon transaction signing error");
                            _ = _beaconWalletClient.SendResponseAsync(receiverId: e.SenderId,
                                new BeaconAbortedError(operationRequest.Id, _beaconWalletClient.SenderId));
                            return;
                        }

                        var result = await Tezos.BlockchainApi
                            .TryBroadcastAsync(tx);

                        if (result.Error != null)
                        {
                            Log.Error("Beacon transaction broadcast error");
                            _ = _beaconWalletClient.SendResponseAsync(receiverId: e.SenderId,
                                new BeaconAbortedError(operationRequest.Id, _beaconWalletClient.SenderId));
                            return;
                        }

                        var response = new OperationResponse(
                            id: operationRequest.Id,
                            senderId: _beaconWalletClient.SenderId,
                            transactionHash: result.Value,
                            operationRequest.Version);

                        _ = _beaconWalletClient.SendResponseAsync(receiverId: e.SenderId, response);

                        Log.Information("{@Sender}: operation done with transaction hash: {@Hash}", "Beacon",
                            result.Value);
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

                    var permissionInfo =
                        await _beaconWalletClient.PermissionInfoRepository.TryReadBySenderIdAsync(message.SenderId);

                    // var signatureRequestViewModel = new SignatureRequestViewModel()
                    // {
                    //     DappName = permissionInfo!.AppMetadata.Name,
                    //     Payload = signRequest.Payload,
                    //     OnSign = async () =>
                    //     {
                    //         var hdWallet = _app.Account.Wallet as HdWallet;
                    //
                    //         using var privateKey = hdWallet!.KeyStorage.GetPrivateKey(
                    //             currency: Tezos,
                    //             keyIndex: connectedWalletAddress.KeyIndex,
                    //             keyType: connectedWalletAddress.KeyType);
                    //
                    //         var signedMessage = TezosSigner.SignHash(
                    //             data: dataToSign,
                    //             privateKey: privateKey.ToUnsecuredBytes(),
                    //             watermark: null,
                    //             isExtendedKey: privateKey.Length == 64);
                    //
                    //         var response = new SignPayloadResponse(
                    //             signature: signedMessage.EncodedSignature,
                    //             version: signRequest.Version,
                    //             id: signRequest.Id,
                    //             senderId: BeaconWalletClient.SenderId);
                    //
                    //         await BeaconWalletClient.SendResponseAsync(receiverId: e.SenderId, response);
                    //         App.DialogService.Close();
                    //         Log.Information("{@Sender}: signed payload for {@Dapp} with signature: {@Signature}",
                    //             "Beacon", permissionInfo.AppMetadata.Name, signedMessage.EncodedSignature);
                    //     },
                    //     OnReject = async () =>
                    //     {
                    //         await BeaconWalletClient.SendResponseAsync(receiverId: e.SenderId,
                    //             new BeaconAbortedError(signRequest.Id, BeaconWalletClient.SenderId));
                    //         App.DialogService.Close();
                    //         Log.Information("{@Sender}: user Aborted signing payload from {@Dapp}", "Beacon",
                    //             permissionInfo.AppMetadata.Name);
                    //     }
                    // };

                    //await Dispatcher.UIThread.InvokeAsync(() => { App.DialogService.Show(signatureRequestViewModel); });
                    break;
                }
                case BeaconMessageType.broadcast_request:
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

        private async Task<Result<(TezosTransaction tx, bool isSuccess, bool isRunSuccess)>> RunAutofillOperation(
            string fromAddress,
            string toAddress,
            long amount,
            JObject operationParams,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var tx = new TezosTransaction
                {
                    From = fromAddress,
                    To = toAddress,
                    Fee = 0,
                    GasLimit = 1_000_000,
                    StorageLimit = 5000,
                    Amount = amount,
                    Currency = Tezos.Name,
                    CreationTime = DateTime.UtcNow,
                    Params = operationParams,

                    UseRun = true,
                    UseSafeStorageLimit = false,
                    UseOfflineCounter = false
                };

                var walletAddress = _app.Account
                    .GetCurrencyAccount(TezosConfig.Xtz)
                    .GetAddressAsync(fromAddress, cancellationToken)
                    .WaitForResult();

                using var securePublicKey = _app.Account.Wallet.GetPublicKey(
                    currency: Tezos,
                    keyIndex: walletAddress.KeyIndex,
                    keyType: walletAddress.KeyType);

                var (isSuccess, isRunSuccess, _) = await tx.FillOperationsAsync(
                    securePublicKey: securePublicKey,
                    tezosConfig: Tezos,
                    headOffset: TezosConfig.HeadOffset,
                    cancellationToken: cancellationToken);

                return (tx, isSuccess, isRunSuccess);
            }
            catch (Exception e)
            {
                Log.Error(e, "Autofill transaction error");
                return new Error(Errors.TransactionCreationError, "Autofill transaction error. Try again later.");
            }
        }

        private void OnAtomexClientChangedEventHandler(object sender, AtomexClientChangedEventArgs args)
        {
            if (args.AtomexClient != null && _app.Account != null) return;

            _beaconWalletClient.Disconnect();
            _app.AtomexClientChanged -= OnAtomexClientChangedEventHandler;
            _beaconWalletClient.OnBeaconMessageReceived -= OnBeaconWalletClientMessageReceived;
            _beaconWalletClient.OnDappsListChanged -= OnDappsListChanged;
        }
        
        private void OnDappsListChanged(object? sender, DappConnectedEventArgs e)
        {
            GetAllDapps();
            Log.Information("{@Sender}: connected dapp: {@Dapp}", "Beacon", e?.dappMetadata.Name);
        }
        
         private void GetAllDapps()
        {
            var peers = _beaconWalletClient.GetAllPeers();
            //
            // Dapps = new ObservableCollection<DappViewModel>(
            //     peers.Select(peer => new DappViewModel
            //     {
            //         Peer = peer,
            //         PermissionInfo = _beaconWalletClient.PermissionInfoRepository
            //             .TryReadBySenderIdAsync(peer.SenderId).Result,
            //         OnDisconnect = disconnectPeer =>
            //         {
            //             var disconnectViewModel = new DisconnectViewModel
            //             {
            //                 DappName = disconnectPeer.Name,
            //                 OnDisconnect = async () => await BeaconWalletClient.RemovePeerAsync(disconnectPeer.SenderId)
            //             };
            //
            //             App.DialogService.Show(disconnectViewModel);
            //         },
            //         OnDappClick = clickedPeer =>
            //         {
            //             var permissionInfo = BeaconWalletClient.PermissionInfoRepository
            //                 .TryReadBySenderIdAsync(clickedPeer.SenderId).Result;
            //             if (permissionInfo == null) return;
            //
            //             var showDappViewModel = new ShowDappViewModel
            //             {
            //                 DappName = clickedPeer.Name,
            //                 DappId = clickedPeer.SenderId,
            //                 Address = clickedPeer.ConnectedAddress,
            //                 Permissions = permissionInfo.Scopes,
            //                 OnDisconnect = () =>
            //                 {
            //                     var disconnectViewModel = new DisconnectViewModel
            //                     {
            //                         DappName = clickedPeer.Name,
            //                         OnDisconnect = async () =>
            //                             await BeaconWalletClient.RemovePeerAsync(clickedPeer.SenderId)
            //                     };
            //
            //                     App.DialogService.Show(disconnectViewModel);
            //                 }
            //             };
            //
            //             App.DialogService.Show(showDappViewModel);
            //         }
            //     }));
        }
    }
}