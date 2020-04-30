using System;
using System.Linq;
using atomex.Common;
using atomex.ViewModel;
using Atomex;

namespace atomex
{
    public class StartViewModel : BaseViewModel
    {
        public IAtomexApp AtomexApp { get; private set; }

        private bool _hasWallets;
        public bool HasWallets
        {
            get => _hasWallets;
            private set { _hasWallets = value; OnPropertyChanged(nameof(HasWallets)); }
        }

        public StartViewModel(IAtomexApp app)
        {
            AtomexApp = app ?? throw new ArgumentNullException(nameof(AtomexApp));
            HasWallets = WalletInfo.AvailableWallets().Count() > 0;
        }
    }
}

