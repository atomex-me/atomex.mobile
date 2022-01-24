using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Threading.Tasks;
using System.Windows.Input;
using atomex.Resources;
using Atomex.Common;
using Atomex.Core;
using Atomex.ViewModels;
using Atomex.Wallet.Abstract;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Serilog;
using Xamarin.Essentials;
using Xamarin.Forms;
using ZXing;

namespace atomex.ViewModel.SendViewModels
{
    public class SelectAddressViewModel : BaseViewModel
    {
        public Action<string, decimal> ConfirmAction { get; set; }
        public Action ScanAction { get; set; }
        public Action<string> ScanResultAction { get; set; }
        public bool UseToSelectFrom { get; set; }
        private ObservableCollection<WalletAddressViewModel> InitialMyAddresses { get; set; }
        [Reactive] public ObservableCollection<WalletAddressViewModel> MyAddresses { get; set; }
        [Reactive] public string SearchPattern { get; set; }
        [Reactive] public string ToAddress { get; set; }
        [Reactive] public bool SortIsAscending { get; set; }
        [Reactive] public bool SortByBalance { get; set; }
        [Reactive] public WalletAddressViewModel SelectedAddress { get; set; }

        public SelectAddressViewModel(IAccount account, CurrencyConfig currency, bool useToSelectFrom = false)
        {
            this.WhenAnyValue(
                    vm => vm.SortByBalance,
                    vm => vm.SortIsAscending,
                    vm => vm.SearchPattern)
                .Subscribe(value =>
                {
                    var (item1, item2, item3) = value;

                    if (MyAddresses == null) return;

                    var myAddresses = new ObservableCollection<WalletAddressViewModel>(
                        InitialMyAddresses
                            .Where(addressViewModel => addressViewModel.WalletAddress.Address.ToLower()
                                .Contains(item3?.ToLower() ?? string.Empty)));

                    if (!item1)
                    {
                        var myAddressesList = myAddresses.ToList();
                        if (item2)
                        {
                            myAddressesList.Sort((a1, a2) =>
                            {
                                var typeResult = a1.WalletAddress.KeyType.CompareTo(a2.WalletAddress.KeyType);

                                if (typeResult != 0)
                                    return typeResult;

                                var accountResult =
                                    a1.WalletAddress.KeyIndex.Account.CompareTo(a2.WalletAddress.KeyIndex.Account);

                                if (accountResult != 0)
                                    return accountResult;

                                var chainResult =
                                    a1.WalletAddress.KeyIndex.Chain.CompareTo(a2.WalletAddress.KeyIndex.Chain);

                                return chainResult != 0
                                    ? chainResult
                                    : a1.WalletAddress.KeyIndex.Index.CompareTo(a2.WalletAddress.KeyIndex.Index);
                            });
                        }
                        else
                        {
                            myAddressesList.Sort((a2, a1) =>
                            {
                                var typeResult = a1.WalletAddress.KeyType.CompareTo(a2.WalletAddress.KeyType);

                                if (typeResult != 0)
                                    return typeResult;

                                var accountResult =
                                    a1.WalletAddress.KeyIndex.Account.CompareTo(a2.WalletAddress.KeyIndex.Account);

                                if (accountResult != 0)
                                    return accountResult;

                                var chainResult =
                                    a1.WalletAddress.KeyIndex.Chain.CompareTo(a2.WalletAddress.KeyIndex.Chain);

                                return chainResult != 0
                                    ? chainResult
                                    : a1.WalletAddress.KeyIndex.Index.CompareTo(a2.WalletAddress.KeyIndex.Index);
                            });
                        }

                        MyAddresses = new ObservableCollection<WalletAddressViewModel>(myAddressesList);
                        
                    }
                    else
                    {
                        MyAddresses = new ObservableCollection<WalletAddressViewModel>(item2
                            ? myAddresses.OrderBy(addressViewModel => addressViewModel.AvailableBalance)
                            : myAddresses.OrderByDescending(addressViewModel => addressViewModel.AvailableBalance));
                        
                    }
                });

            UseToSelectFrom = useToSelectFrom;

            MyAddresses = new ObservableCollection<WalletAddressViewModel>(
                AddressesHelper
                    .GetReceivingAddressesAsync(
                        account: account,
                        currency: currency)
                    .WaitForResult()
                    .Where(address => !useToSelectFrom || !address.IsFreeAddress)
                );
            
            InitialMyAddresses = new ObservableCollection<WalletAddressViewModel>(MyAddresses);
        }

        private ReactiveCommand<Unit, Unit> _changeSortTypeCommand;
        public ReactiveCommand<Unit, Unit> ChangeSortTypeCommand => _changeSortTypeCommand ??=
            (_changeSortTypeCommand = ReactiveCommand.Create(() => { SortByBalance = !SortByBalance; }));

        private ReactiveCommand<Unit, Unit> _changeSortDirectionCommand;
        public ReactiveCommand<Unit, Unit> ChangeSortDirectionCommand => _changeSortDirectionCommand ??=
            (_changeSortDirectionCommand = ReactiveCommand.Create(() => { SortIsAscending = !SortIsAscending; }));

        private ReactiveCommand<Unit, Unit> _confirmCommand;
        public ReactiveCommand<Unit, Unit> ConfirmCommand => _confirmCommand ??=
            (_confirmCommand = ReactiveCommand.Create(() =>
            {
                var selectedAddress = SelectedAddress == null
                    ? ToAddress
                    : SelectedAddress.WalletAddress.Address;

                var balance = SelectedAddress == null
                    ? 0m
                    : SelectedAddress.WalletAddress.AvailableBalance();

                ConfirmAction?.Invoke(selectedAddress, balance);
            }));

        private ICommand _searchAddressCommand;
        public ICommand SearchAddressCommand => _searchAddressCommand ??= new Command<string>((value) => OnSearchEntryTextChanged(value));

        private void OnSearchEntryTextChanged(string value)
        {
            try
            {
                Console.WriteLine(value);
            }
            catch (Exception e)
            {
                Log.Error(e.Message);
            }
        }
        private ICommand _copyAddressCommand;
        public ICommand CopyAddressCommand =>
            _copyAddressCommand ??= (_copyAddressCommand = ReactiveCommand.Create((WalletAddress address) =>
            {
            }));

        private ReactiveCommand<Unit, Unit> _scanCommand;
        public ReactiveCommand<Unit, Unit> ScanCommand =>
            _scanCommand ??= (_scanCommand = ReactiveCommand.CreateFromTask(OnScanButtonClicked));

        private ICommand _scanResultCommand;
        public ICommand ScanResultCommand =>
            _scanResultCommand ??= new Command(async () => await OnScanResult());


        [Reactive] public Result ScanResult { get; set; }
        [Reactive] public bool IsScanning { get; set; }
        [Reactive] public bool IsAnalyzing { get; set; }

        private async Task OnScanResult()
        {
            IsScanning = false;
            IsAnalyzing = false;
            this.RaisePropertyChanged(nameof(IsScanning));
            this.RaisePropertyChanged(nameof(IsAnalyzing));

            if (ScanResult == null)
            {
                await Application.Current.MainPage.DisplayAlert(AppResources.Error, "Incorrect QR code format", AppResources.AcceptButton);

                Device.BeginInvokeOnMainThread(() =>
                {
                    ScanResultAction.Invoke(string.Empty);
                });
                return;
            }

            Device.BeginInvokeOnMainThread(() =>
            {
                int indexOfChar = ScanResult.Text.IndexOf(':');
                if (indexOfChar == -1)
                    ToAddress = ScanResult.Text;
                else
                    ToAddress = ScanResult.Text.Substring(indexOfChar + 1);

                ScanResultAction.Invoke(ToAddress);
            });
        }

        private async Task OnScanButtonClicked()
        {
            PermissionStatus permissions = await Permissions.CheckStatusAsync<Permissions.Camera>();

            if (permissions != PermissionStatus.Granted)
                permissions = await Permissions.RequestAsync<Permissions.Camera>();
            if (permissions != PermissionStatus.Granted)
                return;

            IsScanning = true;
            IsAnalyzing = true;
            this.RaisePropertyChanged(nameof(IsScanning));
            this.RaisePropertyChanged(nameof(IsAnalyzing));

            ScanAction?.Invoke();
        }
    }
}
