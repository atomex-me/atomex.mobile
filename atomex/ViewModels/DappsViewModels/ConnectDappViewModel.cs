using System;
using System.Reactive;
using System.Threading.Tasks;
using System.Windows.Input;
using atomex.Models;
using atomex.Resources;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Xamarin.Essentials;
using Xamarin.Forms;
using ZXing;

namespace atomex.ViewModels.DappsViewModels
{
    public class ConnectDappViewModel : BaseViewModel
    {
        protected INavigationService _navigationService;
        
        public Func<string, Task> OnConnect;
        [Reactive] public string QrCodeString { get; set; }

        [Reactive] public Result ScanResult { get; set; }
        [Reactive] public bool IsScanning { get; set; }
        [Reactive] public bool IsAnalyzing { get; set; }  
        
        [Reactive] public bool IsScanQrCodeTab { get; set; }

        public ConnectDappViewModel(INavigationService navigationService)
        {
            _navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));
            IsScanQrCodeTab = true;
        }
        
        private ReactiveCommand<Unit, Unit> _connectCommand;
        public ReactiveCommand<Unit, Unit> ConnectCommand => 
            _connectCommand ??= ReactiveCommand.Create(() =>
            {
                IsScanning = false;
                IsAnalyzing = false;
                
                Device.InvokeOnMainThreadAsync(async () =>
                {
                    _navigationService?.ClosePage(TabNavigation.Portfolio);
                    
                    if (QrCodeString != null)
                    {
                        _navigationService?.DisplaySnackBar(SnackbarMessage.MessageType.Regular,
                            AppResources.Connecting + "...");
                        await OnConnect(QrCodeString);
                    }
                    
                    QrCodeString = string.Empty;
                    this.RaisePropertyChanged(nameof(QrCodeString));
                });
            });
        
        private ReactiveCommand<bool, Unit> _changeConnectionTypeCommand;
        
        public ReactiveCommand<bool, Unit> ChangeConnectionTypeCommand =>
            _changeConnectionTypeCommand ??= _changeConnectionTypeCommand = ReactiveCommand.Create<bool>(value =>
            {
                IsScanQrCodeTab = value;
            });
        
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
                    _navigationService?.ShowAlert(AppResources.Error, AppResources.EmptyClipboard,
                        AppResources.AcceptButton);
                }
            });
        
        private ReactiveCommand<Unit, Unit> _clearQrCodeCommand;
        
        public ReactiveCommand<Unit, Unit> ClearQrCodeCommand =>
            _clearQrCodeCommand ??= _clearQrCodeCommand = ReactiveCommand.Create(() =>
            {
                QrCodeString = string.Empty;
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

                if (QrCodeString != null)
                {
                    _navigationService?.DisplaySnackBar(SnackbarMessage.MessageType.Regular,
                        AppResources.Connecting + "...");
                    await OnConnect(QrCodeString);
                }

                QrCodeString = string.Empty;
                this.RaisePropertyChanged(nameof(QrCodeString));
            });
        }

        public async Task OnDeepLinkResult(string value)
        {
            if (string.IsNullOrEmpty(value)) return;
            
            await Device.InvokeOnMainThreadAsync(async () =>
            {
                _navigationService?.DisplaySnackBar(SnackbarMessage.MessageType.Regular,
                        AppResources.Connecting + "...");
                await OnConnect(value);
            });
        }
    }
}