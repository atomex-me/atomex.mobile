using atomex.Common;
using atomex.ViewModel.CurrencyViewModels;
using Atomex.Common;
using Atomex.Wallet.Abstract;
using System.Linq;

namespace atomex.ViewModel.ReceiveViewModels
{
    public class EthereumReceiveViewModel : ReceiveViewModel
    {
        public override CurrencyViewModel CurrencyViewModel
        {
            get => _currencyViewModel;
            set
            {
                _currencyViewModel = value;
                OnPropertyChanged(nameof(CurrencyViewModel));

                var activeTokenAddresses = AtomexApp.Account
                    .GetCurrencyAccount<ILegacyCurrencyAccount>(_currencyViewModel.Currency.Name)
                    .GetUnspentTokenAddressesAsync()
                    .WaitForResult()
                    .ToList();

                var activeAddresses = AtomexApp.Account
                    .GetUnspentAddressesAsync(_currencyViewModel.Currency.Name)
                    .WaitForResult()
                    .ToList();

                activeTokenAddresses.ForEach(a => a.Balance = activeAddresses.Find(b => b.Address == a.Address)?.Balance ?? 0m);

                activeAddresses = activeAddresses
                    .Where(a => activeTokenAddresses.FirstOrDefault(b => b.Address == a.Address) == null)
                    .ToList();

                var freeAddress = AtomexApp.Account
                    .GetFreeExternalAddressAsync(_currencyViewModel.CurrencyCode)
                    .WaitForResult();

                var receiveAddresses = activeTokenAddresses
                    .DistinctBy(wa => wa.Address)
                    .Select(w => new WalletAddressViewModel(w, _currencyViewModel.Currency.Format))
                    .Concat(activeAddresses.Select(w => new WalletAddressViewModel(w, _currencyViewModel.Currency.Format)))
                    .ToList();

                if (receiveAddresses.FirstOrDefault(w => w.Address == freeAddress.Address) == null)
                    receiveAddresses.AddEx(new WalletAddressViewModel(freeAddress, _currencyViewModel.Currency.Format, isFreeAddress: true));

                FromAddressList = receiveAddresses;
            }
        }

        public EthereumReceiveViewModel(CurrencyViewModel currency)
           : base(currency)
        {
        }

        protected override WalletAddressViewModel GetDefaultAddress()
        {
            var activeAddressViewModel = FromAddressList
                .OrderByDescending(vm => vm.WalletAddress.AvailableBalance())
                .ToList()
                .FirstOrDefault(vm => vm.WalletAddress.HasActivity);

            if (activeAddressViewModel != null)
                return activeAddressViewModel;

            return FromAddressList.First(vm => vm.IsFreeAddress);
        }
    }
}
