using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Windows.Input;
using Atomex.Common;
using Atomex.Core;
using Atomex.ViewModels;
using Atomex.Wallet.Abstract;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace atomex.ViewModel.SendViewModels
{
    public class SelectAddressViewModel : BaseViewModel
    {
        public Action<string, decimal> ConfirmAction { get; set; }
        public bool UseToSelectFrom { get; set; }
        private ObservableCollection<WalletAddressViewModel> InitialMyAddresses { get; set; }
        [Reactive] public ObservableCollection<WalletAddressViewModel> MyAddresses { get; set; }
        [Reactive] public string SearchPattern { get; set; }
        [Reactive] public string ToAddress { get; set; }
        [Reactive] private bool SortIsAscending { get; set; }
        [Reactive] private bool SortByBalance { get; set; }
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
                    ? SearchPattern
                    : SelectedAddress.WalletAddress.Address;

                var balance = SelectedAddress == null
                    ? 0m
                    : SelectedAddress.WalletAddress.AvailableBalance();

                ConfirmAction?.Invoke(selectedAddress, balance);
            }));

        private ICommand _copyAddressCommand;

        public ICommand CopyAddressCommand =>
            _copyAddressCommand ??= (_copyAddressCommand = ReactiveCommand.Create((WalletAddress address) =>
            {
            }));
    }
}
