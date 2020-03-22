using System.Collections.Generic;
using atomex.Common;
using atomex.ViewModel;

namespace atomex
{
    public class MyWalletsViewModel : BaseViewModel
    {
        public IEnumerable<WalletInfo> Wallets { get; set; }

        public MyWalletsViewModel()
        {
            Wallets = WalletInfo.AvailableWallets();
        }
    }
}

