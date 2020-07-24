using System.Linq;
using Atomex.Common;

namespace atomex.ViewModel.ReceiveViewModels
{
    public class TezosReceiveViewModel : ReceiveViewModel
    {
        public override CurrencyViewModel CurrencyViewModel
        {
            get => _currencyViewModel;
            set
            {
                _currencyViewModel = value;
                OnPropertyChanged(nameof(CurrencyViewModel));

                var activeTokenAddresses = App.Account
                        .GetUnspentTokenAddressesAsync(_currencyViewModel.CurrencyCode)
                        .WaitForResult()
                        .ToList();

                var activeAddresses = App.Account
                    .GetUnspentAddressesAsync(_currencyViewModel.CurrencyCode)
                    .WaitForResult()
                    .ToList();

                activeTokenAddresses.ForEach(a => a.Balance = activeAddresses.Find(b => b.Address == a.Address)?.Balance ?? 0m);

                activeAddresses = activeAddresses.Where(a => activeTokenAddresses.FirstOrDefault(b => b.Address == a.Address) == null).ToList();

                var freeAddress = App.Account
                    .GetFreeExternalAddressAsync(_currencyViewModel.CurrencyCode)
                    .WaitForResult();

                var receiveAddresses = activeTokenAddresses.Select(w => new WalletAddressViewModel(w, _currencyViewModel.Currency.Format))
                    .Concat(activeAddresses.Select(w => new WalletAddressViewModel(w, _currencyViewModel.Currency.Format)))
                    .ToList();

                if (receiveAddresses.FirstOrDefault(w => w.Address == freeAddress.Address) == null)
                    receiveAddresses.AddEx(new WalletAddressViewModel(freeAddress, _currencyViewModel.Currency.Format, isFreeAddress: true));

                receiveAddresses = receiveAddresses.Select(wa =>
                {
                    wa.WalletAddress.Currency = CurrencyViewModel.CurrencyCode;
                    return wa;
                }).ToList();

                FromAddressList = receiveAddresses;
            }
        }

        public TezosReceiveViewModel(CurrencyViewModel currency)
           : base(currency)
        {
        }

        protected override WalletAddressViewModel GetDefaultAddress()
        {
            var activeAddressViewModel = FromAddressList
                .FirstOrDefault(vm => vm.WalletAddress.HasActivity);

            if (activeAddressViewModel != null)
                return activeAddressViewModel;

            return FromAddressList.First(vm => vm.IsFreeAddress);
        }
    }
}
