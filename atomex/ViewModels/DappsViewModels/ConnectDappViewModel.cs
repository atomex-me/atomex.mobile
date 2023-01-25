using System;
using System.Reactive;
using System.Threading.Tasks;
using System.Windows.Input;
using atomex.Resources;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Serilog;
using Xamarin.Essentials;
using Xamarin.Forms;
using ZXing;

namespace atomex.ViewModels.DappsViewModels
{
    public class ConnectDappViewModel : BaseViewModel
    {
        private INavigationService _navigationService;

        public Func<string, Task> OnConnect;
        [Reactive] public string QrCodeString { get; set; }

        [Reactive] public Result ScanResult { get; set; }
        [Reactive] public bool IsScanning { get; set; }

        [Reactive] public bool CameraPermissionDenied { get; set; }

        [Reactive] public bool IsScanQrCodeTab { get; set; }

        public ConnectDappViewModel(INavigationService navigationService)
        {
            _navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));
            _ = CheckCameraPermission();

            this.WhenAnyValue(vm => vm.IsScanQrCodeTab)
                .Subscribe(value =>
                    IsScanning = !CameraPermissionDenied && value);

            this.WhenAnyValue(vm => vm.CameraPermissionDenied)
                .Subscribe(_ => 
                    IsScanning = IsScanQrCodeTab && !CameraPermissionDenied);

            IsScanQrCodeTab = true;
        }

        private async Task CheckCameraPermission()
        {
            try
            {
                PermissionStatus permissions = await Permissions.CheckStatusAsync<Permissions.Camera>();
                CameraPermissionDenied = permissions != PermissionStatus.Granted;
                this.RaisePropertyChanged(nameof(CameraPermissionDenied));
            }
            catch (Exception e)
            {
                Log.Error(e, "Check camera permission error");
            }
        }

        private ReactiveCommand<Unit, Unit> _connectCommand;

        public ReactiveCommand<Unit, Unit> ConnectCommand =>
            _connectCommand ??= ReactiveCommand.Create(() =>
            {
                Device.InvokeOnMainThreadAsync(async () =>
                {
                    IsScanning = false;
                    _navigationService?.ClosePage(TabNavigation.Portfolio);

                    if (QrCodeString != null)
                        await OnConnect(QrCodeString);

                    QrCodeString = string.Empty;
                    this.RaisePropertyChanged(nameof(QrCodeString));
                });
            });

        private ReactiveCommand<Unit, Unit> _cameraPermissionRequestCommand;

        public ReactiveCommand<Unit, Unit> CameraPermissionRequestCommand =>
            _cameraPermissionRequestCommand ??= _cameraPermissionRequestCommand = ReactiveCommand.CreateFromTask(
                async () =>
                {
                    try
                    {
                        PermissionStatus permissions = await Permissions.CheckStatusAsync<Permissions.Camera>();

                        if (permissions != PermissionStatus.Granted)
                        {
                            if (Device.RuntimePlatform == Device.iOS)
                                AppInfo.ShowSettingsUI();
                            else
                                permissions = await Permissions.RequestAsync<Permissions.Camera>();
                        }

                        await Device.InvokeOnMainThreadAsync(() =>
                        {
                            CameraPermissionDenied = permissions != PermissionStatus.Granted;
                            this.RaisePropertyChanged(nameof(CameraPermissionDenied));
                        });
                    }
                    catch (Exception e)
                    {
                        Log.Error(e, "Camera permission request error");
                    }
                });


        private ReactiveCommand<bool, Unit> _changeConnectionTypeCommand;

        public ReactiveCommand<bool, Unit> ChangeConnectionTypeCommand =>
            _changeConnectionTypeCommand ??= _changeConnectionTypeCommand =
                ReactiveCommand.Create<bool>(value => IsScanQrCodeTab = value);

        private ReactiveCommand<Unit, Unit> _pasteCommand;

        public ReactiveCommand<Unit, Unit> PasteCommand =>
            _pasteCommand ??= _pasteCommand = ReactiveCommand.CreateFromTask(async () =>
            {
                if (Clipboard.HasText)
                {
                    var text = await Clipboard.GetTextAsync();
                    QrCodeString = text;
                }
                else
                {
                    _navigationService?.ShowAlert(
                        AppResources.Error,
                        AppResources.EmptyClipboard,
                        AppResources.AcceptButton);
                }
            });

        private ReactiveCommand<Unit, Unit> _clearQrCodeCommand;

        public ReactiveCommand<Unit, Unit> ClearQrCodeCommand =>
            _clearQrCodeCommand ??=
                _clearQrCodeCommand = ReactiveCommand.Create(() => { QrCodeString = string.Empty; });

        private ICommand _scanResultCommand;

        public ICommand ScanResultCommand =>
            _scanResultCommand ??= new Command(OnScanResult);

        private void OnScanResult()
        {
            IsScanning = false;

            if (ScanResult == null)
            {
                _navigationService?.ShowAlert(
                    AppResources.Error,
                    AppResources.IncorrectQrCodeFormat,
                    AppResources.AcceptButton);
                Device.BeginInvokeOnMainThread(() => _navigationService?.ClosePage(TabNavigation.Portfolio));

                return;
            }

            Device.InvokeOnMainThreadAsync(async () =>
            {
                string key = "data=";
                int indexOfChar = ScanResult.Text.IndexOf(key, StringComparison.CurrentCulture);
                if (indexOfChar == -1)
                    QrCodeString = ScanResult.Text;
                else
                    QrCodeString = ScanResult.Text.Substring(indexOfChar + key.Length);

                _navigationService?.ClosePage(TabNavigation.Portfolio);

                if (QrCodeString != null)
                    await OnConnect(QrCodeString);

                QrCodeString = string.Empty;
                this.RaisePropertyChanged(nameof(QrCodeString));
            });
        }

        public async Task OnDeepLinkResult(string value)
        {
            if (string.IsNullOrEmpty(value)) return;

            await Device.InvokeOnMainThreadAsync(async () => await OnConnect(value));
        }

        public void AllowCamera()
        {
            Device.InvokeOnMainThreadAsync(() =>
            {
                CameraPermissionDenied = false;
                this.RaisePropertyChanged(nameof(CameraPermissionDenied));
            });
        }

        public void Reset()
        {
            IsScanning = false;
        }

        public void Init()
        {
            IsScanning = !CameraPermissionDenied && IsScanQrCodeTab;
        }
    }
}