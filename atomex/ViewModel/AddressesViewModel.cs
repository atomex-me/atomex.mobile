﻿using System;
using System.Collections.ObjectModel;
using System.Globalization;
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
using Atomex.Wallet;
using Atomex.Wallet.Tezos;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Serilog;
using Xamarin.Essentials;
using Xamarin.Forms;
using static atomex.Models.SnackbarMessage;

namespace atomex.ViewModel
{
    public class AddressViewModel : BaseViewModel
    {
        private readonly IAtomexApp _app;
        private CurrencyConfig _currency;
        private INavigationService _navigationService;

        [Reactive] public string Address { get; set; }
        [Reactive] public string Type { get; set; }
        [Reactive] public string Path { get; set; }
        [Reactive] public string Balance { get; set; }
        [Reactive] public TokenBalance TokenBalance { get; set; }

        [Reactive] public bool IsUpdating { get; set; }
        [Reactive] public bool IsCopied { get; set; }
        [Reactive] public bool ExportKeyConfirm { get; set; }
        [Reactive] public string CopyButtonName { get; set; }
        [Reactive] public bool IsFreeAddress { get; set; }

        public Func<string, Task<AddressViewModel>> UpdateAddress { get; set; }

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

        private ReactiveCommand<string, Unit> _copyCommand;
        public ReactiveCommand<string, Unit> CopyCommand => _copyCommand ??= ReactiveCommand.Create<string>((s) =>
        {
            try
            {
                Clipboard.SetTextAsync(Address);
                _navigationService?.DisplaySnackBar(MessageType.Regular, AppResources.AddressCopied);
            }
            catch (Exception)
            {
                _navigationService?.ShowAlert(AppResources.Error, AppResources.CopyError, AppResources.AcceptButton);
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
                    var hex = Hex.ToHexString(unsecuredPrivateKey); ;

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
        public ReactiveCommand<string, Unit> UpdateAddressCommand => _updateAddressCommand ??= ReactiveCommand.Create<string>(async (s) =>
        {
            if (UpdateAddress == null)
                return;

            IsUpdating = true;
            var updatedViewModel = await UpdateAddress(Address);
            IsUpdating = false;

            Balance = updatedViewModel?.Balance;
            TokenBalance = updatedViewModel?.TokenBalance;
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
        [Reactive] public ObservableCollection<AddressViewModel> Addresses { get; set; }
        public bool HasTokens => _currency.Name == TezosConfig.Xtz && _tokenContract != null;

        private SelectAddressViewModel _selectAddressViewModel { get; set; }
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

            this.WhenAnyValue(vm => vm.Addresses)
                .WhereNotNull()
                .SubscribeInMainThread(a =>
                {
                    _selectAddressViewModel = new SelectAddressViewModel(
                        account: _app.Account,
                        currency: _currency,
                        navigationService: _navigationService,
                        tab: TabNavigation.Portfolio,
                        mode: SelectAddressMode.ChooseMyAddress)
                    {
                        ConfirmAction = (selectAddressViewModel, walletAddressViewModel) =>
                        {
                            var address = Addresses?
                                .Where(a => a.Address == walletAddressViewModel?.Address)
                                .FirstOrDefault();
                            _navigationService?.ShowPage(new AddressInfoPage(address), TabNavigation.Portfolio);

                            if (_selectAddressViewModel.SelectAddressFrom == SelectAddressFrom.InitSearch)
                                _navigationService?.RemovePreviousPage(TabNavigation.Portfolio);
                        }
                    };
                });

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
        public ReactiveCommand<Unit, Unit> ShowAllAddressesCommand => _showAllAddressesCommand ??= ReactiveCommand.Create(() =>
            _navigationService?.ShowPage(new SelectAddressPage(_selectAddressViewModel), TabNavigation.Portfolio));

        private void SubscribeToServices()
        {
            _app.Account.BalanceUpdated += OnBalanceUpdatedEventHandler;
        }

        private async void OnBalanceUpdatedEventHandler(object sender, CurrencyEventArgs args)
        {
            try
            {
                if (Currencies.IsTezosToken(args.Currency) && Currencies.IsTezosBased(_currency.Name))
                {
                    await ReloadAddresses();
                    return;
                }

                if (_currency.Name == args.Currency)
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

                var freeAddress = _app.Account
                    .GetFreeExternalAddressAsync(_currency?.Name)
                    .WaitForResult();

                Addresses = new ObservableCollection<AddressViewModel>(
                    addresses.Select(a =>
                    {
                        var path = a.KeyType == CurrencyConfig.StandardKey && Currencies.IsTezosBased(_currency.Name)
                            ? $"m/44'/{_currency.Bip44Code}'/{a.KeyIndex.Account}'/{a.KeyIndex.Chain}'"
                            : $"m/44'/{_currency.Bip44Code}'/{a.KeyIndex.Account}'/{a.KeyIndex.Chain}/{a.KeyIndex.Index}";             

                        return new AddressViewModel(_app, _currency, _navigationService)
                        {
                            Address = a.Address,
                            Type = KeyTypeToString(a.KeyType),
                            Path = path,
                            IsFreeAddress = a?.Address == freeAddress?.Address,
                            Balance = $"{a.Balance.ToString(CultureInfo.InvariantCulture)} {_currency.Name}",
                            UpdateAddress = UpdateAddress
                        };
                    }));

                if (HasTokens)
                {
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

        private string KeyTypeToString(int keyType) =>
            keyType switch
            {
                CurrencyConfig.StandardKey => "Standard",
                TezosConfig.Bip32Ed25519Key => "Atomex",
                _ => throw new NotSupportedException($"Key type {keyType} not supported.")
            };

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
                    _navigationService?.DisplaySnackBar(MessageType.Regular, AppResources.AddressLabel + " " + AppResources.HasBeenUpdated);
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
