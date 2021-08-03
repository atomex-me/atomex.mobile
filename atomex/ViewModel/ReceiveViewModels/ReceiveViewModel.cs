using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using atomex.Resources;
using atomex.Services;
using atomex.ViewModel.CurrencyViewModels;
using Atomex;
using Atomex.Common;
using Atomex.Wallet.Abstract;
using Xamarin.Essentials;
using Xamarin.Forms;

namespace atomex.ViewModel
{
    public class ReceiveViewModel : BaseViewModel
    {

        protected IAtomexApp AtomexApp { get; }

        private IToastService ToastService;

        protected CurrencyViewModel _currencyViewModel;
        public virtual CurrencyViewModel CurrencyViewModel
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
                    .GetFreeExternalAddressAsync(_currencyViewModel.Currency.Name)
                    .WaitForResult();

                var receiveAddresses = activeTokenAddresses.Select(w => new WalletAddressViewModel(w, _currencyViewModel.Currency.Format))
                    .Concat(activeAddresses.Select(w => new WalletAddressViewModel(w, _currencyViewModel.Currency.Format)))
                    .ToList();

                if (receiveAddresses.FirstOrDefault(w => w.Address == freeAddress.Address) == null)
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

        private float _opacity = 1f;
        public float Opacity
        {
            get => _opacity;
            set { _opacity = value; OnPropertyChanged(nameof(Opacity)); }
        }

        private bool _isLoading = false;
        public bool IsLoading
        {
            get => _isLoading;
            set
            {
                if (_isLoading == value)
                    return;

                _isLoading = value;

                if (_isLoading)
                    Opacity = 0.3f;
                else
                    Opacity = 1f;

                OnPropertyChanged(nameof(IsLoading));
            }
        }

        public ReceiveViewModel(CurrencyViewModel currencyViewModel)
        {
            AtomexApp = currencyViewModel.GetAtomexApp() ?? throw new ArgumentNullException(nameof(AtomexApp));
            ToastService = DependencyService.Get<IToastService>();
            CurrencyViewModel = currencyViewModel;
        }

        protected virtual WalletAddressViewModel GetDefaultAddress()
        {
            //return FromAddressList.First(vm => vm.IsFreeAddress);

            if (CurrencyViewModel.Currency is TezosConfig || CurrencyViewModel.Currency is EthereumConfig)
            {
                var activeAddressViewModel = FromAddressList
                    .FirstOrDefault(vm => vm.WalletAddress.HasActivity && vm.WalletAddress.AvailableBalance() > 0);

                if (activeAddressViewModel != null)
                    return activeAddressViewModel;
            }

            return FromAddressList.First(vm => vm.IsFreeAddress);
        }

        private ICommand _copyCommand;
        public ICommand CopyCommand => _copyCommand ??= new Command(async () => await OnCopyClicked());

        private ICommand _shareCommand;
        public ICommand ShareCommand => _shareCommand ??= new Command(async () => await OnShareClicked());

        async Task OnCopyClicked()
        {
            if (SelectedAddress != null)
            {
                await Clipboard.SetTextAsync(SelectedAddress.Address);
                ToastService?.Show(AppResources.AddressCopied, ToastPosition.Top, Application.Current.RequestedTheme.ToString());
            }
            else
            {
                await Application.Current.MainPage.DisplayAlert(AppResources.Error, AppResources.CopyError, AppResources.AcceptButton);
            }
        }

        async Task OnShareClicked()
        {
            IsLoading = true;

            await Share.RequestAsync(new ShareTextRequest
            {
                Text = AppResources.MyPublicAddress + " " + SelectedAddress.WalletAddress.Currency + ":\r\n" + SelectedAddress.Address,
                Title = AppResources.AddressSharing
            });

            await Task.Delay(1000);

            IsLoading = false;
        }
    }
}

