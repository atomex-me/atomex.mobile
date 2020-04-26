using Atomex.Core;

namespace atomex.ViewModel
{
    public class WalletAddressViewModel
    {
        public WalletAddress WalletAddress { get; }
        public string Address => WalletAddress.Address;
        public decimal AvailableBalance => WalletAddress.AvailableBalance();
        public string CurrencyFormat { get; }
        public bool IsFreeAddress { get; }
        public string DisplayAddressWithBalance =>
            Address.Substring(0,6) + "..." + Address.Remove(0, Address.Length - 6) + " - " +
            AvailableBalance.ToString("0.######") + " " + WalletAddress.Currency;

        public WalletAddressViewModel(
            WalletAddress walletAddress,
            string currencyFormat,
            bool isFreeAddress = false)
        {
            WalletAddress = walletAddress;
            CurrencyFormat = currencyFormat;
            IsFreeAddress = isFreeAddress;
        }
    }
}

