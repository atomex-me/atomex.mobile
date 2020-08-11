using System;
using System.Collections.Generic;
using System.Linq;
using Atomex;
using Atomex.Common;

namespace atomex.ViewModel
{
    public class ReceiveViewModel : BaseViewModel
    {

        protected IAtomexApp App { get; }

        protected CurrencyViewModel _currencyViewModel;
        public virtual CurrencyViewModel CurrencyViewModel
        {
            get => _currencyViewModel;
            set
            {
                _currencyViewModel = value;
                OnPropertyChanged(nameof(CurrencyViewModel));

                var activeAddresses = App.Account
                    .GetUnspentAddressesAsync(_currencyViewModel.CurrencyCode)
                    .WaitForResult();

                var freeAddress = App.Account
                    .GetFreeExternalAddressAsync(_currencyViewModel.CurrencyCode)
                    .WaitForResult();

                var receiveAddresses = activeAddresses
                    .Select(wa => new WalletAddressViewModel(wa, _currencyViewModel.Currency.Format))
                    .ToList();

                if (activeAddresses.FirstOrDefault(w => w.Address == freeAddress.Address) == null)
                    receiveAddresses.AddEx(new WalletAddressViewModel(freeAddress, _currencyViewModel.Currency.Format, isFreeAddress: true));

                FromAddressList = receiveAddresses;
            }
        }

        private List<WalletAddressViewModel> _fromAddressList;
        public virtual List<WalletAddressViewModel> FromAddressList
        {
            get => _fromAddressList;
            set
            {
                _fromAddressList = value;
                OnPropertyChanged(nameof(FromAddressList));

                SelectedAddress = GetDefaultAddress();
            }
        }

        private WalletAddressViewModel _selectedAddress;
        public WalletAddressViewModel SelectedAddress
        {
            get => _selectedAddress;
            set
            {
                _selectedAddress = value;
                OnPropertyChanged(nameof(SelectedAddress));
            }
        }

        public ReceiveViewModel(CurrencyViewModel currencyViewModel)
        {
            App = currencyViewModel.GetAtomexApp() ?? throw new ArgumentNullException(nameof(App));
            CurrencyViewModel = currencyViewModel;
        }

        protected virtual WalletAddressViewModel GetDefaultAddress()
        {
            return FromAddressList.First(vm => vm.IsFreeAddress);
        }
    }
}

