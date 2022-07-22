using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Threading.Tasks;
using System.Windows.Input;
using atomex.Common;
using atomex.Resources;
using atomex.Views;
using Atomex;
using Atomex.Blockchain.Tezos;
using Atomex.Blockchain.Tezos.Internal;
using Atomex.Common;
using Atomex.Core;
using Atomex.Cryptography;
using Atomex.ViewModels;
using Atomex.Wallet;
using Atomex.Wallet.Tezos;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Serilog;
using Xamarin.Essentials;
using Xamarin.Forms;
using static atomex.Models.SnackbarMessage;

namespace atomex.ViewModels
{
    public class AddressViewModel : BaseViewModel
    {
        private readonly IAtomexApp _app;
        private CurrencyConfig _currency;
        private INavigationService _navigationService;

        [Reactive] public WalletAddress WalletAddress { get; set; }
        public string Address => WalletAddress.Address;
        public string Type => KeyTypeToString(WalletAddress.KeyType);
        [Reactive] public string Path { get; set; }
        [Reactive] public bool HasTokens { get; set; }
        [Reactive] public decimal Balance { get; set; }
        [Reactive] public string BalanceCode { get; set; }
        [Reactive] public TokenBalance TokenBalance { get; set; }

        public string TokenBalanceString =>
            $"{TokenBalance?.GetTokenBalance() ?? 0} {TokenBalance?.Symbol ?? "TOKENS"}";

        [Reactive] public bool IsUpdating { get; set; }
        [Reactive] public bool IsCopied { get; set; }
        [Reactive] public bool ExportKeyConfirm { get; set; }
        [Reactive] public string CopyButtonName { get; set; }
        [Reactive] public bool IsFreeAddress { get; set; }

        public Func<string, Task<AddressViewModel>> UpdateAddress { get; set; }

        public AddressViewModel()
        {
        }

        public AddressViewModel(
            IAtomexApp app,
            CurrencyConfig currency,
            INavigationService navigationService)
        {
            _app = app ?? throw new ArgumentNullException(nameof(_app));
            _currency = currency ?? throw new ArgumentNullException(nameof(_currency));
            _navigationService = navigationService ?? throw new ArgumentNullException(nameof(_navigationService));
            CopyButtonName = AppResources.CopyKeyButton;
        }

        private string KeyTypeToString(int keyType) =>
            keyType switch
            {
                CurrencyConfig.StandardKey => "Standard",
                TezosConfig.Bip32Ed25519Key => "Atomex",
                _ => throw new NotSupportedException($"Key type {keyType} not supported.")
            };

        private ReactiveCommand<string, Unit> _copyCommand;

        public ReactiveCommand<string, Unit> CopyCommand => _copyCommand ??= ReactiveCommand.Create<string>((s) =>
        {
            try
            {
                Clipboard.SetTextAsync(Address);
                Device.InvokeOnMainThreadAsync(() =>
                {
                    _navigationService?.DisplaySnackBar(MessageType.Regular, AppResources.AddressCopied);
                });
            }
            catch (Exception)
            {
                _navigationService?.ShowAlert(AppResources.Error, AppResources.CopyError, AppResources.AcceptButton);
            }
        });

        private ReactiveCommand<string, Unit> _exportKeyCommand;

        public ReactiveCommand<string, Unit> ExportKeyCommand => _exportKeyCommand ??=
            ReactiveCommand.CreateFromTask<string>(async (s) =>
            {
                try
                {
                    if (!ExportKeyConfirm)
                    {
                        ExportKeyConfirm = true;
                        _navigationService?.ShowBottomSheet(new ExportKeyBottomSheet(this));
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

                    var unsecuredPrivateKey = privateKey.ToUnsecuredBytes();

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
                        var hex = Hex.ToHexString(unsecuredPrivateKey);
                        ;

                        await Clipboard.SetTextAsync(hex);
                    }

                    await Task.Delay(1500);

                    IsCopied = false;
                    CopyButtonName = AppResources.CopyKeyButton;
                    ExportKeyConfirm = false;

                    _navigationService?.CloseBottomSheet();
                }
                catch (Exception e)
                {
                    Log.Error(e, "Private key export error");
                }
            });

        private ReactiveCommand<string, Unit> _updateAddressCommand;

        public ReactiveCommand<string, Unit> UpdateAddressCommand => _updateAddressCommand ??=
            ReactiveCommand.Create<string>(async (s) =>
            {
                try
                {
                    if (UpdateAddress == null) return;

                    IsUpdating = true;
                    var updatedViewModel = await UpdateAddress(Address);

                    if (updatedViewModel == null) return;

                    await Device.InvokeOnMainThreadAsync(() =>
                    {
                        Balance = updatedViewModel.Balance;
                        TokenBalance = updatedViewModel?.TokenBalance;
                    });
                }
                catch (Exception e)
                {
                    Log.Error("Update address error", e);
                }
                finally
                {
                    IsUpdating = false;
                }
            });

        private ICommand _closeBottomSheetCommand;

        public ICommand CloseBottomSheetCommand => _closeBottomSheetCommand ??= ReactiveCommand.Create(() =>
        {
            ExportKeyConfirm = false;
            _navigationService?.CloseBottomSheet();
        });
    }

    public class AddressesViewModel : BaseViewModel
    {
        private readonly IAtomexApp _app;
        private CurrencyConfig _currency { get; set; }
        private INavigationService _navigationService { get; }

        private readonly string _tokenContract;
        private readonly decimal _tokenId;
        public bool HasTokens => _currency.Name == TezosConfig.Xtz && _tokenContract != null;
        [Reactive] public ObservableCollection<AddressViewModel> Addresses { get; set; }
        [Reactive] public AddressViewModel SelectedAddress { get; set; }

        public const int MinimalAddressUpdateTimeMs = 1000;

        public Action AddressesChanged;

        public AddressesViewModel(
            IAtomexApp app,
            CurrencyConfig currency,
            INavigationService navigationService,
            string tokenContract = null,
            decimal tokenId = 0)
        {
            _app = app ?? throw new ArgumentNullException(nameof(_app));
            _currency = currency ?? throw new ArgumentNullException(nameof(_currency));
            _navigationService = navigationService ?? throw new ArgumentNullException(nameof(_navigationService));
            _tokenContract = tokenContract;
            _tokenId = tokenId;

            this.WhenAnyValue(vm => vm.SelectedAddress)
                .WhereNotNull()
                .SubscribeInMainThread(a =>
                {
                    _navigationService?.ShowPage(new AddressInfoPage(SelectedAddress), TabNavigation.Portfolio);
                    SelectedAddress = null;
                });

            _ = ReloadAddresses();
            SubscribeToServices();
        }

        private ReactiveCommand<Unit, Unit> _showAllAddressesCommand;

        public ReactiveCommand<Unit, Unit> ShowAllAddressesCommand => _showAllAddressesCommand ??=
            ReactiveCommand.Create(() =>
            {
                var selectAddressViewModel = new SelectAddressViewModel(
                    account: _app.Account,
                    currency: _currency,
                    navigationService: _navigationService,
                    tokenContract: _tokenContract,
                    selectedTokenId: (int) _tokenId,
                    tab: TabNavigation.Portfolio,
                    mode: SelectAddressMode.ChooseMyAddress)
                {
                    ConfirmAction = (selectAddressViewModel, walletAddressViewModel) =>
                    {
                        var address = Addresses?
                            .Where(a => a.Address == walletAddressViewModel?.Address)
                            .FirstOrDefault();
                        _navigationService?.ShowPage(new AddressInfoPage(address), TabNavigation.Portfolio);

                        if (selectAddressViewModel.SelectAddressFrom == SelectAddressFrom.InitSearch)
                            _navigationService?.RemovePreviousPage(TabNavigation.Portfolio);
                    }
                };

                _navigationService?.ShowPage(new SelectAddressPage(selectAddressViewModel), TabNavigation.Portfolio);
            });

        private void SubscribeToServices()
        {
            _app.Account.BalanceUpdated += OnBalanceUpdatedEventHandler;
        }

        private async void OnBalanceUpdatedEventHandler(object sender, CurrencyEventArgs args)
        {
            try
            {
                if ((args.Currency != null && _currency.Name == args.Currency) ||
                    (args.IsTokenUpdate && (args.TokenContract == null ||
                                            (args.TokenContract == _tokenContract && args.TokenId == _tokenId))))
                {
                    await ReloadAddresses();
                }
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
                var addresses = AddressesHelper
                    .GetReceivingAddressesAsync(
                        account: _app.Account,
                        currency: _currency,
                        tokenContract: _tokenContract,
                        tokenId: (int) _tokenId)
                    .WaitForResult()
                    .OrderByDescending(address => address.Balance)
                    .ToList();

                addresses.Sort((a1, a2) =>
                {
                    var typeResult = a1.WalletAddress.KeyType.CompareTo(a2.WalletAddress.KeyType);

                    if (typeResult != 0)
                        return typeResult;

                    var accountResult = a1.WalletAddress.KeyIndex.Account.CompareTo(a2.WalletAddress.KeyIndex.Account);

                    if (accountResult != 0)
                        return accountResult;

                    var chainResult = a1.WalletAddress.KeyIndex.Chain.CompareTo(a2.WalletAddress.KeyIndex.Chain);

                    return chainResult != 0
                        ? chainResult
                        : a1.WalletAddress.KeyIndex.Index.CompareTo(a2.WalletAddress.KeyIndex.Index);
                });

                var freeAddress = _app.Account
                    .GetFreeExternalAddressAsync(_currency?.Name)
                    .WaitForResult();

                await Device.InvokeOnMainThreadAsync(() =>
                {
                    Addresses = new ObservableCollection<AddressViewModel>(
                        addresses.Select(a =>
                        {
                            var path = a.WalletAddress.KeyType == CurrencyConfig.StandardKey &&
                                       Currencies.IsTezosBased(_currency.Name)
                                ? $"m/44'/{_currency.Bip44Code}'/{a.WalletAddress.KeyIndex.Account}'/{a.WalletAddress.KeyIndex.Chain}'"
                                : $"m/44'/{_currency.Bip44Code}'/{a.WalletAddress.KeyIndex.Account}'/{a.WalletAddress.KeyIndex.Chain}/{a.WalletAddress.KeyIndex.Index}";

                            return new AddressViewModel(_app, _currency, _navigationService)
                            {
                                WalletAddress = a.WalletAddress,
                                Path = path,
                                HasTokens = HasTokens,
                                IsFreeAddress = a?.Address == freeAddress?.Address,
                                Balance = a.AvailableBalance,
                                BalanceCode = _currency.DisplayedName,
                                UpdateAddress = UpdateAddress
                            };
                        })
                    );
                });

                if (HasTokens)
                {
                    var account = _app.Account
                        .GetCurrencyAccount(_currency.Name);

                    var tezosAccount = account as TezosAccount;

                    (await tezosAccount
                            .DataRepository
                            .GetTezosTokenAddressesByContractAsync(_tokenContract))
                        .Where(w => w.TokenBalance.TokenId == _tokenId)
                        .Where(w => w.Balance != 0)
                        .ToList()
                        .ForEachDo(addressWithTokens =>
                        {
                            var addressViewModel = Addresses
                                .FirstOrDefault(a => a.Address == addressWithTokens.Address);
                            if (addressViewModel == null)
                                return;

                            addressViewModel.TokenBalance = addressWithTokens.TokenBalance;
                        });
                }

                AddressesChanged?.Invoke();
            }
            catch (Exception e)
            {
                Log.Error(e, "Error while reload addresses list.");
            }
        }

        private async Task<AddressViewModel> UpdateAddress(string address)
        {
            try
            {
                var updateTask = new HdWalletScanner(_app.Account)
                    .ScanAddressAsync(_currency.Name, address);

                await Task.WhenAll(Task.Delay(MinimalAddressUpdateTimeMs), updateTask);

                if (_currency.Name == TezosConfig.Xtz && _tokenContract != null)
                {
                    // update tezos token balance
                    var tezosAccount = _app.Account
                        .GetCurrencyAccount<TezosAccount>(TezosConfig.Xtz);

                    await new TezosTokensScanner(tezosAccount)
                        .UpdateBalanceAsync(address, _tokenContract, (int) _tokenId);
                }

                await ReloadAddresses();

                await Device.InvokeOnMainThreadAsync(() =>
                {
                    _navigationService?.DisplaySnackBar(MessageType.Regular,
                        AppResources.AddressLabel + " " + AppResources.HasBeenUpdated);
                });

                return Addresses.FirstOrDefault(a => a.Address == address);
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

            return null;
        }

        public void Dispose() => _app.Account.BalanceUpdated -= OnBalanceUpdatedEventHandler;
    }
}