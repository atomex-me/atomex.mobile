using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using atomex.Resources;
using atomex.Services;
using Atomex;
using Atomex.Common;
using Atomex.Core;
using Atomex.Wallet;
using Serilog;
using Xamarin.Essentials;
using Xamarin.Forms;

namespace atomex.ViewModel
{
    public class AddressInfo : BaseViewModel
    {
        public string Address { get; set; }
        public string Path { get; set; }
        public decimal Balance { get; set; }
        public Action<string> CopyToClipboard { get; set; }
        public Action<string> ExportKey { get; set; }
        public Action<string> UpdateAddress { get; set; }

        private bool _isUpdating;

        public bool IsUpdating
        {
            get => _isUpdating;
            set
            {
                _isUpdating = value;

                if (_isUpdating)
                    Opacity = 0.3f;
                else
                    Opacity = 1f;

                OnPropertyChanged(nameof(IsUpdating));
            }
        }

        private float _opacity = 1f;
        public float Opacity
        {
            get => _opacity;
            set { _opacity = value; OnPropertyChanged(nameof(Opacity)); }
        }

        private ICommand _copyCommand;
        public ICommand CopyCommand => _copyCommand ??= new Command<string>( (s) =>
        {
            CopyToClipboard?.Invoke(Address);
        });

        private ICommand _exportKeyCommand;
        public ICommand ExportKeyCommand => _exportKeyCommand ??= new Command<string>((s) =>
        {
            ExportKey?.Invoke(Address);
        });


        private ICommand _updateAddressCommand;
        public ICommand UpdateAddressCommand => _updateAddressCommand ??= new Command<string>((s) =>
        {
            if (IsUpdating) return;
            IsUpdating = true;
            UpdateAddress?.Invoke(Address);
        });

        private ICommand _addressUpdatedCommand;
        public ICommand AddressUpdatedCommand => _addressUpdatedCommand ??= new Command<decimal>((value) =>
        {
            Balance = value;
            IsUpdating = false;
        });
    }

    public class AddressesViewModel : BaseViewModel
    {
        private readonly IAtomexApp _app;

        public CurrencyConfig Currency {get; set;}

        public List<AddressInfo> Addresses { get; set; }

        private IToastService ToastService;

        public INavigation Navigation { get; set; }

        public AddressInfo SelectedAddress { get; set; }

        public AddressesViewModel(IAtomexApp app, CurrencyConfig currency, INavigation navigation)
        {
            _app = app ?? throw new ArgumentNullException(nameof(app));
            Currency = currency ?? throw new ArgumentNullException(nameof(currency));
            Navigation = navigation ?? throw new ArgumentNullException(nameof(navigation));
            ToastService = DependencyService.Get<IToastService>();

            _ = Load();
        }

        public async Task Load()
        {
            try
            {
                var account = _app.Account.GetCurrencyAccount(Currency.Name);

                var addresses = (await account.GetAddressesAsync())
                    .ToList();

                addresses.Sort((a1, a2) =>
                {
                    var chainResult = a1.KeyIndex.Chain.CompareTo(a2.KeyIndex.Chain);

                    return chainResult == 0
                        ? a1.KeyIndex.Index.CompareTo(a2.KeyIndex.Index)
                        : chainResult;
                });

                Addresses = addresses.Select(a => new AddressInfo
                {
                    Address = a.Address,
                    Path = $"m/44'/{Currency.Bip44Code}/0'/{a.KeyIndex.Chain}/{a.KeyIndex.Index}",
                    Balance = a.Balance,
                    CopyToClipboard = OnCopyAddressButtonClicked,
                    ExportKey = OnExportKeyButtonClicked,
                    UpdateAddress = OnUpdateButtonClicked
                }).ToList();

                OnPropertyChanged(nameof(Addresses));
            }
            catch (Exception e)
            {
                Log.Error(e, "Error while load addresses.");
            }
        }

        private ICommand _selectAddressCommand;
        public ICommand SelectAddressCommand => _selectAddressCommand ??= new Command<AddressInfo>(async (item) => await OnAddressItemTapped(item));

        async Task OnAddressItemTapped(AddressInfo address)
        {
            if (address != null)
            {
                SelectedAddress = address;
                await Navigation.PushAsync(new AddressInfoPage(this));
            }
        }

        private async void OnCopyAddressButtonClicked(string address)
        {
            try
            {
                await Clipboard.SetTextAsync(address);
                ToastService?.Show(AppResources.AddressCopied, ToastPosition.Top, Application.Current.RequestedTheme.ToString());
            }
            catch (Exception)
            {
                await Application.Current.MainPage.DisplayAlert(AppResources.Error, AppResources.CopyError, AppResources.AcceptButton);
            }
        }

        private async void OnExportKeyButtonClicked(string address)
        {
            try
            {
                string message = string.Format(
                  CultureInfo.InvariantCulture,
                  AppResources.CopyingPrivateKeyWarning,
                  address);

                var confirm = await Application.Current.MainPage.DisplayAlert(
                    string.Empty,
                    message,
                    AppResources.CopyButton,
                    AppResources.CancelButton);

                if (confirm)
                {
                    var walletAddress = await _app.Account
                        .GetAddressAsync(Currency.Name, address);

                    var hdWallet = _app.Account.Wallet as HdWallet;

                    using var privateKey = hdWallet.KeyStorage
                        .GetPrivateKey(Currency, walletAddress.KeyIndex);

                    using var unsecuredPrivateKey = privateKey.ToUnsecuredBytes();

                    var hex = Hex.ToHexString(unsecuredPrivateKey.Data);

                    await Clipboard.SetTextAsync(hex);

                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        ToastService?.Show(AppResources.PrivateKeyCopied, ToastPosition.Top, Application.Current.RequestedTheme.ToString());
                    });
                }
            }
            catch (Exception e)
            {
                Log.Error(e, "Private key export error");
            }
        }


        private async void OnUpdateButtonClicked(string address)
        {
            try
            {
                await new HdWalletScanner(_app.Account)
                    .ScanAddressAsync(Currency.Name, address)
                    .ConfigureAwait(false);

                var balance = await _app.Account
                    .GetAddressBalanceAsync(Currency.Name, address)
                    .ConfigureAwait(false);

                var currentAddress = Addresses.FirstOrDefault(a => a.Address == address);
                currentAddress!.AddressUpdatedCommand.Execute(balance.Available);

                MainThread.BeginInvokeOnMainThread(() =>
                {
                    ToastService?.Show(AppResources.AddressLabel + " " + AppResources.HasBeenUpdated, ToastPosition.Top, Application.Current.RequestedTheme.ToString());
                });
            }
            catch (Exception e)
            {
                Log.Error(e, "Update address error");
            }
        }
    }
}

