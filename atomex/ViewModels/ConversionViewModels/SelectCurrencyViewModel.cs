using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Linq;
using System.Windows.Input;
using atomex.Common;
using atomex.Views;
using atomex.Views.CreateSwap;
using Atomex;
using Atomex.Blockchain.BitcoinBased;
using Atomex.Common;
using Atomex.Core;
using atomex.Models;
using atomex.ViewModels.CurrencyViewModels;
using Atomex.Wallet.Abstract;
using Atomex.Wallet.BitcoinBased;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace atomex.ViewModels.ConversionViewModels
{
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
                .Select(outputs => $"from {outputs.Count()} outputs")
                .ToPropertyExInMainThread(this, vm => vm.SelectedAddressDescription);


            this.WhenAnyValue(vm => vm.SelectedOutputs)
                .WhereNotNull()
                .Select(outputs =>
                {
                    var currency = (BitcoinBasedConfig) CurrencyViewModel.Currency;
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
                .Select(address => address.Balance)
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

        public ICommand ChangeAddressesCommand => _changeAddressesCommand ??=
            ReactiveCommand.Create<SelectCurrencyViewModelItem>(async i =>
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
                            Config = (BitcoinBasedConfig) currency,
                            IsSelected =
                                itemWithOutputs?.SelectedOutputs?.Any(so => so.TxId == o.TxId && so.Index == o.Index) ??
                                true
                        }),
                        config: (BitcoinBasedConfig) currency,
                        account: bitcoinBasedAccount,
                        navigationService: _navigationService,
                        tab: TabNavigation.Exchange)
                    {
                        ConfirmAction = (selectOutputsViewModel, outputs) =>
                        {
                            itemWithOutputs.SelectedOutputs = outputs;
                            _navigationService?.ReturnToInitiatedPage(TabNavigation.Exchange);

                            CurrencySelected?.Invoke(i);
                        }
                    };

                    _navigationService?.ShowPage(new SelectOutputsPage(selectOutputsViewModel), TabNavigation.Exchange);
                }
                else if (i is SelectCurrencyWithAddressViewModelItem itemWithAddress)
                {
                    _selectAddressViewModel = new SelectAddressViewModel(
                        account: _account,
                        currency: currency,
                        navigationService: _navigationService,
                        tab: TabNavigation.Exchange,
                        mode: Type == SelectCurrencyType.From
                            ? SelectAddressMode.SendFrom
                            : SelectAddressMode.ReceiveTo,
                        selectedAddress: itemWithAddress.SelectedAddress?.Address)
                    {
                        ConfirmAction = (selectAddressViewModel, walletAddressViewModel) =>
                        {
                            var selectedAvaialbleAddress = itemWithAddress
                                .AvailableAddresses
                                .FirstOrDefault(a => a?.Address == walletAddressViewModel?.Address);

                            if (Type == SelectCurrencyType.From)
                            {
                                itemWithAddress.SelectedAddress =
                                    selectedAvaialbleAddress ?? itemWithAddress.SelectedAddress;
                            }
                            else
                            {
                                itemWithAddress.SelectedAddress = selectedAvaialbleAddress ?? new WalletAddress
                                {
                                    Address = walletAddressViewModel.Address,
                                    Currency = currency.Name
                                };
                            }

                            _navigationService?.ReturnToInitiatedPage(TabNavigation.Exchange);

                            CurrencySelected?.Invoke(i);
                        }
                    };

                    if (Type == SelectCurrencyType.From)
                        _navigationService?.ShowPage(new SelectAddressPage(_selectAddressViewModel),
                            TabNavigation.Exchange);
                    else
                        _navigationService?.ShowPopup(new AddressesBottomSheet(this));
                }
            });

        private ICommand _enterExternalAddressCommand;

        public ICommand EnterExternalAddressCommand => _enterExternalAddressCommand ??= ReactiveCommand.Create(() =>
        {
            _selectAddressViewModel?.SetAddressMode(SelectAddressMode.EnterExternalAddress);
            _navigationService?.ClosePopup();
            _navigationService?.ShowPage(new SelectAddressPage(_selectAddressViewModel), TabNavigation.Exchange);
        });

        private ICommand _chooseMyAddressCommand;

        public ICommand ChooseMyAddressCommand => _chooseMyAddressCommand ??= ReactiveCommand.Create(() =>
        {
            _selectAddressViewModel?.SetAddressMode(SelectAddressMode.ChooseMyAddress);
            _navigationService?.ClosePopup();
            _navigationService?.ShowPage(new SelectAddressPage(_selectAddressViewModel), TabNavigation.Exchange);
        });

        private ICommand _closeBottomSheetCommand;

        public ICommand CloseBottomSheetCommand => _closeBottomSheetCommand ??=
            ReactiveCommand.Create(() => _navigationService?.ClosePopup());

        private readonly IAccount _account;
        private readonly INavigationService _navigationService;

        public SelectCurrencyViewModel(
            IAccount account,
            INavigationService navigationService,
            SelectCurrencyType type,
            IEnumerable<SelectCurrencyViewModelItem> currencies,
            SelectCurrencyViewModelItem selected = null)
        {
            _account = account ?? throw new ArgumentNullException(nameof(account));
            _navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));

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