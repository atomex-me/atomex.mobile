using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Input;
using Atomex;
using Atomex.Core;
using atomex.Resources;
using Xamarin.Forms;
using Newtonsoft.Json;
using System.Text;
using Xamarin.Essentials;
using ZXing;
using System.Collections.Generic;
using System.Linq;
using Netezos.Encoding;
using Beacon.Sdk;
using Beacon.Sdk.Beacon;
using atomex.Views.WalletBeacon;
using atomex.Helpers;
using Sodium;
using Beacon.Sdk.Core.Domain.Entities;
using System.Globalization;
using Serilog;
using Beacon.Sdk.Beacon.Operation;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Beacon.Sdk.Core.Domain;

namespace atomex.ViewModel.WalletBeacon
{
    public class DappsViewModel : BaseViewModel
    {
        private readonly IAtomexApp _app;

        private readonly IWalletBeaconClient _walletBeaconClient;

        public DappsViewModel(
            IAtomexApp app,
            IWalletBeaconClient walletBeaconClient,
            INavigation navigation)
        {
            _app = app ?? throw new ArgumentNullException(nameof(app));
            _walletBeaconClient = walletBeaconClient ?? throw new ArgumentNullException(nameof(walletBeaconClient));
            Navigation = navigation ?? throw new ArgumentNullException(nameof(navigation));

            DeleteCommand = new Command<string>(async (name) => await DeleteAsync(name));
            ScanQrCodeCommand = new Command(async () => await ScanQrCodeAsync());
            ScanResultCommand = new Command(async () => await OnScanAddressAsync());

            _walletBeaconClient.OnDappConnected += OnDappConnectedEventHandler;
            _walletBeaconClient.OnBeaconMessageReceived += OnBeaconMessageRecievedHandler;

            var permissions = _walletBeaconClient.PermissionInfoRepository.ReadAllAsync().Result;
            foreach (var permission in permissions)
                Permissions.Add(permission);
        }

        [Reactive] public bool IsScanning { get; set; }

        [Reactive] public bool IsAnalyzing { get; set; }

        public Result ScanResult { get; set; }

        public INavigation Navigation { get; }

        public ObservableCollection<PermissionInfo> Permissions { get; } = new();

        public ICommand DeleteCommand { get; }

        public ICommand ScanQrCodeCommand { get; }

        public ICommand ScanResultCommand { get; }

        private void OnDappConnectedEventHandler(object sender, DappConnectedEventArgs e)
        {
            if (sender is not ResponseMessageHandler)
                throw new ArgumentException("sender is not ResponseMessageHandler");

            Permissions.Add(e.dappPermissionInfo);
        }

        private void OnBeaconMessageRecievedHandler(object sender, BeaconMessageEventArgs args)
        {
            BaseBeaconMessage message = args.Request;

            if (message.Type != BeaconMessageType.operation_request)
                return;

            var request = message as OperationRequest;

            if (request!.OperationDetails.Count <= 0)
                return;

            var transactionOperation = request.OperationDetails[0];

            if (!long.TryParse(transactionOperation.Amount, out long amount))
                return;

            Device.BeginInvokeOnMainThread(async () =>
            {
                await Navigation.PushAsync(
                    new TezosTransactionRequestPage(
                        new TezosTransactionRequestViewModel(
                            _app,
                            _walletBeaconClient,
                            args.SenderId,
                            Navigation,
                            request,
                            transactionOperation)));
            });
        }

        private async Task DeleteAsync(string address)
        {
            var selectedDapp = Permissions.FirstOrDefault(w => w.Address == address);

            var confirm = await Application.Current.MainPage.DisplayAlert("Delete",
                string.Format(CultureInfo.InvariantCulture, "Are you sure?", selectedDapp?.Address),
                AppResources.DeleteButton, AppResources.CancelButton);

            if (confirm)
            {

                try
                {
                    await _walletBeaconClient.PermissionInfoRepository.DeleteByAddressAsync(address);

                    var index = Permissions.IndexOf(selectedDapp);

                    Permissions.RemoveAt(index);//.Remove(selectedDapp);
                }
                catch (Exception e)
                {
                    Log.Error(e, "Delete dapp error");
                }
            }
        }

        private async Task ScanQrCodeAsync()
        {
            PermissionStatus permissions = await Xamarin.Essentials.Permissions.CheckStatusAsync<Permissions.Camera>();

            if (permissions != PermissionStatus.Granted)
                permissions = await Xamarin.Essentials.Permissions.RequestAsync<Permissions.Camera>();
            if (permissions != PermissionStatus.Granted)
                return;

            IsScanning = true;
            IsAnalyzing = true;

            this.RaisePropertyChanged(nameof(IsScanning));
            this.RaisePropertyChanged(nameof(IsAnalyzing));

            await Navigation.PushAsync(new ScanningQrPage(this));
        }

        private async Task OnScanAddressAsync()
        {
            IsScanning = false;
            IsAnalyzing = false;

            this.RaisePropertyChanged(nameof(IsScanning));
            this.RaisePropertyChanged(nameof(IsAnalyzing));

            if (ScanResult == null)
            {
                await Application.Current.MainPage.DisplayAlert(AppResources.Error, "Incorrect QR code format", AppResources.AcceptButton);
                Device.BeginInvokeOnMainThread(async () =>
                {
                    await Navigation.PopAsync();
                });

                return;
            }

            try
            {
                string search = "data=";
                string data = ScanResult.Text.Substring(ScanResult.Text.IndexOf(search) + search.Length);

                byte[] decodedBytes = Base58.Parse(data);

                string message = Encoding.Default.GetString(decodedBytes);
                var pairingRequest = JsonConvert.DeserializeObject<P2PPairingRequest>(message);

                Device.BeginInvokeOnMainThread(async () =>
                {
                    var confirmDappPage = new PairingRequestPage(new PairingRequestViewModel(_app, _walletBeaconClient, Navigation, pairingRequest));
                    await Navigation.PushAsync(confirmDappPage);
                });
            }
            catch (Exception ex)
            {
                Device.BeginInvokeOnMainThread(async () =>
                {
                    await Application.Current.MainPage.DisplayAlert(AppResources.Error, "Incorrect QR code format", AppResources.AcceptButton);
                });
            }
        }
    }
}

//try
//{
//    Console.WriteLine($"1 walletBeaconClient.LoggedIn {walletBeaconClient.LoggedIn}");
//    //Task.Run(async () =>
//    //{
//        Console.WriteLine($"2 walletBeaconClient.LoggedIn {walletBeaconClient.LoggedIn}");
//        _= walletBeaconClient.InitAsync();
//        Console.WriteLine($"3 walletBeaconClient.LoggedIn {walletBeaconClient.LoggedIn}");
//    //});

//} catch(Exception ex)
//{

//}

//try
//{
//    var factory = new MatrixClientFactory();
//    IMatrixClient client = factory.Create();

//    (Uri matrixNodeAddress, string username, string password, string deviceId) = CreateLoginRequest();

//    client.LoginAsync(matrixNodeAddress, username, password, deviceId).Wait();
//    Console.WriteLine($"client.IsLoggedIn {client.IsLoggedIn}");
//}
//catch(Exception ex)
//{

//}

//public class DappInfo
//{
//    public int Id { get; set; }
//    public string Name { get; set; }
//    public string ImageUrl { get; set; }
//    public Network Network { get; set; }
//    public bool IsActive { get; set; }
//    public DappType DappDeviceType { get; set; }
//    public IReadOnlyCollection<Permission> Permissions { get; set; }

//    public string PermissionsFormatted => Permissions != null
//        ? string.Join(", ", Permissions.Select(x => x.Name))
//        : string.Empty;

//    //public static List<DappInfo> MockDapps() =>
//    //    new List<DappInfo>()
//    //    {
//    //        new() { Name = "abcd", Network = Network.TestNet, ImageUrl = "LTC", IsActive = true, DappDeviceType = DappType.Desktop, Permissions = new List<Permission>() { new Permission() {Name="Ты можешь читать"}, new Permission() {Name="Ты можешь писать"}, new Permission() {Name="Ты не можешь отсосать"}}},
//    //        new() { Name = "xyz", Network = Network.MainNet, ImageUrl = "BTC", IsActive = true, DappDeviceType = DappType.Mobile},
//    //        new() { Name = "Desktop", Network = Network.MainNet, ImageUrl = "ETH", IsActive = true, DappDeviceType = DappType.Web},
//    //        new() { Name = "xyz5", Network = Network.MainNet, ImageUrl = "LTC", IsActive = true, DappDeviceType = DappType.Mobile},
//    //        new() { Name = "xyz4", Network = Network.MainNet, ImageUrl = "LTC", IsActive = true, DappDeviceType = DappType.Mobile},
//    //        new() { Name = "xyz3", Network = Network.MainNet, ImageUrl = "LTC", IsActive = true, DappDeviceType = DappType.Mobile},
//    //        new() { Name = "xyz2", Network = Network.MainNet, ImageUrl = "LTC", IsActive = true, DappDeviceType = DappType.Mobile},
//    //        new() { Name = "xyz43", Network = Network.MainNet,ImageUrl = "LTC", IsActive = true, DappDeviceType = DappType.Mobile},
//    //    };
//}

//public enum DappType
//{
//    Mobile,
//    Desktop,
//    Web
//}

//public class Permission
//{
//    public string Name { get; set; }
//    public bool IsActive { get; set; }
//}