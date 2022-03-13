using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using atomex.Resources;
using atomex.Services;
using Atomex;
using Atomex.Blockchain.Tezos.Internal;
using Atomex.Common;
using Atomex.Core;
using Atomex.Cryptography;
using Atomex.Wallet;
using Atomex.Wallet.Tezos;
using Serilog;
using Xamarin.Essentials;
using Xamarin.Forms;

namespace atomex.ViewModel
{
    public class AddressInfo : BaseViewModel
    {
        public string Address { get; set; }
        public string Type { get; set; }
        public string Path { get; set; }

        private string _balance;
        public string Balance
        {
            get => _balance;
            set
            {
                _balance = value;
                OnPropertyChanged(nameof(Balance));
            }
        }

        public string TokenBalance { get; set; }

        public Action<string> CopyToClipboard { get; set; }
        public Action<string> ExportKey { get; set; }
        public Action<string> UpdateAddress { get; set; }

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
            UpdateAddress?.Invoke(Address);
        });
    }

    public class AddressesViewModel : BaseViewModel
    {
        private readonly IAtomexApp _app;

        public CurrencyConfig Currency { get; set; }

        private CancellationTokenSource _cancellation;

        private bool _isUpdating;
        public bool IsUpdating
        {
            get => _isUpdating;
            set
            {
                _isUpdating = value;
                OnPropertyChanged(nameof(IsUpdating));
            }
        }

        private readonly string _tokenContract;

        public ObservableCollection<AddressInfo> Addresses { get; set; }

        public bool HasTokens { get; set; }

        private IToastService ToastService;

        public INavigation Navigation { get; set; }

        private AddressInfo _selectedAddress;
        public AddressInfo SelectedAddress
        {
            get => _selectedAddress;
            set
            {
                if (value == null) return;
                _selectedAddress = value;

                Navigation.PushAsync(new AddressInfoPage(this));
            }
        }

        public AddressesViewModel(IAtomexApp app, CurrencyConfig currency, INavigation navigation, string tokenContract = null)
        {
            _app = app ?? throw new ArgumentNullException(nameof(app));
            Currency = currency ?? throw new ArgumentNullException(nameof(Currency));
            Navigation = navigation ?? throw new ArgumentNullException(nameof(navigation));
            ToastService = DependencyService.Get<IToastService>();
            _tokenContract = tokenContract;

            ReloadAddresses();
        }

        public async void ReloadAddresses()
        {
            try
            {
                var account = _app.Account
                    .GetCurrencyAccount(Currency.Name);

                var addresses = (await account
                    .GetAddressesAsync())
                    .ToList();

                addresses.Sort((a1, a2) =>
                {
                    var typeResult = a1.KeyType.CompareTo(a2.KeyType);

                    if (typeResult != 0)
                        return typeResult;

                    var accountResult = a1.KeyIndex.Account.CompareTo(a2.KeyIndex.Account);

                    if (accountResult != 0)
                        return accountResult;

                    var chainResult = a1.KeyIndex.Chain.CompareTo(a2.KeyIndex.Chain);

                    return chainResult != 0
                       ? chainResult
                       : a1.KeyIndex.Index.CompareTo(a2.KeyIndex.Index);
                });

                Addresses = new ObservableCollection<AddressInfo>(
                    addresses.Select(a =>
                    {
                        var path = a.KeyType == CurrencyConfig.StandardKey && Currencies.IsTezosBased(Currency.Name)
                            ? $"m/44'/{Currency.Bip44Code}'/{a.KeyIndex.Account}'/{a.KeyIndex.Chain}'"
                            : $"m/44'/{Currency.Bip44Code}'/{a.KeyIndex.Account}'/{a.KeyIndex.Chain}/{a.KeyIndex.Index}";

                        return new AddressInfo
                        {
                            Address = a.Address,
                            Type = KeyTypeToString(a.KeyType),
                            Path = path,
                            Balance = $"{a.Balance.ToString(CultureInfo.InvariantCulture)} {Currency.Name}",
                            CopyToClipboard = OnCopyAddressButtonClicked,
                            ExportKey = OnExportKeyButtonClicked,
                            UpdateAddress = OnUpdateButtonClicked
                        };
                    }));

                if (Currency.Name == TezosConfig.Xtz && _tokenContract != null)
                {
                    HasTokens = true;

                    var tezosAccount = account as TezosAccount;

                    var addressesWithTokens = (await tezosAccount
                        .DataRepository
                        .GetTezosTokenAddressesByContractAsync(_tokenContract))
                        .Where(w => w.Balance != 0)
                        .GroupBy(w => w.Address);

                    foreach (var addressWithTokens in addressesWithTokens)
                    {
                        var addressInfo = Addresses.FirstOrDefault(a => a.Address == addressWithTokens.Key);

                        if (addressInfo == null)
                            continue;

                        if (addressWithTokens.Count() == 1)
                        {
                            var tokenAddress = addressWithTokens.First();

                            addressInfo.TokenBalance = tokenAddress.Balance.ToString("F8", CultureInfo.InvariantCulture);

                            var tokenCode = tokenAddress?.TokenBalance?.Symbol;

                            if (tokenCode != null)
                                addressInfo.TokenBalance += $" {tokenCode}";
                        }
                        else
                        {
                            addressInfo.TokenBalance = $"{addressWithTokens.Count()} TOKENS";
                        }
                    }
                }

                OnPropertyChanged(nameof(Addresses));
                OnPropertyChanged(nameof(HasTokens));
            }
            catch (Exception e)
            {
                Log.Error(e, "Error while reload addresses list.");
            }
        }

        private string KeyTypeToString(int keyType) =>
            keyType switch
            {
                CurrencyConfig.StandardKey => "Standard",
                TezosConfig.Bip32Ed25519Key => "Atomex",
                _ => throw new NotSupportedException($"Key type {keyType} not supported.")
            };

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

                    using var privateKey = hdWallet.KeyStorage.GetPrivateKey(
                        currency: Currency,
                        keyIndex: walletAddress.KeyIndex,
                        keyType: walletAddress.KeyType);

                    using var unsecuredPrivateKey = privateKey.ToUnsecuredBytes();

                    if (Currencies.IsBitcoinBased(Currency.Name))
                    {
                        var btcBasedConfig = Currency as BitcoinBasedConfig;

                        var wif = new NBitcoin.Key(unsecuredPrivateKey)
                            .GetWif(btcBasedConfig.Network)
                            .ToWif();

                        await Clipboard.SetTextAsync(wif);
                    }
                    else if (Currencies.IsTezosBased(Currency.Name))
                    {
                        var base58 = unsecuredPrivateKey.Length == 32
                            ? Base58Check.Encode(unsecuredPrivateKey, Prefix.Edsk)
                            : Base58Check.Encode(unsecuredPrivateKey, Prefix.EdskSecretKey);

                        await Clipboard.SetTextAsync(base58);
                    }
                    else
                    {
                        var hex = Hex.ToHexString(unsecuredPrivateKey.Data);

                        await Clipboard.SetTextAsync(hex);
                    }

                    await Device.InvokeOnMainThreadAsync(() =>
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
            IsUpdating = true;

            try
            {
                await new HdWalletScanner(_app.Account)
                    .ScanAddressAsync(Currency.Name, address);

                if (Currency.Name == TezosConfig.Xtz && _tokenContract != null)
                {
                    // update tezos token balance
                    var tezosAccount = _app.Account
                        .GetCurrencyAccount<TezosAccount>(TezosConfig.Xtz);

                    await new TezosTokensScanner(tezosAccount)
                        .ScanContractAsync(address, _tokenContract);

                    // reload balances for all tezos tokens account
                    foreach (var currency in _app.Account.Currencies)
                        if (Currencies.IsTezosToken(currency.Name))
                            _app.Account
                                .GetCurrencyAccount<TezosTokenAccount>(currency.Name)
                                .ReloadBalances();
                }

                ReloadAddresses();

                await Device.InvokeOnMainThreadAsync(() =>
                { 
                    ToastService?.Show(AppResources.AddressLabel + " " + AppResources.HasBeenUpdated, ToastPosition.Top, Application.Current.RequestedTheme.ToString());
                });
            }
            catch (OperationCanceledException)
            {
                Log.Debug("Address balance update operation canceled");
            }
            catch (Exception e)
            {
                Log.Error(e, "AddressesViewModel.OnUpdateButtonClicked");
                // todo: message to user!?
            }
            IsUpdating = false;
        }
    }
}

