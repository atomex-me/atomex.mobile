using System;
using System.Linq;
using atomex.Common;
using atomex.ViewModel;

namespace atomex
{
    public class StartViewModel : BaseViewModel
    {
        private bool _hasWallets;
        public bool HasWallets
        {
            get => _hasWallets;
            private set { _hasWallets = value; OnPropertyChanged(nameof(HasWallets)); }
        }

        public StartViewModel()
        {
            HasWallets = WalletInfo.AvailableWallets().Count() > 0;
        }
    }
}

