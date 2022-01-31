using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using atomex.Resources;
using atomex.Services;
using atomex.Views.Send;
using Atomex;
using Atomex.Blockchain.BitcoinBased;
using Atomex.Core;
using Atomex.Wallet.BitcoinBased;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Xamarin.Essentials;
using Xamarin.Forms;

namespace atomex.ViewModel.SendViewModels
{
    public class SelectOutputsViewModel : BaseViewModel
    {
        protected IToastService ToastService { get; set; }
        protected INavigation Navigation { get; set; }

        [Reactive] public ObservableCollection<OutputViewModel> Outputs { get; set; }
        [Reactive] public string TotalSelectedString { get; set; }
        [Reactive] public decimal SelectedAmount { get; set; }
        public BitcoinBasedConfig Currency { get; }
        private BitcoinBasedAccount Account { get; }
        public Action<IEnumerable<BitcoinBasedTxOutput>> ConfirmAction { get; set; }

        public SelectOutputsViewModel(IEnumerable<OutputViewModel> outputs, BitcoinBasedAccount account, BitcoinBasedConfig config, INavigation navigation)
        {
            Account = account ?? throw new ArgumentNullException(nameof(Account));
            Currency = config ?? throw new ArgumentNullException(nameof(Currency));
            ToastService = DependencyService.Get<IToastService>() ?? throw new ArgumentNullException(nameof(ToastService));
            Navigation = navigation ?? throw new ArgumentNullException(nameof(Navigation));

            Outputs = new ObservableCollection<OutputViewModel>(outputs);

            SelectAll = Outputs.Aggregate(true, (result, output) => result && output.IsSelected);

            this.WhenAnyValue(vm => vm.Outputs)
                .WhereNotNull()
                .Take(1)
                .Subscribe(async outputs =>
                {
                    var addresses = (await Account
                        .GetAddressesAsync())
                        .Where(address => outputs.FirstOrDefault(o => o.Address == address.Address) != null)
                        .ToList();

                    var outputsWithAddresses = outputs.Select(output =>
                    {
                        var address = addresses.FirstOrDefault(a => a.Address == output.Address);
                        output.WalletAddress = address ?? null;;
                        return output;
                    });

                    Outputs = new ObservableCollection<OutputViewModel>(
                        outputsWithAddresses.OrderByDescending(output => output.Balance));

                    UpdateSelectedAmount();
                });

            this.WhenAnyValue(vm => vm.SelectAll)
                .Where(_ => !_selectFromList)
                .Throttle(TimeSpan.FromMilliseconds(1))
                .Skip(1)
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(_ =>
                {
                    if (Outputs == null)
                        return;

                    SelectAllOutputs();
                    UpdateSelectedAmount();
                });

            this.WhenAnyValue(vm => vm.SortIsAscending)
                .Where(_ => Outputs != null)
                .Subscribe(sortIsAscending =>
                {
                    Outputs = sortIsAscending
                        ? new ObservableCollection<OutputViewModel>(
                            Outputs.OrderBy(output => output.Balance))
                        : new ObservableCollection<OutputViewModel>(
                            Outputs.OrderByDescending(output => output.Balance));
                });

            this.WhenAnyValue(vm => vm.SortByBalance, vm => vm.SortIsAscending)
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(value =>
                {
                    var (item1, item2) = value;

                    if (Outputs == null) return;

                    if (!item1)
                    {
                        var outputsList = Outputs.ToList();
                        if (item2)
                        {
                            outputsList.Sort((a1, a2) =>
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
                            outputsList.Sort((a2, a1) =>
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

                        Outputs = new ObservableCollection<OutputViewModel>(outputsList);
                    }
                    else
                    {
                        Outputs = new ObservableCollection<OutputViewModel>(item2
                            ? Outputs.OrderBy(output => output.Balance)
                            : Outputs.OrderByDescending(output => output.Balance));
                    }
                });
        }

        [Reactive] public bool SelectAll { get; set; }
        [Reactive] public bool SortByBalance { get; set; }
        [Reactive] public bool SortIsAscending { get; set; }

        private bool _selectFromList = false;

        private ReactiveCommand<Unit, Unit> _changeSortTypeCommand;
        public ReactiveCommand<Unit, Unit> ChangeSortTypeCommand => _changeSortTypeCommand ??=
            (_changeSortTypeCommand = ReactiveCommand.Create(() => { SortByBalance = !SortByBalance; }));

        private ReactiveCommand<Unit, Unit> _changeSortDirectionCommand;
        public ReactiveCommand<Unit, Unit> ChangeSortDirectionCommand => _changeSortDirectionCommand ??=
            (_changeSortDirectionCommand = ReactiveCommand.Create(() => { SortIsAscending = !SortIsAscending; }));

        private ReactiveCommand<OutputViewModel, Unit> _selectOutputCommand;
        public ReactiveCommand<OutputViewModel, Unit> SelectOutputCommand => _selectOutputCommand ??=
            (_selectOutputCommand = ReactiveCommand.Create<OutputViewModel>(SelectOutput));

        private ReactiveCommand<object, Unit> _selectAllCommand;
        public ReactiveCommand<object, Unit> SelectAllCommand => _selectAllCommand ??=
            (_selectAllCommand = ReactiveCommand.Create<object>(o => SelectAllOutputs()));

        private ReactiveCommand<OutputViewModel, Unit> _copyCommand;
        public ReactiveCommand<OutputViewModel, Unit> CopyCommand =>
            _copyCommand ??= (_copyCommand = ReactiveCommand.CreateFromTask<OutputViewModel>(OnCopyButtonClicked));

        private ReactiveCommand<Unit, Unit> _searchCommand;
        public ReactiveCommand<Unit, Unit> SearchCommand =>
            _searchCommand ??= (_searchCommand = ReactiveCommand.CreateFromTask(OnSearchButtonClicked));

        private ICommand _confirmOutputsCommand;
        public ICommand ConfirmOutputsCommand => _confirmOutputsCommand ??=
            (_confirmOutputsCommand = ReactiveCommand.Create(ConfirmOutputs));


        private void ConfirmOutputs()
        {
            var outputs = Outputs
                .Where(output => output.IsSelected)
                .Select(o => o.Output);

            ConfirmAction?.Invoke(outputs);
        }

        private void SelectAllOutputs()
        {
            _selectFromList = false;
            Outputs.ToList().ForEach(o => o.IsSelected = SelectAll);

            UpdateSelectedAmount();
        }

        private void UpdateSelectedAmount()
        {
            var selectedCount = Outputs
                .Where(o => o.IsSelected).Count();

            TotalSelectedString = string.Format(
                CultureInfo.InvariantCulture,
                AppResources.SelectedOfTotal,
                selectedCount,
                Outputs.Count);

            SelectedAmount = Outputs
                .Where(o => o.IsSelected)
                .Aggregate((decimal)0, (sum, output) => sum + output.Balance);
        }

        private void SelectOutput(OutputViewModel vm)
        {
            vm.IsSelected = !vm.IsSelected;

            _selectFromList = true;
            var selectionResult = Outputs.Aggregate(true, (result, output) => result && output.IsSelected);
            if (SelectAll != selectionResult)
                SelectAll = selectionResult;

            UpdateSelectedAmount();
        }

        protected async Task OnCopyButtonClicked(OutputViewModel output)
        {
            if (output != null)
            {
                await Clipboard.SetTextAsync(output.Address);
                ToastService?.Show(AppResources.AddressCopied, ToastPosition.Top, Application.Current.RequestedTheme.ToString());
            }
            else
            {
                await Application.Current.MainPage.DisplayAlert(AppResources.Error, AppResources.CopyError, AppResources.AcceptButton);
            }
        }

        private async Task OnSearchButtonClicked()
        {
            await Navigation.PushAsync(new SearchOutputsPage(this));
        }
    }

    public class OutputViewModel : BaseViewModel
    {
        [Reactive] public bool IsSelected { get; set; }
        public BitcoinBasedTxOutput Output { get; set; }
        public BitcoinBasedConfig Config { get; set; }
        public WalletAddress WalletAddress { get; set; }
        public Action<string> CopyAction { get; set; }

        public decimal Balance => Config.SatoshiToCoin(Output.Value);
        public string Address => Output.DestinationAddress(Config.Network);
    }
}

