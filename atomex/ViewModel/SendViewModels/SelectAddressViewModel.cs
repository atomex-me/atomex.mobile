using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Threading.Tasks;
using System.Windows.Input;
using atomex.Resources;
using atomex.Services;
using atomex.Views.Send;
using Atomex.Common;
using Atomex.Core;
using Atomex.ViewModels;
using Atomex.Wallet.Abstract;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Xamarin.Essentials;
using Xamarin.Forms;
using ZXing;

namespace atomex.ViewModel.SendViewModels
{
    public class SelectAddressViewModel : BaseViewModel
    {
        protected IToastService ToastService { get; set; }
        protected INavigation Navigation { get; set; }

        public enum SettingType
        {
            Init,
            Change,
            InitFromSearch,
            ChangeFromSearch
        }

        public Action<string, decimal> ConfirmAction { get; set; }
        public bool UseToSelectFrom { get; set; }
        public SettingType AddressSettingType { get; set; }
        private ObservableCollection<WalletAddressViewModel> InitialMyAddresses { get; set; }
        [Reactive] public ObservableCollection<WalletAddressViewModel> MyAddresses { get; set; }
        [Reactive] public string SearchPattern { get; set; }
        [Reactive] public string ToAddress { get; set; }
        [Reactive] public bool SortIsAscending { get; set; }
        [Reactive] public bool SortByBalance { get; set; }
        [Reactive] public WalletAddressViewModel SelectedAddress { get; set; }

        [Reactive] public Result ScanResult { get; set; }
        [Reactive] public bool IsScanning { get; set; }
        [Reactive] public bool IsAnalyzing { get; set; }

        [Reactive] public bool IsMyAddressesTab { get; set; }
        [Reactive] public string ToolbarIcon { get; set; }
        [Reactive] public ReactiveCommand<Unit, Unit> ToolbarCommand { get; set; }

        public SelectAddressViewModel(IAccount account, CurrencyConfig currency, INavigation navigation, bool useToSelectFrom = false, string tokenContract = null)
        {
            ToastService = DependencyService.Get<IToastService>() ?? throw new ArgumentNullException(nameof(ToastService));
            Navigation = navigation ?? throw new ArgumentNullException(nameof(Navigation));

            this.WhenAnyValue(vm => vm.IsMyAddressesTab)
                .Subscribe(value =>
                {
                    if (IsMyAddressesTab)
                    {
                        ToolbarIcon = "ic_search";
                        ToolbarCommand = SearchCommand;
                    }
                    else
                    {
                        ToolbarIcon = "ic_qr";
                        ToolbarCommand = ScanCommand;
                    }
                });

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
            IsMyAddressesTab = false;
            AddressSettingType = SettingType.Init;

            var addresses = AddressesHelper
                .GetReceivingAddressesAsync(
                  account: account,
                  currency: currency,
                  tokenContract: tokenContract)
                .WaitForResult()
                .Where(address => !useToSelectFrom || address.Balance != 0)
                .OrderByDescending(address => address.Balance);

            MyAddresses = new ObservableCollection<WalletAddressViewModel>(addresses);

            InitialMyAddresses = new ObservableCollection<WalletAddressViewModel>(addresses);
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
                ConfirmAction?.Invoke(ToAddress, 0m);
            }));

        private ReactiveCommand<WalletAddressViewModel, Unit> _selectAddressCommand;
        public ReactiveCommand<WalletAddressViewModel, Unit> SelectAddressCommand => _selectAddressCommand ??=
            (_selectAddressCommand = ReactiveCommand.Create<WalletAddressViewModel>(SelectAddress));

        private ReactiveCommand<Unit, Unit> _pasteCommand;
        public ReactiveCommand<Unit, Unit> PasteCommand =>
            _pasteCommand ??= (_pasteCommand = ReactiveCommand.CreateFromTask(OnPasteButtonClicked));

        private ReactiveCommand<WalletAddressViewModel, Unit> _copyCommand;
        public ReactiveCommand<WalletAddressViewModel, Unit> CopyCommand =>
            _copyCommand ??= (_copyCommand = ReactiveCommand.CreateFromTask<WalletAddressViewModel>(OnCopyButtonClicked));

        private ReactiveCommand<Unit, Unit> _scanCommand;
        public ReactiveCommand<Unit, Unit> ScanCommand =>
            _scanCommand ??= (_scanCommand = ReactiveCommand.CreateFromTask(OnScanButtonClicked));

        private ReactiveCommand<Unit, Unit> _searchCommand;
        public ReactiveCommand<Unit, Unit> SearchCommand =>
            _searchCommand ??= (_searchCommand = ReactiveCommand.CreateFromTask(OnSearchButtonClicked));

        private ReactiveCommand<Unit, Unit> _clearToAddressCommand;
        public ReactiveCommand<Unit, Unit> ClearToAddressCommand =>
            _clearToAddressCommand ??= (_clearToAddressCommand = ReactiveCommand.Create(() =>
            {
                ToAddress = string.Empty;
            }));

        private ReactiveCommand<Unit, Unit> _clearSearchAddressCommand;
        public ReactiveCommand<Unit, Unit> ClearSearchAddressCommand =>
            _clearSearchAddressCommand ??= (_clearSearchAddressCommand = ReactiveCommand.Create(() =>
            {
                SearchPattern = string.Empty;
            }));

        private ReactiveCommand<bool, Unit> _changeAddressesTabCommand;
        public ReactiveCommand<bool, Unit> ChangeAddressesTabCommand =>
            _changeAddressesTabCommand ??= (_changeAddressesTabCommand = ReactiveCommand.Create<bool>((value) =>
            {
                IsMyAddressesTab = value;
            }));

        private ICommand _scanResultCommand;
        public ICommand ScanResultCommand =>
            _scanResultCommand ??= new Command(async () => await OnScanResult());

        private ICommand _backCommand;
        public ICommand BackCommand => _backCommand ??= new Command(() =>
            {
                SearchPattern = string.Empty;
                if (AddressSettingType == SettingType.InitFromSearch)
                    AddressSettingType = SettingType.Init;
                if (AddressSettingType == SettingType.ChangeFromSearch)
                    AddressSettingType = SettingType.Change;
            });

        protected async void OnCopyClicked(string value)
        {
            if (!string.IsNullOrEmpty(value))
            {
                await Clipboard.SetTextAsync(value);
                ToastService?.Show(AppResources.AddressCopied, ToastPosition.Top, Application.Current.RequestedTheme.ToString());
            }
            else
            {
                await Application.Current.MainPage.DisplayAlert(AppResources.Error, AppResources.CopyError, AppResources.AcceptButton);
            }
        }

        private void SelectAddress(WalletAddressViewModel address)
        {
            SelectedAddress = address;
            var selectedAddress = SelectedAddress?.WalletAddress?.Address;
            var balance = SelectedAddress?.WalletAddress?.AvailableBalance();

            ConfirmAction?.Invoke(selectedAddress, balance ?? 0m);
        }

        private async Task OnScanResult()
        {
            IsScanning = false;
            IsAnalyzing = false;
            this.RaisePropertyChanged(nameof(IsScanning));
            this.RaisePropertyChanged(nameof(IsAnalyzing));

            if (ScanResult == null)
            {
                await Application.Current.MainPage.DisplayAlert(AppResources.Error, "Incorrect QR code format", AppResources.AcceptButton);
                Device.BeginInvokeOnMainThread(async () =>
                {
                    await Navigation.PopAsync();
                });

                return;
            }

            Device.BeginInvokeOnMainThread(async () =>
            {
                int indexOfChar = ScanResult.Text.IndexOf(':');
                if (indexOfChar == -1)
                    ToAddress = ScanResult.Text;
                else
                    ToAddress = ScanResult.Text.Substring(indexOfChar + 1);

                await Navigation.PopAsync();

                ConfirmAction?.Invoke(ToAddress, 0m);
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

            await Navigation.PushAsync(new ScanningQrPage(this));
        }

        private async Task OnSearchButtonClicked()
        {
            if (AddressSettingType == SettingType.Init)
                AddressSettingType = SettingType.InitFromSearch;
            if (AddressSettingType == SettingType.Change)
                AddressSettingType = SettingType.ChangeFromSearch;

            await Navigation.PushAsync(new SearchAddressPage(this));
        }

        private async Task OnPasteButtonClicked()
        {
            if (Clipboard.HasText)
            {
                var text = await Clipboard.GetTextAsync();
                ToAddress = text;
            }
            else
            {
                await Application.Current.MainPage.DisplayAlert(AppResources.Error, AppResources.EmptyClipboard, AppResources.AcceptButton);
            }
        }

        private async Task OnCopyButtonClicked(WalletAddressViewModel address)
        {
            if (address != null)
            {
                await Clipboard.SetTextAsync(address.Address);
                ToastService?.Show(AppResources.AddressCopied, ToastPosition.Top, Application.Current.RequestedTheme.ToString());
            }
            else
            {
                await Application.Current.MainPage.DisplayAlert(AppResources.Error, AppResources.CopyError, AppResources.AcceptButton);
            }
        }
    }
}
