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
using Xamarin.Forms;

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

            var confirm = await Application.Current.MainPage.DisplayAlert(AppResources.DeletingDapp, string.Format(CultureInfo.InvariantCulture, AppResources.DeletingDappConfirmationText, selectedDapp?.Name), AppResources.DeleteButton, AppResources.CancelButton);
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

        private async Task OnScanQrCodeClicked()
        {
            await Navigation.PushAsync(new ConfirmDappPage(new ConfirmDappViewModel(_app, Navigation)));
        }
    }
}