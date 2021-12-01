using System;
using System.Collections.Generic;
using Atomex;
using Atomex.Core;
using Xamarin.Forms;
namespace atomex.ViewModel
{
    public class ConfirmDappViewModel : BaseViewModel
    {

        private readonly IAtomexApp _app;

        public INavigation Navigation { get; set; }

        private DappInfo _dapp;

        // public P2PPairingRequest PairingRequest { get; set; }

        public DappInfo Dapp
        {
            get => _dapp;
            set { _dapp = value; OnPropertyChanged(nameof(Dapp)); }
        }

        // public ConfirmDappViewModel(IAtomexApp app, INavigation navigation, P2PPairingRequest pairingRequest)
        // {
        //     _app = app ?? throw new ArgumentNullException(nameof(app));;
        //     Navigation = navigation ?? throw new ArgumentNullException(nameof(navigation));
        //     LoadDapp();
        // }

        void LoadDapp()
        {
            Dapp = new DappInfo()
            {
                Name = "Dapp", Network = Network.TestNet, ImageUrl = "BTC", DappDeviceType = DappType.Mobile,
                Permissions = new List<Permission>()
                {
                    new Permission() { Name = "Ты можешь сидеть?" },
                    new Permission() { Name = "Тебе еще что-то надо?" }
                }
            };
        }
    }
}