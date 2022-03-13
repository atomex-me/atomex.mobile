using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using atomex.Resources;
using atomex.Services;
using atomex.Views;
using Atomex;
using Atomex.Common;
using Atomex.Core;
using Atomex.ViewModels;
using Atomex.Wallet.Abstract;
using Xamarin.Essentials;
using Xamarin.Forms;

namespace atomex.ViewModel
{
    public class ReceiveViewModel : BaseViewModel
    {

        protected IAtomexApp AtomexApp { get; }

        private IToastService ToastService;

        public INavigation Navigation { get; set; }

        private CurrencyConfig _currency;
        public CurrencyConfig Currency
        {
            get => _currency;
            set
            {
                _currency = value;
                OnPropertyChanged(nameof(Currency));

                // get all addresses with tokens (if exists)
                var tokenAddresses = Currencies.HasTokens(_currency.Name)
                    ? (AtomexApp.Account
                        .GetCurrencyAccount(_currency.Name) as IHasTokens)
                        ?.GetUnspentTokenAddressesAsync()
                        .WaitForResult() ?? new List<WalletAddress>()
                    : new List<WalletAddress>();

                // get all active addresses
                var activeAddresses = AtomexApp.Account
                    .GetUnspentAddressesAsync(_currency.Name)
                    .WaitForResult()
                    .ToList();

                // get free external address
                var freeAddress = AtomexApp.Account
                    .GetFreeExternalAddressAsync(_currency.Name)
                    .WaitForResult();

                FromAddressList = activeAddresses
                    .Concat(tokenAddresses)
                    .Concat(new WalletAddress[] { freeAddress })
                    .GroupBy(w => w.Address)
                    .Select(g =>
                    {
                            // main address
                            var address = g.FirstOrDefault(w => w.Currency == _currency.Name);

                        var isFreeAddress = address?.Address == freeAddress.Address;

                        var hasTokens = g.Any(w => w.Currency != _currency.Name);

                        var tokenAddresses = TokenContract != null
                            ? g.Where(w => w.TokenBalance?.Contract == TokenContract)
                            : Enumerable.Empty<WalletAddress>();

                        var hasSeveralTokens = tokenAddresses.Count() > 1;

                        var tokenAddress = tokenAddresses.FirstOrDefault();

                        var tokenBalance = hasSeveralTokens
                            ? tokenAddresses.Count()
                            : tokenAddress?.Balance ?? 0m;

                        var showTokenBalance = hasSeveralTokens
                            ? tokenBalance != 0
                            : TokenContract != null && tokenAddress?.TokenBalance?.Symbol != null;

                        var tokenCode = hasSeveralTokens
                            ? "TOKENS"
                            : tokenAddress?.TokenBalance?.Symbol ?? "";

                        return new WalletAddressViewModel
                        {
                            Address = g.Key,
                            HasActivity = address?.HasActivity ?? hasTokens,
                            AvailableBalance = address?.AvailableBalance() ?? 0m,
                            CurrencyFormat = _currency.Format,
                            CurrencyCode = _currency.Name,
                            IsFreeAddress = isFreeAddress,
                            ShowTokenBalance = showTokenBalance,
                            TokenBalance = tokenBalance,
                            TokenFormat = "F8",
                            TokenCode = tokenCode
                        };
                    })
                    .ToList();
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

        public string TokenContract { get; private set; }

        private bool _isLoading = false;
        public bool IsLoading
        {
            get => _isLoading;
            set
            {
                if (_isLoading == value)
                    return;

                _isLoading = value;
                OnPropertyChanged(nameof(IsLoading));
            }
        }

        public ReceiveViewModel(IAtomexApp app, CurrencyConfig currency, INavigation navigation)
        {
            AtomexApp = app ?? throw new ArgumentNullException(nameof(AtomexApp));
            Currency = currency ?? throw new ArgumentNullException(nameof(Currency));
            Navigation = navigation ?? throw new ArgumentNullException(nameof(Navigation));
            ToastService = DependencyService.Get<IToastService>();
        }

        public ReceiveViewModel(IAtomexApp app, CurrencyConfig currency, INavigation navigation, string tokenContract = null)
        {
            AtomexApp = app ?? throw new ArgumentNullException(nameof(AtomexApp));
            TokenContract = tokenContract;
            Currency = currency ?? throw new ArgumentNullException(nameof(Currency));
            Navigation = navigation ?? throw new ArgumentNullException(nameof(Navigation));
            ToastService = DependencyService.Get<IToastService>();
        }

        protected virtual WalletAddressViewModel GetDefaultAddress()
        {
            if (Currency is TezosConfig || Currency is EthereumConfig)
            {
                var activeAddressViewModel = FromAddressList
                    .Where(vm => vm.HasActivity && vm.AvailableBalance > 0)
                    .MaxByOrDefault(vm => vm.AvailableBalance);

                if (activeAddressViewModel != null)
                    return activeAddressViewModel;
            }

            return FromAddressList.First(vm => vm.IsFreeAddress);
        }

        private ICommand _showReceiveAddressesCommand;
        public ICommand ShowReceiveAddressesCommand => _showReceiveAddressesCommand ??= new Command(async () => await OnShowReceiveAddressesClicked());

        private ICommand _selectAddressCommand;
        public ICommand SelectAddressCommand => _selectAddressCommand ??= new Command<WalletAddressViewModel>(async (address) => await OnAddressClicked(address));

        private async Task OnAddressClicked(WalletAddressViewModel address)
        {
            SelectedAddress = address;

            await Navigation.PopAsync();
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
                Text = AppResources.MyPublicAddress +
                    " " +
                    SelectedAddress.CurrencyCode +
                    ":\r\n" +
                    SelectedAddress.Address,

                Title = AppResources.AddressSharing
            });

            await Task.Delay(1000);

            IsLoading = false;
        }

        async Task OnShowReceiveAddressesClicked()
        {
            await Navigation.PushAsync(new AddressesListPage(this));
        }
    }
}

