using System;
using System.Collections.Generic;
using System.Linq;
using atomex.Common;
using atomex.ViewModel;
using Atomex;

namespace atomex
{
    public class MyWalletsViewModel : BaseViewModel
    {
        public IAtomexApp AtomexApp { get; private set; }

        public List<WalletInfo> Wallets { get; set; }

        public MyWalletsViewModel(IAtomexApp app)
        {
            AtomexApp = app ?? throw new ArgumentNullException(nameof(AtomexApp));
            Wallets = WalletInfo.AvailableWallets().ToList();
        }
    }
}

