using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using atomex.Resources;
using Atomex;
using Atomex.Core;
using atomex.Views.SettingsOptions.Dapps;
using Serilog;
using Xamarin.Essentials;
using Xamarin.Forms;
using ZXing;

namespace atomex.ViewModel
{
    public class DappsViewModel : BaseViewModel
    {
        private readonly IAtomexApp _app;

        public INavigation Navigation { get; set; }

        private ObservableCollection<DappInfo> _dappsInfo;

        public ObservableCollection<DappInfo> DappsInfo
        {
            get => _dappsInfo;
            set { _dappsInfo = value; OnPropertyChanged(nameof(DappsInfo)); }
        }

        public string QrCodeScanningResult { get; set; }

        public DappsViewModel(IAtomexApp app, INavigation navigation)
        {
            _app = app ?? throw new ArgumentNullException(nameof(app));;
            Navigation = navigation ?? throw new ArgumentNullException(nameof(navigation));
            LoadDapps();
        }

        /// <summary>
        /// TODO: BeaconClient Request with user crendetails
        /// </summary>
        void LoadDapps()
        {
            DappsInfo = new ObservableCollection<DappInfo>(DappInfo.MockDapps());
        }

        private ICommand _deleteDappCommand;
        public ICommand DeleteDappCommand => _deleteDappCommand ??= new Command<string>((name) => OnDappTapped(name));

        private async void OnDappTapped(string name)
        {
            var selectedDapp = DappsInfo.FirstOrDefault(w => w.Name == name);

            var confirm = await Application.Current.MainPage.DisplayAlert(AppResources.DeletingDapp, 
                string.Format(CultureInfo.InvariantCulture, AppResources.DeletingDappConfirmationText, selectedDapp?.Name), 
                AppResources.DeleteButton, AppResources.CancelButton);
            if (confirm)
            {
                DeleteDapp(selectedDapp);
            }
        }

        private async void DeleteDapp(DappInfo dapp)
        {
            try
            {
                var index = DappsInfo.IndexOf(dapp);
                DappsInfo.Remove(dapp);
            }
            catch (Exception e)
            {
                Log.Error(e, "Delete dapp error");
            }
        }

        private ICommand _scanQrCodeCommand;

        public ICommand ScanQrCodeCommand => _scanQrCodeCommand ??= new Command(async () => await OnScanQrCodeClicked());
        public Result ScanResult { get; set; }

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

        private async Task OnScanResultCommand()
        {
            IsScanning = IsAnalyzing = false;

            if (ScanResult == null)
            {
                await Application.Current.MainPage.DisplayAlert(AppResources.Error, "Incorrect QR code format", AppResources.AcceptButton);
                await Navigation.PopAsync();
                return;
            }

            Device.BeginInvokeOnMainThread(() =>
            {
                int indexOfChar = ScanResult.Text.IndexOf(':');
                var result  = indexOfChar == -1 
                    ? ScanResult.Text 
                    : ScanResult.Text.Substring(indexOfChar + 1);
            });

            await Navigation.PopAsync();
        }

        private async Task OnScanQrCodeClicked()
        {
            IsScanning = IsAnalyzing = true;
            PermissionStatus permissions = await Permissions.CheckStatusAsync<Permissions.Camera>();

            if (permissions != PermissionStatus.Granted)
                permissions = await Permissions.RequestAsync<Permissions.Camera>();
            if (permissions != PermissionStatus.Granted)
                return;

            await Navigation.PushAsync(new ScanningQrPage(this));

            var confirmDappPage = new ConfirmDappPage(new ConfirmDappViewModel(_app, Navigation, QrCodeScanningResult));

            await Navigation.PushAsync(confirmDappPage);
        }
    }
}