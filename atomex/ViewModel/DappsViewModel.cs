using System;
using System.Collections.ObjectModel;
using System.Threading;
using Atomex;
using Atomex.Core;
using Xamarin.Forms;

namespace atomex.ViewModel
{
    public class DappsViewModel : BaseViewModel
    {
        private readonly IAtomexApp _app;

        public INavigation Navigation { get; set; }

        private CancellationTokenSource _cancellation;

        private bool _isUpdating;
        public bool IsUpdating
        {
            get => _isUpdating;
            set
            {
                _isUpdating = value;
                OnPropertyChanged(nameof(IsUpdating));
            }
        }

        public ObservableCollection<DappInfo> DappsInfo { get; set; }

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
            DappsInfo = new ObservableCollection<DappInfo>()
            {
                new DappInfo() { Name = "abcd", Network = Network.TestNet, IsActive = true},
                new DappInfo() { Name = "xyz", Network = Network.MainNet, IsActive = true}
            };
            OnPropertyChanged(nameof(DappsInfo));
        }
    }
}