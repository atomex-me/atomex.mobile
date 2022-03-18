using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Atomex;
using Atomex.Core;
using atomex.Resources;
using Serilog;
using Xamarin.Forms;
using Newtonsoft.Json;
using System.Text;
using Xamarin.Essentials;
using ZXing;
using System.Collections.Generic;
using System.Linq;
using Netezos;
using Netezos.Encoding;
using Beacon.Sdk;
using Beacon.Sdk.Beacon;
using atomex.Views.WalletBeacon;
using Beacon.Sdk.WalletBeaconClient;
using Serilog.Extensions.Logging;
using atomex.Helpers;

namespace atomex.ViewModel.WalletBeacon
{
    public class DappInfo
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string ImageUrl { get; set; }
        public Network Network { get; set; }
        public bool IsActive { get; set; }
        public DappType DappDeviceType { get; set; }
        public IReadOnlyCollection<Permission> Permissions { get; set; }

        public string PermissionsFormatted => Permissions != null
            ? string.Join(", ", Permissions.Select(x => x.Name))
            : string.Empty;

        public static List<DappInfo> MockDapps() =>
            new List<DappInfo>()
            {
                new() { Name = "abcd", Network = Network.TestNet, ImageUrl = "LTC", IsActive = true, DappDeviceType = DappType.Desktop, Permissions = new List<Permission>() { new Permission() {Name="Ты можешь читать"}, new Permission() {Name="Ты можешь писать"}, new Permission() {Name="Ты не можешь отсосать"}}},
                new() { Name = "xyz", Network = Network.MainNet, ImageUrl = "BTC", IsActive = true, DappDeviceType = DappType.Mobile},
                new() { Name = "Desktop", Network = Network.MainNet, ImageUrl = "ETH", IsActive = true, DappDeviceType = DappType.Web},
                new() { Name = "xyz5", Network = Network.MainNet, ImageUrl = "LTC", IsActive = true, DappDeviceType = DappType.Mobile},
                new() { Name = "xyz4", Network = Network.MainNet, ImageUrl = "LTC", IsActive = true, DappDeviceType = DappType.Mobile},
                new() { Name = "xyz3", Network = Network.MainNet, ImageUrl = "LTC", IsActive = true, DappDeviceType = DappType.Mobile},
                new() { Name = "xyz2", Network = Network.MainNet, ImageUrl = "LTC", IsActive = true, DappDeviceType = DappType.Mobile},
                new() { Name = "xyz43", Network = Network.MainNet,ImageUrl = "LTC", IsActive = true, DappDeviceType = DappType.Mobile},
            };
    }

    public enum DappType
    {
        Mobile,
        Desktop,
        Web
    }

    public class Permission
    {
        public string Name { get; set; }
        public bool IsActive { get; set; }
    }

    public class DappsViewModel : BaseViewModel
    {
        private readonly IWalletBeaconClient walletBeaconClient;

        private readonly IAtomexApp _app;

        public INavigation Navigation { get; set; }

        

        public ObservableCollection<DappInfo> DappsInfo { get; } = new ObservableCollection<DappInfo>();

        public DappsViewModel(IAtomexApp app, INavigation navigation)
        {
            walletBeaconClient = WalletBeaconHelper.WalletBeaconClient;

            _app = app ?? throw new ArgumentNullException(nameof(app)); ;
            Navigation = navigation ?? throw new ArgumentNullException(nameof(navigation));

     

            //app = app.UseWalletBeacon();

            DeleteCommand = new Command<string>(async (name) => await DeleteAsync(name));
            ScanQrCodeCommand = new Command(async () => await ScanQrCodeAsync());
            OnScanAddressCommand = new Command(async () => await OnScanAddressAsync());

            _ = walletBeaconClient.InitAsync();
        }

        public ICommand DeleteCommand { get; } 
        private async Task DeleteAsync(string name)
        {
            await Task.CompletedTask;
            //var selectedDapp = DappsInfo.FirstOrDefault(w => w.Name == name);

            //var confirm = await Application.Current.MainPage.DisplayAlert(AppResources.DeletingDapp,
            //    string.Format(CultureInfo.InvariantCulture, AppResources.DeletingDappConfirmationText, selectedDapp?.Name),
            //    AppResources.DeleteButton, AppResources.CancelButton);
            //if (confirm)
            //{
            //    DeleteDapp(selectedDapp);
            //}

            //    try
            //    {
            //        var index = DappsInfo.IndexOf(dapp);
            //        DappsInfo.Remove(dapp);
            //    }
            //    catch (Exception e)
            //    {
            //        Log.Error(e, "Delete dapp error");
            //    }
        }

        public ICommand ScanQrCodeCommand { get; }
        private async Task ScanQrCodeAsync()
        {
            IsScanning = IsAnalyzing = true;
            PermissionStatus permissions = await Permissions.CheckStatusAsync<Permissions.Camera>();

            if (permissions != PermissionStatus.Granted)
                permissions = await Permissions.RequestAsync<Permissions.Camera>();
            if (permissions != PermissionStatus.Granted)
                return;

            await Navigation.PushAsync(new ScanningQrPage(this));

        }

        public Result ScanResult { get; set; }
 
        public ICommand OnScanAddressCommand { get; }

        private async Task OnScanAddressAsync()
        {
            IsScanning = IsAnalyzing = false;
           
            if (ScanResult == null)
            {
                await Application.Current.MainPage.DisplayAlert(AppResources.Error, "Incorrect QR code format", AppResources.AcceptButton);
                await Navigation.PopAsync();
                return;
            }

            Device.BeginInvokeOnMainThread(async () =>
            {
                await Navigation.PopAsync();
            });

            try
            {
                string search = "data=";
                string data = ScanResult.Text.Substring(ScanResult.Text.IndexOf(search) + search.Length);

                byte[] decodedBytes = Base58.Parse(data);

                string message = Encoding.Default.GetString(decodedBytes);
                var pairingRequest = JsonConvert.DeserializeObject<P2PPairingRequest>(message);

                Device.BeginInvokeOnMainThread(async () =>
                {
                    var confirmDappPage = new ConfirmDappPage(new ConfirmDappViewModel(Navigation, pairingRequest));
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

        private bool _isScanning = true;
        public bool IsScanning
        {
            get => _isScanning;
            set { _isScanning = value; OnPropertyChanged(nameof(IsScanning)); }
        }

        private bool _isAnalyzing = true;
        public bool IsAnalyzing
        {
            get => _isAnalyzing;
            set { _isAnalyzing = value; OnPropertyChanged(nameof(IsAnalyzing)); }
        }
    }
}