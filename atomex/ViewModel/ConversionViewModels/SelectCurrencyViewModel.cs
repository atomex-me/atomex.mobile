﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Reactive.Linq;
using System.Windows.Input;
using atomex.Common;
using atomex.ViewModel.CurrencyViewModels;
using atomex.ViewModel.SendViewModels;
using atomex.Views.CreateSwap;
using atomex.Views.Send;
using Atomex;
using Atomex.Blockchain.BitcoinBased;
using Atomex.Common;
using Atomex.Core;
using Atomex.Wallet.Abstract;
using Atomex.Wallet.BitcoinBased;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Rg.Plugins.Popup.Services;
using Xamarin.Forms;

namespace atomex.ViewModel.ConversionViewModels
{
    public enum SelectCurrencyType
    {
        [Description("From")]
        From,
        [Description("To")]
        To
    }

    public abstract class SelectCurrencyViewModelItem : BaseViewModel
    {
        [Reactive] public CurrencyViewModel CurrencyViewModel { get; set; }
        [ObservableAsProperty] public string SelectedAddressDescription { get; }
        [ObservableAsProperty] public decimal SelectedBalance { get; }
        public abstract string ShortAddressDescription { get; }
        public abstract IFromSource FromSource { get; }

        public SelectCurrencyViewModelItem(CurrencyViewModel currencyViewModel)
        {
            CurrencyViewModel = currencyViewModel ?? throw new ArgumentNullException(nameof(currencyViewModel));
        }
    }

    public class SelectCurrencyWithOutputsViewModelItem : SelectCurrencyViewModelItem
    {
        public IEnumerable<BitcoinBasedTxOutput> AvailableOutputs { get; set; }
        [Reactive] public IEnumerable<BitcoinBasedTxOutput> SelectedOutputs { get; set; }
        public override string ShortAddressDescription => $"{SelectedOutputs?.Count() ?? 0} outputs";
        public override IFromSource FromSource => SelectedOutputs != null
            ? new FromOutputs(SelectedOutputs)
            : null;

        public SelectCurrencyWithOutputsViewModelItem(
            CurrencyViewModel currencyViewModel,
            IEnumerable<BitcoinBasedTxOutput> availableOutputs,
            IEnumerable<BitcoinBasedTxOutput> selectedOutputs = null)
            : base(currencyViewModel)
        {
            this.WhenAnyValue(vm => vm.SelectedOutputs)
                .WhereNotNull()
                .Select(outputs =>
                {
                    return $"from {outputs.Count()} outputs";
                })
                .ToPropertyExInMainThread(this, vm => vm.SelectedAddressDescription);


            this.WhenAnyValue(vm => vm.SelectedOutputs)
                .WhereNotNull()
                .Select(outputs =>
                {
                    var currency = (BitcoinBasedConfig)CurrencyViewModel.Currency;
                    var totalAmountInSatoshi = outputs.Sum(o => o.Value);
                    var totalAmount = currency.SatoshiToCoin(totalAmountInSatoshi);
                    return totalAmount;
                })
                .ToPropertyExInMainThread(this, vm => vm.SelectedBalance);

            AvailableOutputs = availableOutputs ?? throw new ArgumentNullException(nameof(availableOutputs));
            SelectedOutputs = selectedOutputs ?? availableOutputs;
        }

        public void TryInitializeFromItem(SelectCurrencyViewModelItem source)
        {
            if (source is not SelectCurrencyWithOutputsViewModelItem sourceWithOutputs)
                return;

            var selectedOutputs = new List<BitcoinBasedTxOutput>();

            foreach (var output in sourceWithOutputs.SelectedOutputs)
            {
                var selectedOutput = AvailableOutputs
                   .FirstOrDefault(o => o.TxId == output.TxId && o.Index == output.Index && o.Value == output.Value);

                if (selectedOutput == null)
                {
                    selectedOutputs.Clear();
                    return;
                }

                selectedOutputs.Add(selectedOutput);
            }

            SelectedOutputs = selectedOutputs;
        }
    }

    public class SelectCurrencyWithAddressViewModelItem : SelectCurrencyViewModelItem
    {
        private readonly SelectCurrencyType _type;

        public IEnumerable<WalletAddress> AvailableAddresses { get; set; }
        [Reactive] public WalletAddress SelectedAddress { get; set; }
        [Reactive] public bool IsNew { get; set; }
        public override string ShortAddressDescription => SelectedAddress?.Address;
        public override IFromSource FromSource => SelectedAddress?.Address != null
            ? new FromAddress(SelectedAddress.Address)
            : null;

        public SelectCurrencyWithAddressViewModelItem(
            CurrencyViewModel currencyViewModel,
            SelectCurrencyType type,
            IEnumerable<WalletAddress> availableAddresses,
            WalletAddress selectedAddress = null)
            : base(currencyViewModel)
        {
            _type = type;
            
            this.WhenAnyValue(vm => vm.SelectedAddress)
                .WhereNotNull()
                .Select(address =>
                {
                    if (_type == SelectCurrencyType.To && IsNew)
                        return $"to new address";
                    var prefix = _type == SelectCurrencyType.From ? "from" : "to";

                    return $"{prefix} {address.Address.TruncateAddress()}";
                })
                .ToPropertyExInMainThread(this, vm => vm.SelectedAddressDescription);

            this.WhenAnyValue(vm => vm.SelectedAddress)
                .WhereNotNull()
                .Select(address =>
                {
                    return address.Balance;
                })
                .ToPropertyExInMainThread(this, vm => vm.SelectedBalance);

            AvailableAddresses = availableAddresses ?? throw new ArgumentNullException(nameof(availableAddresses));
            SelectedAddress = selectedAddress ?? availableAddresses.MaxByOrDefault(w => w.Balance);
        }

        public void TryInitializeFromItem(SelectCurrencyViewModelItem source)
        {
            if (source is not SelectCurrencyWithAddressViewModelItem sourceWithAddress)
                return;

            var address = AvailableAddresses
                .FirstOrDefault(a => a.Address == sourceWithAddress.ShortAddressDescription);

            if (address != null)
                SelectedAddress = address;
        }
    }

    public class SelectCurrencyViewModel : BaseViewModel
    {
        public Action<SelectCurrencyViewModelItem> CurrencySelected;

        [Reactive] public ObservableCollection<SelectCurrencyViewModelItem> Currencies { get; set; }
        [Reactive] public SelectCurrencyViewModelItem SelectedCurrency { get; set; }
        [Reactive] public SelectCurrencyType Type { get; set; }
        private SelectAddressViewModel _selectAddressViewModel;

        private ICommand _changeAddressesCommand;
        public ICommand ChangeAddressesCommand => _changeAddressesCommand ??= ReactiveCommand.Create<SelectCurrencyViewModelItem>(async i =>
        {
            var currency = i.CurrencyViewModel.Currency;

            if (i is SelectCurrencyWithOutputsViewModelItem itemWithOutputs && Type == SelectCurrencyType.From)
            {
                var bitcoinBasedAccount = _account
                    .GetCurrencyAccount<BitcoinBasedAccount>(currency.Name);

                var outputs = (await bitcoinBasedAccount
                    .GetAvailableOutputsAsync())
                    .Cast<BitcoinBasedTxOutput>();

                var selectOutputsViewModel = new SelectOutputsViewModel(
                    outputs: outputs.Select(o => new OutputViewModel()
                    {
                        Output = o,
                        Config = (BitcoinBasedConfig)currency,
                        IsSelected = itemWithOutputs?.SelectedOutputs?.Any(so => so.TxId == o.TxId && so.Index == o.Index) ?? true
                    }),
                    config: (BitcoinBasedConfig)currency,
                    account: bitcoinBasedAccount,
                    navigation: _navigation)
                {
                    ConfirmAction = (selectOutputsViewModel, outputs) =>
                    {
                        itemWithOutputs.SelectedOutputs = outputs;

                        if (selectOutputsViewModel.SelectAddressFrom == SelectAddressFrom.Init)
                        {
                            _navigation.RemovePage(_navigation.NavigationStack[_navigation.NavigationStack.Count - 2]);
                        }
                        else
                        {
                            for (int i = 0; i <= 1; i++)
                                _navigation.RemovePage(_navigation.NavigationStack[_navigation.NavigationStack.Count - 2]);
                        }

                        CurrencySelected?.Invoke(i);
                    }
                };

                await _navigation.PushAsync(new SelectOutputsPage(selectOutputsViewModel));
            }
            else if (i is SelectCurrencyWithAddressViewModelItem itemWithAddress)
            {
                _selectAddressViewModel = new SelectAddressViewModel(
                    account: _account,
                    currency: currency,
                    navigation: _navigation,
                    mode: Type == SelectCurrencyType.From ? SelectAddressMode.SendFrom : SelectAddressMode.ReceiveTo,
                    selectedAddress: itemWithAddress.SelectedAddress?.Address)
                {
                    ConfirmAction = (selectAddressViewModel, walletAddressViewModel) =>
                    {
                        var selectedAvaialbleAddress = itemWithAddress
                            .AvailableAddresses
                            .FirstOrDefault(a => a?.Address == walletAddressViewModel?.Address);

                        if (Type == SelectCurrencyType.From)
                        {
                            itemWithAddress.SelectedAddress = selectedAvaialbleAddress ?? itemWithAddress.SelectedAddress;
                        }
                        else
                        {
                            itemWithAddress.SelectedAddress = selectedAvaialbleAddress ?? new WalletAddress
                            {
                                Address = walletAddressViewModel.Address,
                                Currency = currency.Name
                            };
                        }

                        if (selectAddressViewModel.SelectAddressFrom == SelectAddressFrom.Init)
                        {
                            _navigation.RemovePage(_navigation.NavigationStack[_navigation.NavigationStack.Count - 2]);
                        }
                        else
                        {
                            for (int i = 0; i <= 1; i++)
                                _navigation.RemovePage(_navigation.NavigationStack[_navigation.NavigationStack.Count - 2]);
                        }

                        CurrencySelected?.Invoke(i);
                    }
                };

                _ = Type == SelectCurrencyType.From
                    ? _navigation.PushAsync(new SelectAddressPage(_selectAddressViewModel))
                    : PopupNavigation.Instance.PushAsync(new AddressesBottomSheet(this));
            }
        });

        private ICommand _enterExternalAddressCommand;
        public ICommand EnterExternalAddressCommand => _enterExternalAddressCommand ??= ReactiveCommand.CreateFromTask(async () =>
        {
            _selectAddressViewModel?.SetAddressMode(SelectAddressMode.EnterExternalAddress);
            if (PopupNavigation.Instance.PopupStack.Count > 0)
                _ = PopupNavigation.Instance.PopAsync();
            await _navigation.PushAsync(new SelectAddressPage(_selectAddressViewModel));
        });

        private ICommand _chooseMyAddressCommand;
        public ICommand ChooseMyAddressCommand => _chooseMyAddressCommand ??= ReactiveCommand.CreateFromTask(async () =>
        {
            _selectAddressViewModel?.SetAddressMode(SelectAddressMode.ChooseMyAddress);
            if (PopupNavigation.Instance.PopupStack.Count > 0)
                _ = PopupNavigation.Instance.PopAsync();
            await _navigation.PushAsync(new SelectAddressPage(_selectAddressViewModel));
        });

        private ICommand _closeBottomSheetCommand;
        public ICommand CloseBottomSheetCommand => _closeBottomSheetCommand ??= ReactiveCommand.Create(() =>
        {
            if (PopupNavigation.Instance.PopupStack.Count > 0)
                _ = PopupNavigation.Instance.PopAsync();
        });

        private readonly IAccount _account;
        private readonly INavigation _navigation;

        public SelectCurrencyViewModel(
            IAccount account,
            INavigation navigation,
            SelectCurrencyType type,
            IEnumerable<SelectCurrencyViewModelItem> currencies,
            SelectCurrencyViewModelItem selected = null)
        {
            _account = account ?? throw new ArgumentNullException(nameof(account));
            _navigation = navigation ?? throw new ArgumentNullException(nameof(navigation));

            Type = type;
            Currencies = new ObservableCollection<SelectCurrencyViewModelItem>(currencies);

            if (selected != null)
            {
                var selectedCurrencyViewItem = Currencies
                    .FirstOrDefault(c => c.CurrencyViewModel.Currency.Name == selected.CurrencyViewModel.Currency.Name);

                if (selectedCurrencyViewItem is SelectCurrencyWithOutputsViewModelItem selectedCurrencyWithOutputs)
                {
                    selectedCurrencyWithOutputs.TryInitializeFromItem(selected);
                }
                else if (selectedCurrencyViewItem is SelectCurrencyWithAddressViewModelItem selectedCurrencyWithAddress)
                {
                    selectedCurrencyWithAddress.TryInitializeFromItem(selected);
                }

                SelectedCurrency = selectedCurrencyViewItem;
            }

            this.WhenAnyValue(vm => vm.SelectedCurrency)
                .WhereNotNull()
                .Subscribe(i =>
                {
                    CurrencySelected?.Invoke(i);
                    SelectedCurrency = null;
                });
        }
    }
}
