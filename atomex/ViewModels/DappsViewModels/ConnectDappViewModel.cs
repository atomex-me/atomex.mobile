using System;
using System.Reactive;
using System.Threading.Tasks;
using System.Windows.Input;
using atomex.Common;
using atomex.Resources;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Serilog;
using Xamarin.Forms;
using ZXing;

namespace atomex.ViewModels.DappsViewModels
{
    public class ConnectDappViewModel : BaseViewModel
    {
        protected INavigationService _navigationService;
        
        public Action OnBack;
        public Func<string, string, Task> OnConnect;
        [Reactive] public string AddressToConnect { get; set; }
        [Reactive] public string QrCodeString { get; set; }

        [Reactive] public Result ScanResult { get; set; }
        [Reactive] public bool IsScanning { get; set; }
        [Reactive] public bool IsAnalyzing { get; set; }

        public ConnectDappViewModel(INavigationService navigationService)
        {
            _navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));
        }

        private ReactiveCommand<Unit, Unit> _backCommand;

        public ReactiveCommand<Unit, Unit> BackCommand =>
            _backCommand ??= _backCommand = ReactiveCommand.Create(() => { OnBack?.Invoke(); });

        private ReactiveCommand<Unit, Unit> _connectCommand;

        public ReactiveCommand<Unit, Unit> ConnectCommand =>
            _connectCommand ??= _connectCommand = ReactiveCommand.CreateFromTask(async () =>
            {
                if (QrCodeString != null && AddressToConnect != null)
                    await OnConnect(QrCodeString, AddressToConnect);
            });

        private ReactiveCommand<string, Unit> _copyCommand;

        public ReactiveCommand<string, Unit> CopyCommand => _copyCommand ??= ReactiveCommand.Create<string>(data =>
        {
            try
            {
               
            }
            catch (Exception e)
            {
                Log.Error(e, "Copy to clipboard error");
            }
        });
        
        private ICommand _scanResultCommand;

        public ICommand ScanResultCommand =>
            _scanResultCommand ??= new Command(OnScanResult);
        
        private void OnScanResult()
        {
            IsScanning = false;
            IsAnalyzing = false;

            if (ScanResult == null)
            {
                _navigationService?.ShowAlert(AppResources.Error, "Incorrect QR code format",
                    AppResources.AcceptButton);
                Device.BeginInvokeOnMainThread(() =>  _navigationService?.ClosePage(TabNavigation.Portfolio));

                return;
            }

            Device.BeginInvokeOnMainThread(async () =>
            {
                string key = "data=";
                int indexOfChar = ScanResult.Text.IndexOf(key, StringComparison.CurrentCulture);
                if (indexOfChar == -1)
                    QrCodeString = ScanResult.Text;
                else
                    QrCodeString = ScanResult.Text.Substring(indexOfChar + key.Length);
                
                _navigationService?.ClosePage(TabNavigation.Portfolio);

                if (QrCodeString != null && AddressToConnect != null)
                    await OnConnect(QrCodeString, AddressToConnect); ;
            });
        }
    }
}