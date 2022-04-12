using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Reactive;
using System.Threading.Tasks;
using System.Windows.Input;
using atomex.Common;
using atomex.Resources;
using atomex.Services;
using atomex.Views;
using Atomex;
using Atomex.Blockchain.Tezos.Internal;
using Atomex.Common;
using Atomex.Core;
using Atomex.Cryptography;
using Atomex.Wallet;
using Atomex.Wallet.Tezos;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Rg.Plugins.Popup.Services;
using Serilog;
using Xamarin.Essentials;
using Xamarin.Forms;

namespace atomex.ViewModel
{
    public class AddressViewModel : BaseViewModel
    {
        private readonly IAtomexApp _app;
        private CurrencyConfig _currency;
        private IToastService _toastService;

        [Reactive] public string Address { get; set; }
        [Reactive] public string Type { get; set; }
        [Reactive] public string Path { get; set; }
        [Reactive] public string Balance { get; set; }
        [Reactive] public string TokenBalance { get; set; }

        [Reactive] public bool IsUpdating { get; set; }
        [Reactive] public bool IsCopied { get; set; }
        [Reactive] public bool ExportKeyConfirm { get; set; }
        [Reactive] public string CopyButtonName { get; set; }

        public Func<string, Task> UpdateAddress { get; set; }

        public AddressViewModel(IAtomexApp app, CurrencyConfig currency)
        {
            _app = app ?? throw new ArgumentNullException(nameof(app));
            _currency = currency ?? throw new ArgumentNullException(nameof(_currency));
            _toastService = DependencyService.Get<IToastService>();
            CopyButtonName = AppResources.CopyKeyButton;
        }

        private ReactiveCommand<string, Unit> _copyCommand;
        public ReactiveCommand<string, Unit> CopyCommand => _copyCommand ??= ReactiveCommand.Create<string>((s) =>
        {
            try
            {
                Clipboard.SetTextAsync(Address);
                _toastService?.Show(AppResources.AddressCopied, ToastPosition.Top, Application.Current.RequestedTheme.ToString());
            }
            catch (Exception)
            {
                Application.Current.MainPage.DisplayAlert(AppResources.Error, AppResources.CopyError, AppResources.AcceptButton);
            }
        });

        private ReactiveCommand<string, Unit> _exportKeyCommand;
        public ReactiveCommand<string, Unit> ExportKeyCommand => _exportKeyCommand ??= ReactiveCommand.CreateFromTask<string>(async (s) =>
        {
            try
            {
                if (!ExportKeyConfirm)
                {
                    ExportKeyConfirm = true;
                    _ = PopupNavigation.Instance.PushAsync(new ExportKeyBottomSheet(this));
                    return;
                }

                IsCopied = true;
                CopyButtonName = AppResources.Copied;

                var walletAddress = await _app.Account
                    .GetAddressAsync(_currency.Name, Address);

                var hdWallet = _app.Account.Wallet as HdWallet;

                using var privateKey = hdWallet.KeyStorage.GetPrivateKey(
                    currency: _currency,
                    keyIndex: walletAddress.KeyIndex,
                    keyType: walletAddress.KeyType);

                using var unsecuredPrivateKey = privateKey.ToUnsecuredBytes();

                if (Currencies.IsBitcoinBased(_currency.Name))
                {
                    var btcBasedConfig = _currency as BitcoinBasedConfig;

                    var wif = new NBitcoin.Key(unsecuredPrivateKey)
                        .GetWif(btcBasedConfig.Network)
                        .ToWif();

                    await Clipboard.SetTextAsync(wif);
                }
                else if (Currencies.IsTezosBased(_currency.Name))
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

                await Task.Delay(1500);

                IsCopied = false;
                CopyButtonName = AppResources.CopyKeyButton;

                ExportKeyConfirm = false;
                if (PopupNavigation.Instance.PopupStack.Count > 0)
                    _ = PopupNavigation.Instance.PopAsync();

            }
            catch (Exception e)
            {
                Log.Error(e, "Private key export error");
            }
        });

        private ReactiveCommand<string, Unit> _updateAddressCommand;
        public ReactiveCommand<string, Unit> UpdateAddressCommand => _updateAddressCommand ??= ReactiveCommand.Create<string>(async (s) =>
        {
            if (UpdateAddress == null)
                return;

            IsUpdating = true;
            await UpdateAddress(Address);
            IsUpdating = false;
        });

        private ICommand _closeBottomSheetCommand;
        public ICommand CloseBottomSheetCommand => _closeBottomSheetCommand ??= ReactiveCommand.Create(() =>
        {
            ExportKeyConfirm = false;

            if (PopupNavigation.Instance.PopupStack.Count > 0)
                _ = PopupNavigation.Instance.PopAsync();
        });
    }

    public class AddressesViewModel : BaseViewModel
    {
        private readonly IAtomexApp _app;
        private CurrencyConfig _currency { get; set; }

        private readonly string _tokenContract;
        [Reactive] public ObservableCollection<AddressViewModel> Addresses { get; set; }
        [Reactive] public bool HasTokens { get; set; }

        private IToastService _toastService;
        public INavigation Navigation { get; set; }

        [Reactive] public AddressViewModel SelectedAddress { get; set; }

        public AddressesViewModel(IAtomexApp app, CurrencyConfig currency, string tokenContract = null)
        {
            _app = app ?? throw new ArgumentNullException(nameof(app));
            _currency = currency ?? throw new ArgumentNullException(nameof(_currency));
            _toastService = DependencyService.Get<IToastService>();
            _tokenContract = tokenContract;

            this.WhenAnyValue(vm => vm.SelectedAddress)
                .WhereNotNull()
                .SubscribeInMainThread(a =>
                {
                    Navigation?.PushAsync(new AddressInfoPage(SelectedAddress));
                    SelectedAddress = null;
                });

            _ = ReloadAddresses();
            SubscribeToServices();
        }

        private void SubscribeToServices()
        {
            _app.Account.BalanceUpdated += OnBalanceUpdatedEventHandler;
        }

        private async void OnBalanceUpdatedEventHandler(object sender, CurrencyEventArgs args)
        {
            try
            {
                if (_currency.Name != args.Currency) return;
                await ReloadAddresses();
            }
            catch (Exception e)
            {
                Log.Error(e, "Reload addresses event handler error");
            }
        }

        public async Task ReloadAddresses()
        {
            try
            {
                var account = _app.Account
                    .GetCurrencyAccount(_currency.Name);

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

                Addresses = new ObservableCollection<AddressViewModel>(
                    addresses.Select(a =>
                    {
                        var path = a.KeyType == CurrencyConfig.StandardKey && Currencies.IsTezosBased(_currency.Name)
                            ? $"m/44'/{_currency.Bip44Code}'/{a.KeyIndex.Account}'/{a.KeyIndex.Chain}'"
                            : $"m/44'/{_currency.Bip44Code}'/{a.KeyIndex.Account}'/{a.KeyIndex.Chain}/{a.KeyIndex.Index}";

                        return new AddressViewModel(_app, _currency)
                        {
                            Address = a.Address,
                            Type = KeyTypeToString(a.KeyType),
                            Path = path,
                            Balance = $"{a.Balance.ToString(CultureInfo.InvariantCulture)} {_currency.Name}",
                            UpdateAddress = UpdateAddress
                        };
                    }));

                if (_currency.Name == TezosConfig.Xtz && _tokenContract != null)
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

        private async Task UpdateAddress(string address)
        {
            try
            {
                await new HdWalletScanner(_app.Account)
                    .ScanAddressAsync(_currency.Name, address);

                if (_currency.Name == TezosConfig.Xtz && _tokenContract != null)
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

                await ReloadAddresses();

                await Device.InvokeOnMainThreadAsync(() =>
                { 
                    _toastService?.Show(AppResources.AddressLabel + " " + AppResources.HasBeenUpdated, ToastPosition.Top, Application.Current.RequestedTheme.ToString());
                });
            }
            catch (OperationCanceledException)
            {
                Log.Debug("Address balance update operation canceled");
            }
            catch (Exception e)
            {
                Log.Error(e, "UpdateAddress error");
                // todo: message to user!?
            }
        }
    }
}

