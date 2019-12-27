using System.Collections.Generic;
using atomex.Models;
using Atomex;

namespace atomex.ViewModel
{
    public class WalletsViewModel : BaseViewModel
    {
        
        public float TotalCost { get; set; }

        List<Wallet> wallets;
        public List<Wallet> Wallets
        {
            get { return wallets; }
            set
            {
                if (wallets != value)
                {
                    wallets = value;
                    OnPropertyChanged(nameof(Wallets));
                }
            }
        }

        //public WalletsViewModel(IAtomexApp atomexApp)
        //{
        //    var terminal = atomexApp.Terminal;


        //    atomexApp.Terminal.SwapUpdated += Terminal_SwapUpdated;

        //    Wallets = WalletData.Wallets;
        //}

        public WalletsViewModel()
        {
            Wallets = WalletData.Wallets;
        }

        private void Terminal_SwapUpdated(object sender, Atomex.Swaps.SwapEventArgs e)
        {
            //var swap = e.Swap;
            //e.Swap;

            //throw new System.NotImplementedException();
        }
    }
}