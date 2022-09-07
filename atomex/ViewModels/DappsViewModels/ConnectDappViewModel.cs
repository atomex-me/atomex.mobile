using System;
using System.Threading.Tasks;
using System.Windows.Input;
using atomex.Models;
using atomex.Resources;
using ReactiveUI.Fody.Helpers;
using Xamarin.Forms;
using ZXing;

namespace atomex.ViewModels.DappsViewModels
{
    public class ConnectDappViewModel : BaseViewModel
    {
        protected INavigationService _navigationService;
        
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

        private ICommand _scanResultCommand;

        public ICommand ScanResultCommand =>
            _scanResultCommand ??= new Command(OnScanResult);
        
        private void OnScanResult()
        {
            IsScanning = false;
            IsAnalyzing = false;

            if (ScanResult == null)
            {
                _navigationService?.ShowAlert(AppResources.Error, AppResources.IncorrectQrCodeFormat,
                    AppResources.AcceptButton);
                Device.BeginInvokeOnMainThread(() =>  _navigationService?.ClosePage(TabNavigation.Portfolio));

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

                if (QrCodeString != null && AddressToConnect != null)
                {
                    _navigationService?.DisplaySnackBar(SnackbarMessage.MessageType.Regular,
                        AppResources.Connecting + "...");
                    await OnConnect(QrCodeString, AddressToConnect);
                }
            });
        }
    }
}