using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Atomex;
using Atomex.Blockchain.Tezos;
using Atomex.Blockchain.Tezos.Internal;
using Atomex.Client.Common;
using atomex.Common;
using Atomex.Common;
using atomex.Models;
using atomex.Resources;
using Atomex.ViewModels;
using atomex.Views;
using atomex.Views.Dapps;
using Atomex.Wallet;
using Atomex.Wallet.Tezos;
using Atomex.Wallets.Tezos;
using Beacon.Sdk;
using Beacon.Sdk.Beacon;
using Beacon.Sdk.Beacon.Error;
using Beacon.Sdk.Beacon.Operation;
using Beacon.Sdk.Beacon.Permission;
using Beacon.Sdk.Beacon.Sign;
using Beacon.Sdk.BeaconClients;
using Beacon.Sdk.BeaconClients.Abstract;
using Beacon.Sdk.Core.Domain.Entities;
using Netezos.Encoding;
using Netezos.Forging.Models;
using Netezos.Keys;
using Newtonsoft.Json.Linq;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Serilog;
using Serilog.Extensions.Logging;
using Xamarin.Essentials;
using Xamarin.Forms;
using Hex = Atomex.Common.Hex;

namespace atomex.ViewModels.DappsViewModels
{
    public class DappViewModel : BaseViewModel
    {
        public PermissionInfo PermissionInfo { get; set; }
        public string Id => PermissionInfo?.SenderId;
        public string Name => PermissionInfo?.AppMetadata.Name;
        public string ConnectedAddress => PermissionInfo?.Address;
        public string Logo => PermissionInfo?.AppMetadata.Icon;
        public List<PermissionScope> Permissions => PermissionInfo?.Scopes ?? new List<PermissionScope>();
        public List<string> PermissionStrings => BeaconHelper.GetPermissionStrings(Permissions);
        public Action<PermissionInfo> OnDisconnect { get; set; }
        public Action<string> OnCopy { get; set; }
        public Action<DappViewModel> ShowDisconnection { get; set; }
        public Action CloseDisconnection { get; set; }

        public string DappInfoTitle => string.Format(AppResources.DappIsConnected, Name);
        public string DisconnectTitle => string.Format(AppResources.DisconnectDappTitle, Name);
        public string DisconnectConfirmationText => string.Format(AppResources.AreYouSureToDisconnect, Name);

        private ReactiveCommand<Unit, Unit> _openDappSiteCommand;

        public ReactiveCommand<Unit, Unit> OpenDappSiteCommand =>
            _openDappSiteCommand ??= ReactiveCommand.Create(() =>
            {
                if (PermissionInfo?.AppMetadata?.AppUrl != null)
                    Launcher.OpenAsync(new Uri(PermissionInfo.AppMetadata.AppUrl));
            });

        private ReactiveCommand<DappViewModel, Unit> _showDisconnectionCommand;

        public ReactiveCommand<DappViewModel, Unit> ShowDisconnectionCommand =>
            _showDisconnectionCommand ??=
                ReactiveCommand.Create<DappViewModel>(dappViewModel => ShowDisconnection?.Invoke(dappViewModel));

        private ReactiveCommand<Unit, Unit> _closeDisconnectionCommand;

        public ReactiveCommand<Unit, Unit> CloseDisconnectionCommand =>
            _closeDisconnectionCommand ??= ReactiveCommand.Create(() => CloseDisconnection?.Invoke());

        private ReactiveCommand<Unit, Unit> _disconnectCommand;

        public ReactiveCommand<Unit, Unit> DisconnectCommand =>
            _disconnectCommand ??= ReactiveCommand.Create(() => OnDisconnect?.Invoke(PermissionInfo));

        private ReactiveCommand<string, Unit> _copyCommand;

        public ReactiveCommand<string, Unit> CopyCommand =>
            _copyCommand ??= ReactiveCommand.Create<string>(value => OnCopy?.Invoke(value));
    }

    public class DappsViewModel : BaseViewModel
    {
        private readonly IAtomexApp _app;
        private INavigationService _navigationService;
        private IWalletBeaconClient _beaconWalletClient;

        [Reactive] public ObservableCollection<DappViewModel> Dapps { get; set; }
        [Reactive] public DappViewModel SelectedDapp { get; set; }
        private TezosConfig _tezos { get; }
        public SelectAddressViewModel SelectAddressViewModel { get; set; }
        public WalletAddressViewModel AddressToConnect { get; set; }
        public ConnectDappViewModel ConnectDappViewModel { get; set; }

        public const double DefaultDappRowHeight = 72;
        public const double DappListHeaderHeight = 52;
        public const double DappListFooterHeight = 82;
        [Reactive] public double DappListViewHeight { get; set; }

        public DappsViewModel(
            IAtomexApp app,
            INavigationService navigationService)
        {
            _app = app ?? throw new ArgumentNullException(nameof(app));
            _navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));
            _app.AtomexClientChanged += OnAtomexClientChangedEventHandler;
            _tezos = (TezosConfig) _app.Account.Currencies.GetByName(TezosConfig.Xtz);

            this.WhenAnyValue(vm => vm.Dapps)
                .WhereNotNull()
                .SubscribeInMainThread(vm =>
                    DappListViewHeight = (Dapps!.Count + 1) * DefaultDappRowHeight +
                                         DappListFooterHeight + DappListHeaderHeight);

            this.WhenAnyValue(vm => vm.SelectedDapp)
                .WhereNotNull()
                .SubscribeInMainThread(async (dapp) =>
                {
                    _navigationService?.ShowPage(new DappInfoPage(dapp), TabNavigation.Portfolio);
                    SelectedDapp = null;
                });

            ConnectDappViewModel = new ConnectDappViewModel(_navigationService)
            {
                OnConnect = Connect
            };

            _ = Task.Run(async () =>
            {
                if (_app.Account == null) return;

                try
                {
                    var pathToWalletFolder = new DirectoryInfo(_app.Account.Wallet.PathToWallet).Parent?.FullName;
                    var path = Path.Combine(pathToWalletFolder!, "beacon.db");

                    var options = new BeaconOptions
                    {
                        AppName = "Atomex mobile",
                        AppUrl = "https://atomex.me",
                        IconUrl = "https://bcd-static-assets.fra1.digitaloceanspaces.com/dapps/atomex/atomex_logo.jpg",
                        KnownRelayServers = new[]
                        {
                            "beacon-node-1.diamond.papers.tech",
                            "beacon-node-1.sky.papers.tech",
                            "beacon-node-2.sky.papers.tech",
                            "beacon-node-1.hope.papers.tech",
                            "beacon-node-1.hope-2.papers.tech",
                            "beacon-node-1.hope-3.papers.tech",
                            "beacon-node-1.hope-4.papers.tech",
                        },
                        DatabaseConnectionString = $"Filename={path}"
                    };

                    _beaconWalletClient = BeaconClientFactory
                        .Create<IWalletBeaconClient>(
                            options,
                            new SerilogLoggerProvider(Log.Logger));

                    _beaconWalletClient.OnBeaconMessageReceived += OnBeaconWalletClientMessageReceived;
                    _beaconWalletClient.OnConnectedClientsListChanged += OnDappsListChanged;
                    await _beaconWalletClient.InitAsync();
                    _beaconWalletClient.Connect();

                    GetAllDapps();

                    Log.Debug("{@Sender}: WalletClient connected {@Connected}", "Beacon",
                        _beaconWalletClient.Connected);
                    Log.Debug("{@Sender}: WalletClient logged in {@LoggedIn}", "Beacon", _beaconWalletClient.LoggedIn);
                }
                catch (Exception e)
                {
                    Log.Error(e, "Connect beacon client error");
                }
            });
        }

        public async Task Connect(string qrCodeString)
        {
            if (_app.Account == null) return;

            try
            {
                var pairingRequest = _beaconWalletClient.GetPairingRequest(qrCodeString);
                await _beaconWalletClient.AddPeerAsync(pairingRequest);

                Device.BeginInvokeOnMainThread(() => _navigationService?.DisplaySnackBar(
                    SnackbarMessage.MessageType.Regular,
                    AppResources.ConnectedSuccessfully));
            }
            catch (Exception e)
            {
                Device.BeginInvokeOnMainThread(() =>
                {
                    _navigationService?.ClosePage(TabNavigation.Portfolio);
                    _navigationService?.DisplaySnackBar(SnackbarMessage.MessageType.Error,
                        AppResources.ConnectionError);
                });
                Log.Error(e, "Connect dApp error");
            }
        }

        private ReactiveCommand<Unit, Unit> _newDappCommand;

        public ReactiveCommand<Unit, Unit> NewDappCommand => _newDappCommand ??= ReactiveCommand.Create(() =>
        {
            SelectAddressViewModel =
                new SelectAddressViewModel(
                    account: _app.Account,
                    currency: _tezos,
                    navigationService: _navigationService,
                    mode: SelectAddressMode.Connect)
                {
                    ConfirmAction = async (selectAddressViewModel, walletAddressViewModel) =>
                    {
                        AddressToConnect = walletAddressViewModel;
                        ConnectDappViewModel!.AddressToConnect = AddressToConnect.Address;
                        selectAddressViewModel.SelectedAddress = null;
                        PermissionStatus permissions = await Permissions.CheckStatusAsync<Permissions.Camera>();

                        if (permissions != PermissionStatus.Granted)
                            permissions = await Permissions.RequestAsync<Permissions.Camera>();
                        if (permissions != PermissionStatus.Granted)
                        {
                            _navigationService?.ClosePage(TabNavigation.Portfolio);
                            return;
                        }

                        ConnectDappViewModel!.IsScanning = true;
                        ConnectDappViewModel!.IsAnalyzing = true;
                        _navigationService?.ShowPage(new ScanningQrPage(ConnectDappViewModel), TabNavigation.Portfolio);
                    }
                };

            _navigationService?.ShowPage(new SelectAddressPage(SelectAddressViewModel), TabNavigation.Portfolio);
        });

        private async void OnBeaconWalletClientMessageReceived(object sender, BeaconMessageEventArgs e)
        {
            var message = e.Request;
            if (message == null) return;
            
            var permissions = _beaconWalletClient
                .PermissionInfoRepository
                .TryReadBySenderIdAsync(message.SenderId)
                .Result;

            Log.Debug("{@Sender}: message with type {@Type}, from {@SenderId} received",
                "Beacon", message.Type, message.SenderId);

            var connectedWalletAddress = await _app
                .Account
                .GetAddressAsync(_tezos.Name, permissions?.Address ?? string.Empty);

            switch (message.Type)
            {
                case BeaconMessageType.permission_request:
                {
                    if (message is not PermissionRequest permissionRequest) return;

                    if (permissionRequest.Network.Type == NetworkType.mainnet &&
                        _app.Account.Network != Atomex.Core.Network.MainNet)
                    {
                        await _beaconWalletClient.SendResponseAsync(
                            receiverId: message.SenderId,
                            response: new NetworkNotSupportedBeaconError(permissionRequest.Id,
                                _beaconWalletClient.SenderId));
                        return;
                    }

                    var addressToConnect = await _app
                        .Account
                        .GetAddressAsync(_tezos.Name, ConnectDappViewModel.AddressToConnect);

                    var response = new PermissionResponse(
                        id: permissionRequest.Id,
                        senderId: _beaconWalletClient.SenderId,
                        appMetadata: _beaconWalletClient.Metadata,
                        network: permissionRequest.Network,
                        scopes: permissionRequest.Scopes,
                        publicKey: PubKey.FromBase64(addressToConnect.PublicKey).ToString(),
                        version: permissionRequest.Version);

                    
                    var permissionRequestViewModel = new PermissionRequestViewModel
                    {
                        DappName = permissionRequest.AppMetadata.Name,
                        DappLogo = permissionRequest.AppMetadata.Icon,
                        Address = addressToConnect.Address,
                        Balance = addressToConnect.Balance,
                        Permissions = permissionRequest.Scopes,
                        OnReject = async () =>
                        {
                            await _beaconWalletClient.SendResponseAsync(
                                receiverId: message.SenderId,
                                response: new BeaconAbortedError(permissionRequest.Id, _beaconWalletClient.SenderId));
                            await _beaconWalletClient.RemovePeerAsync(message.SenderId);

                            _navigationService?.CloseBottomSheet();
                            _navigationService?.ReturnToInitiatedPage(TabNavigation.Portfolio);

                            Log.Information(
                                "{@Sender}: Rejected permissions [{@PermissionsList}] to dapp {@Dapp} with address {@Address}",
                                "Beacon",
                                permissionRequest.Scopes.Aggregate(string.Empty,
                                    (res, scope) => res + $"{scope.ToString()}, "),
                                permissionRequest.AppMetadata.Name, addressToConnect.Address);
                        },
                        OnAllow = async () =>
                        {
                            await _beaconWalletClient.SendResponseAsync(
                                receiverId: message.SenderId,
                                response: response);

                            _navigationService?.CloseBottomSheet();
                            _navigationService?.ReturnToInitiatedPage(TabNavigation.Portfolio);

                            Log.Information(
                                "{@Sender}: Issued permissions [{@PermissionsList}] to dapp {@Dapp} with address {@Address}",
                                "Beacon",
                                permissionRequest.Scopes.Aggregate(string.Empty,
                                    (res, scope) => res + $"{scope.ToString()}, "),
                                permissionRequest.AppMetadata.Name, addressToConnect.Address);
                        }
                    };

                    await Device.InvokeOnMainThreadAsync(() =>
                    {
                        _navigationService?.ShowBottomSheet(new PermissionRequestPopup(permissionRequestViewModel));
                    });
                    break;
                }
                case BeaconMessageType.operation_request:
                {
                    if (message is not OperationRequest operationRequest)
                        return;

                    var permissionInfo = _beaconWalletClient
                        .PermissionInfoRepository
                        .TryReadBySenderIdAsync(operationRequest.SenderId)
                        .Result;

                    if (permissionInfo == null)
                    {
                        await _beaconWalletClient.SendResponseAsync(
                            receiverId: message.SenderId,
                            response: new BeaconAbortedError(operationRequest.Id, _beaconWalletClient.SenderId));

                        return;
                    }

                    var rpc = new Rpc(_tezos.RpcNodeUri);

                    var head = await rpc
                        .GetHeader()
                        .ConfigureAwait(false);

                    var managerKey = await rpc
                        .GetManagerKey(connectedWalletAddress.Address)
                        .ConfigureAwait(false);

                    var revealed = managerKey.Value<string>() != null;

                    var account = await rpc
                        .GetAccountForBlock(head["hash"]!.ToString(), connectedWalletAddress.Address)
                        .ConfigureAwait(false);

                    var counter = int.Parse(account["counter"]!.ToString());

                    var operations = new List<ManagerOperationContent>();

                    if (!revealed)
                    {
                        operations.Add(new RevealContent
                        {
                            Counter = ++counter,
                            Fee = 0,
                            GasLimit = 1_000_000,
                            Source = connectedWalletAddress.Address,
                            PublicKey = PubKey.FromBase64(connectedWalletAddress.PublicKey).ToString(),
                            StorageLimit = 0
                        });
                    }

                    operations.AddRange(operationRequest.OperationDetails.Select(o =>
                    {
                        if (!long.TryParse(o.Amount, out var amount))
                            amount = 0;

                        var txContent = new TransactionContent
                        {
                            Source = connectedWalletAddress.Address,
                            Destination = o.Destination,
                            Amount = amount,
                            Counter = ++counter,
                            Fee = 0,
                            GasLimit = 500_000,
                            StorageLimit = 5000
                        };

                        if (o.Parameters == null) return txContent;

                        try
                        {
                            txContent.Parameters = new Parameters
                            {
                                Entrypoint = o.Parameters?["entrypoint"]!.ToString(),
                                Value = Micheline.FromJson(o.Parameters?["value"]!.ToString())
                            };
                        }
                        catch (Exception ex)
                        {
                            Log.Error(ex, "Exception during parsing Beacon operation params");
                        }

                        return txContent;
                    }));

                    var error = await TezosOperationFiller
                        .AutoFillAsync(
                            operations,
                            head["hash"]!.ToString(),
                            head["chain_id"]!.ToString(),
                            _tezos)
                        .ConfigureAwait(false);

                    if (error != null)
                    {
                        await _beaconWalletClient.SendResponseAsync(
                            receiverId: message.SenderId,
                            response: new TransactionInvalidBeaconError(operationRequest.Id,
                                _beaconWalletClient.SenderId));

                        Log.Error("{@Sender}: error during AutoFill transaction, {@Msg}", "Beacon", error.Description);
                        return;
                    }

                    var forgedOperations = await TezosForge.ForgeAsync(
                        operations: operations,
                        branch: head["hash"]!.ToString());

                    var operationsViewModel = new ObservableCollection<BaseBeaconOperationViewModel>();

                    foreach (var item in operations.Select((value, idx) => new {idx, value}))
                    {
                        var operation = item.value;
                        var index = item.idx;

                        switch (operation)
                        {
                            case TransactionContent transactionOperation:
                                operationsViewModel.Add(new TransactionContentViewModel
                                {
                                    Id = index + 1,
                                    Operation = transactionOperation,
                                    QuotesProvider = _app.QuotesProvider,
                                });
                                break;
                            case RevealContent revealOperation:
                                operationsViewModel.Add(new RevealContentViewModel
                                {
                                    Id = index + 1,
                                    Operation = revealOperation,
                                    QuotesProvider = _app.QuotesProvider,
                                });
                                break;
                        }
                    }

                    var operationRequestViewModel = new OperationRequestViewModel
                    {
                        DappName = permissionInfo.AppMetadata.Name,
                        DappLogo = permissionInfo.AppMetadata.Icon,
                        Operations = operationsViewModel,
                        OnReject = async () =>
                        {
                            await _beaconWalletClient.SendResponseAsync(
                                receiverId: message.SenderId,
                                response: new BeaconAbortedError(operationRequest.Id, _beaconWalletClient.SenderId));

                            _navigationService?.CloseBottomSheet();
                        },

                        OnConfirm = async () =>
                        {
                            var wallet = (HdWallet) _app.Account.Wallet;
                            var keyStorage = wallet.KeyStorage;

                            using var securePrivateKey = keyStorage.GetPrivateKey(
                                currency: _tezos,
                                keyIndex: connectedWalletAddress.KeyIndex,
                                keyType: connectedWalletAddress.KeyType);

                            var privateKey = securePrivateKey.ToUnsecuredBytes();

                            var signedMessage = TezosSigner.SignHash(
                                data: forgedOperations,
                                privateKey: privateKey,
                                watermark: Watermark.Generic,
                                isExtendedKey: privateKey.Length == 64);

                            if (signedMessage == null)
                            {
                                Log.Error("Beacon transaction signing error");

                                await _beaconWalletClient.SendResponseAsync(
                                    receiverId: message.SenderId,
                                    response: new TransactionInvalidBeaconError(operationRequest.Id,
                                        _beaconWalletClient.SenderId));

                                return;
                            }

                            string operationId = null;

                            try
                            {
                                var injectedOperation = await rpc
                                    .InjectOperations(signedMessage.SignedBytes)
                                    .ConfigureAwait(false);

                                operationId = injectedOperation.ToString();
                            }
                            catch (Exception ex)
                            {
                                Log.Error("Beacon transaction broadcast error {@Description}", ex.Message);

                                await _beaconWalletClient.SendResponseAsync(
                                    receiverId: message.SenderId,
                                    response: new BroadcastBeaconError(operationRequest.Id,
                                        _beaconWalletClient.SenderId));

                                return;
                            }

                            var response = new OperationResponse(
                                id: operationRequest.Id,
                                senderId: _beaconWalletClient.SenderId,
                                transactionHash: operationId,
                                operationRequest.Version);
                            await _beaconWalletClient.SendResponseAsync(
                                receiverId: message.SenderId,
                                response);

                            _navigationService?.CloseBottomSheet();

                            Log.Information("{@Sender}: operation done with transaction hash: {@Hash}",
                                "Beacon",
                                operationId);
                        }
                    };

                    await Device.InvokeOnMainThreadAsync(() =>
                    {
                        _navigationService?.ShowBottomSheet(new OperationRequestPopup(operationRequestViewModel));
                    });
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
                        await _beaconWalletClient
                            .PermissionInfoRepository
                            .TryReadBySenderIdAsync(message.SenderId);

                    var signatureRequestViewModel = new SignatureRequestViewModel()
                    {
                        DappName = permissionInfo!.AppMetadata.Name,
                        DappLogo = permissionInfo!.AppMetadata.Icon,
                        Payload = signRequest.Payload,
                        OnSign = async () =>
                        {
                            var hdWallet = _app.Account.Wallet as HdWallet;

                            using var privateKey = hdWallet!.KeyStorage.GetPrivateKey(
                                currency: _tezos,
                                keyIndex: connectedWalletAddress.KeyIndex,
                                keyType: connectedWalletAddress.KeyType);

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

                            await _beaconWalletClient.SendResponseAsync(
                                receiverId: message.SenderId,
                                response: response);

                            _navigationService?.CloseBottomSheet();

                            Log.Information("{@Sender}: signed payload for {@Dapp} with signature: {@Signature}",
                                "Beacon", permissionInfo.AppMetadata.Name, signedMessage.EncodedSignature);
                        },
                        OnReject = async () =>
                        {
                            await _beaconWalletClient.SendResponseAsync(
                                receiverId: message.SenderId,
                                response: new BeaconAbortedError(signRequest.Id, _beaconWalletClient.SenderId));

                            _navigationService?.CloseBottomSheet();

                            Log.Information("{@Sender}: user Aborted signing payload from {@Dapp}", "Beacon",
                                permissionInfo.AppMetadata.Name);
                        }
                    };

                    await Device.InvokeOnMainThreadAsync(() =>
                    {
                        _navigationService?.ShowBottomSheet(new SignatureRequestPopup(signatureRequestViewModel));
                    });
                    break;
                }
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
                    Currency = _tezos.Name,
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
                    currency: _tezos,
                    keyIndex: walletAddress.KeyIndex,
                    keyType: walletAddress.KeyType);

                var (isSuccess, isRunSuccess, _) = await tx.FillOperationsAsync(
                    securePublicKey: securePublicKey,
                    tezosConfig: _tezos,
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
            if (_app.Account != null && _app.AtomexClient != null) return;

            _app.AtomexClientChanged -= OnAtomexClientChangedEventHandler;
            
            if (_beaconWalletClient == null) return;
            
            _beaconWalletClient.Disconnect();
            _beaconWalletClient.OnBeaconMessageReceived -= OnBeaconWalletClientMessageReceived;
            _beaconWalletClient.OnConnectedClientsListChanged -= OnDappsListChanged;
        }

        private void OnDappsListChanged(object sender, ConnectedClientsListChangedEventArgs e)
        {
            GetAllDapps();
            Log.Information("{@Sender}: connected dapp: {@Dapp}", "Beacon", e?.Metadata.Name);
        }

        private void GetAllDapps()
        {
            var permissionInfoList = _beaconWalletClient
                .PermissionInfoRepository
                .ReadAllAsync()
                .Result;

            Dapps = new ObservableCollection<DappViewModel>(permissionInfoList
                .Select(permissionInfo => new DappViewModel
                {
                    PermissionInfo = permissionInfo,
                    ShowDisconnection = connectedDapp =>
                        _navigationService?.ShowBottomSheet(new DappDisconnectPopup(connectedDapp)),
                    CloseDisconnection = () => _navigationService?.CloseBottomSheet(),
                    OnDisconnect = async disconnectPeer =>
                    {
                        _ = Device.InvokeOnMainThreadAsync(() =>
                        {
                            _navigationService?.ReturnToInitiatedPage(TabNavigation.Portfolio);
                            _navigationService?.CloseBottomSheet();
                            _navigationService?.DisplaySnackBar(SnackbarMessage.MessageType.Regular,
                                AppResources.Disconnecting + "...");
                        });
                        await _beaconWalletClient.RemovePeerAsync(disconnectPeer.SenderId);
                        await Device.InvokeOnMainThreadAsync(() =>
                        {
                            _navigationService?.DisplaySnackBar(SnackbarMessage.MessageType.Regular,
                                AppResources.DisconnectedSuccessfully);
                        });
                    },
                    OnCopy = value =>
                    {
                        _ = Device.InvokeOnMainThreadAsync(() =>
                        {
                            if (value != null)
                            {
                                _ = Clipboard.SetTextAsync(value);
                                _navigationService?.DisplaySnackBar(SnackbarMessage.MessageType.Regular,
                                    AppResources.Copied);
                            }
                            else
                            {
                                _navigationService?.ShowAlert(AppResources.Error, AppResources.CopyError,
                                    AppResources.AcceptButton);
                            }
                        });
                    }
                }));
        }
    }
}