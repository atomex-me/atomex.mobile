using System.Linq;
using atomex.Common;
using Xamarin.Forms;

namespace atomex
{
    public class StartViewModel : ContentPage
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

