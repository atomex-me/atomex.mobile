using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using atomex.Common;
using atomex.Resources;
using atomex.Views;
using atomex.Views.Popup;
using Atomex;
using Atomex.Blockchain;
using Atomex.Common;
using Atomex.Core;
using Atomex.MarketData.Abstract;
using atomex.Models;
using atomex.ViewModels.SendViewModels;
using atomex.ViewModels.TransactionViewModels;
using Atomex.Wallet;
using Atomex.Wallet.Abstract;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Serilog;
using Xamarin.Essentials;
using Xamarin.Forms;
using static atomex.Models.SnackbarMessage;

namespace atomex.ViewModels.CurrencyViewModels
{
    public class CurrencyViewModel : BaseViewModel, IDisposable
    {
        protected readonly IAtomexApp App;
        protected readonly INavigationService NavigationService;
        private IAccount _account;

        public CurrencyConfig Currency { get; set; }
        private IQuotesProvider QuotesProvider { get; set; }

        public string CurrencyCode => Currency?.Name;
        public string CurrencyName => Currency?.DisplayedName;
        public string FeeCurrencyCode => Currency?.FeeCode;
        public string BaseCurrencyCode => "USD";
        public bool IsBitcoinBased => Currency is BitcoinBasedConfig;
        public bool IsStakingAvailable => CurrencyCode == TezosConfig.Xtz;
        public bool HasCollectibles => CurrencyCode == TezosConfig.Xtz;
        public bool HasTokens => CurrencyCode == TezosConfig.Xtz;
        public bool HasDapps => CurrencyCode == TezosConfig.Xtz;
        public bool CanBuy => BuyViewModel.Currencies.Contains(Currency?.Name);

        protected CancellationTokenSource CancellationTokenSource;

        [Reactive] public decimal TotalAmount { get; set; }
        [Reactive] public decimal TotalAmountInBase { get; set; }
        [Reactive] public decimal AvailableAmount { get; set; }
        [Reactive] public decimal AvailableAmountInBase { get; set; }
        [Reactive] public decimal DailyChangePercent { get; set; }
        [Reactive] public decimal UnconfirmedAmount { get; set; }
        [Reactive] public decimal UnconfirmedAmountInBase { get; set; }
        public bool HasUnconfirmedAmount => UnconfirmedAmount != 0;
        [Reactive] public decimal CurrentQuote { get; set; }
        public event EventHandler AmountUpdated;

        protected IList<TransactionViewModel> Transactions { get; set; }
        [Reactive] public ObservableCollection<Grouping<TransactionViewModel>> GroupedTransactions { get; set; }
        [Reactive] public TransactionViewModel SelectedTransaction { get; set; }

        [Reactive] public int QtyDisplayedTxs { get; set; }
        private int _defaultQtyDisplayedTxs = 5;
        [Reactive] public bool IsTxsLoading { get; set; }
        public int LoadingStepTxs => 20;

        [Reactive] public AddressesViewModel AddressesViewModel { get; set; }
        [Reactive] public ObservableCollection<AddressViewModel> Addresses { get; set; }
        [Reactive] public bool CanShowMoreAddresses { get; set; }
        private int QtyDisplayedAddresses => 3;

        public class Grouping<T> : ObservableCollection<T>
        {
            public DateTime Date { get; set; }

            public Grouping(DateTime date, IEnumerable<T> items)
            {
                Date = date;

                foreach (T item in items)
                    Items.Add(item);
            }

            public string DateString =>
                Date.ToString(AppResources.Culture.DateTimeFormat.MonthDayPattern, AppResources.Culture);
        }

        [Reactive] public bool IsRefreshing { get; set; }
        [Reactive] public CurrencyTab SelectedTab { get; set; }

        public CurrencyViewModel(
            IAtomexApp app,
            CurrencyConfig currency,
            INavigationService navigationService)
        {
            App = app ?? throw new ArgumentNullException(nameof(app));
            _account = app.Account ?? throw new ArgumentNullException(nameof(app.Account));
            NavigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));
            Currency = currency ?? throw new ArgumentNullException(nameof(currency));

            this.WhenAnyValue(vm => vm.SelectedTransaction)
                .WhereNotNull()
                .SubscribeInMainThread(t =>
                {
                    NavigationService?.ShowPage(new TransactionInfoPage(t), TabNavigation.Portfolio);
                    SelectedTransaction = null;
                });

            this.WhenAnyValue(vm => vm.Addresses)
                .WhereNotNull()
                .SubscribeInMainThread(_ =>
                    CanShowMoreAddresses = AddressesViewModel?.Addresses?.Count > QtyDisplayedAddresses);

            this.WhenAnyValue(vm => vm.AddressesViewModel)
                .WhereNotNull()
                .SubscribeInMainThread(_ =>
                {
                    AddressesViewModel!.AddressesChanged += OnAddresesChangedEventHandler;
                    OnAddresesChangedEventHandler();
                });
            
            _ = UpdateBalanceAsync();

            SelectedTab = CurrencyTab.Activity;
            QtyDisplayedTxs = _defaultQtyDisplayedTxs;
        }

        public virtual void LoadAddresses()
        {
            try
            {
                AddressesViewModel?.Dispose();

                AddressesViewModel = new AddressesViewModel(
                    app: App,
                    currency: Currency,
                    navigationService: NavigationService);
            }
            catch (Exception e)
            {
                Log.Error(e, "Load addresses error for currency {@Currency}", CurrencyCode);
            }
        }

        public void SubscribeToServices()
        {
            App.Account.BalanceUpdated += OnBalanceUpdatedEventHandler;
            App.Account.UnconfirmedTransactionAdded += OnUnconfirmedTransactionAdded;
        }

        public void SubscribeToRatesProvider(IQuotesProvider quotesProvider)
        {
            QuotesProvider = quotesProvider;
            QuotesProvider.QuotesUpdated += OnQuotesUpdatedEventHandler;
        }

        private void OnAddresesChangedEventHandler()
        {
            try
            {
                Device.InvokeOnMainThreadAsync(() =>
                    Addresses = new ObservableCollection<AddressViewModel>(
                        AddressesViewModel?.Addresses
                            .OrderByDescending(a => a.Balance)
                            .Take(QtyDisplayedAddresses) ?? new List<AddressViewModel>()));
            }
            catch (Exception e)
            {
                Log.Error(e, "On addresses changes event handler error for currency {@Currency}", CurrencyCode);
            }
        }

        protected virtual async void OnBalanceUpdatedEventHandler(object sender, CurrencyEventArgs args)
        {
            try
            {
                if (args.Currency != Currency.Name)
                    return;

                await Task.Run(async () =>
                {
                    await UpdateBalanceAsync();
                    await LoadTransactionsAsync();
                });
            }
            catch (Exception e)
            {
                Log.Error(e, "Error for currency {@Currency}", args?.Currency);
            }
        }

        public async Task UpdateBalanceAsync()
        {
            try
            {
                var balance = await App.Account
                    .GetBalanceAsync(Currency.Name)
                    .ConfigureAwait(false);

                if (balance == null) return;

                await Device.InvokeOnMainThreadAsync(() =>
                {
                    TotalAmount = balance.Confirmed;
                    AvailableAmount = balance.Available;
                    UnconfirmedAmount = balance.UnconfirmedIncome + balance.UnconfirmedOutcome;
                });
                UpdateQuotesInBaseCurrency(QuotesProvider);
            }
            catch (Exception e)
            {
                Log.Error(e, "UpdateBalanceAsync error for {@Currency}", CurrencyCode);
            }
        }

        private void OnQuotesUpdatedEventHandler(object sender, EventArgs args)
        {
            if (sender is not IQuotesProvider quotesProvider)
                return;

            UpdateQuotesInBaseCurrency(quotesProvider);
        }

        private void UpdateQuotesInBaseCurrency(IQuotesProvider quotesProvider)
        {
            if (quotesProvider == null) return;

            try
            {
                var quote = quotesProvider.GetQuote(CurrencyCode, BaseCurrencyCode);

                if (quote == null) return;

                Device.InvokeOnMainThreadAsync(() =>
                {
                    CurrentQuote = quote?.Bid ?? 0m;
                    DailyChangePercent = quote?.DailyChangePercent ?? 0m;
                    TotalAmountInBase = TotalAmount * (quote?.Bid ?? 0m);
                    AvailableAmountInBase = AvailableAmount * (quote?.Bid ?? 0m);
                    UnconfirmedAmountInBase = UnconfirmedAmount * (quote?.Bid ?? 0m);
                });

                AmountUpdated?.Invoke(this, EventArgs.Empty);
            }
            catch (Exception e)
            {
                Log.Error(e, "Update quotes error on {@Currency}", CurrencyCode);
            }
        }

        private async void OnUnconfirmedTransactionAdded(object sender, TransactionEventArgs e)
        {
            try
            {
                if (e.Transaction.Currency != Currency?.Name)
                    return;

                await Task.Run(async () => await LoadTransactionsAsync());
            }
            catch (Exception ex)
            {
                Log.Error(ex, "UnconfirmedTxAddedEventHandler error for {@Currency}", CurrencyCode);
            }
        }

        public virtual async Task LoadTransactionsAsync()
        {
            Log.Debug("LoadTransactionsAsync for {@Currency}", CurrencyCode);

            try
            {
                IsTxsLoading = true;

                if (App.Account == null)
                    return;

                var txs = await Task.Run(async () =>
                {
                    var transactions = (await App.Account.GetTransactionsAsync(Currency?.Name))
                        .ToList();

                    Transactions = new ObservableCollection<TransactionViewModel>(
                        transactions.Select(t =>
                                TransactionViewModelCreator.CreateViewModel(t, Currency, NavigationService))
                            .ToList()
                            .SortList((t1, t2) => t2.LocalTime.CompareTo(t1.LocalTime))
                            .ForEachDo(t =>
                            {
                                t.RemoveClicked += RemoveTransactonEventHandler;
                                t.CopyAddress = CopyAddress;
                                t.CopyTxId = CopyTxId;
                            }));

                    return Transactions
                        .Take(QtyDisplayedTxs)
                        .GroupBy(p => p.LocalTime.Date)
                        .Select(g => new Grouping<TransactionViewModel>(g.Key,
                            new ObservableCollection<TransactionViewModel>(g)))
                        .ToList();
                });

                await Device.InvokeOnMainThreadAsync(() =>
                    GroupedTransactions = new ObservableCollection<Grouping<TransactionViewModel>>(txs));
            }
            catch (Exception e)
            {
                Log.Error(e, "LoadTransactionAsync error for {@Currency}", CurrencyCode);
            }
            finally
            {
                IsTxsLoading = false;
            }
        }

        protected async void RemoveTransactonEventHandler(object sender, TransactionEventArgs args)
        {
            if (App.Account == null)
                return;

            try
            {
                var txId = $"{args.Transaction.Id}:{Currency.Name}";

                var isRemoved = await App.Account
                    .RemoveTransactionAsync(txId);

                if (isRemoved)
                    await Task.Run(async () => await LoadTransactionsAsync());
            }
            catch (Exception e)
            {
                Log.Error(e, "Transaction remove error");
            }

            NavigationService?.ClosePage(TabNavigation.Portfolio);
        }

        private ReactiveCommand<Unit, Unit> _receiveCommand;
        public ReactiveCommand<Unit, Unit> ReceiveCommand => _receiveCommand ??= ReactiveCommand.Create(OnReceiveClick);

        private ReactiveCommand<Unit, Unit> _sendCommand;
        public ReactiveCommand<Unit, Unit> SendCommand => _sendCommand ??= ReactiveCommand.Create(OnSendClick);

        private ReactiveCommand<Unit, Unit> _convertCurrencyCommand;

        public ReactiveCommand<Unit, Unit> ConvertCurrencyCommand => _convertCurrencyCommand ??= ReactiveCommand.Create(
            () =>
            {
                NavigationService?.ClosePopup();
                NavigationService?.SetInitiatedPage(TabNavigation.Exchange);
                NavigationService?.GoToExchange(Currency);
            });

        private ReactiveCommand<Unit, Unit> _buyCurrencyCommand;

        public ReactiveCommand<Unit, Unit> BuyCurrencyCommand => _buyCurrencyCommand ??= ReactiveCommand.Create(() =>
        {
            NavigationService?.ClosePopup();
            NavigationService?.GoToBuy(Currency);
        });

        private ICommand _refreshCommand;
        public ICommand RefreshCommand => _refreshCommand ??= new Command(async () => await ScanCurrency());

        private ICommand _closeBottomSheetCommand;

        public ICommand CloseBottomSheetCommand => _closeBottomSheetCommand ??= new Command(() =>
            NavigationService?.ClosePopup());

        public virtual async Task ScanCurrency()
        {
            CancellationTokenSource = new CancellationTokenSource();
            IsRefreshing = true;

            try
            {
                var scanner = new HdWalletScanner(App.Account);
                await scanner.ScanAsync(
                    currency: Currency.Name,
                    skipUsed: true,
                    cancellationToken: CancellationTokenSource.Token);

                await Task.Run(async () => await LoadTransactionsAsync());

                if (CancellationTokenSource is {IsCancellationRequested: false})
                    await Device.InvokeOnMainThreadAsync(() =>
                        NavigationService?.DisplaySnackBar(
                            MessageType.Regular,
                            Currency.Description + " " + AppResources.HasBeenUpdated));
            }
            catch (OperationCanceledException)
            {
                // nothing to do...
            }
            catch (Exception e)
            {
                Log.Error(e, "HdWalletScanner error for {Currency}", Currency?.Name);
            }
            finally
            {
                IsRefreshing = false;
            }
        }

        private ReactiveCommand<Unit, Unit> _cancelUpdateCommand;

        public ReactiveCommand<Unit, Unit> CancelUpdateCommand => _cancelUpdateCommand ??= ReactiveCommand.Create(() =>
        {
            try
            {
                CancellationTokenSource?.Cancel();
                CancellationTokenSource?.Dispose();
                CancellationTokenSource = null;
            }
            catch (Exception)
            {
                // ignored
            }
        });

        private ReactiveCommand<Unit, Unit> _showAvailableAmountCommand;

        public ReactiveCommand<Unit, Unit> ShowAvailableAmountCommand => _showAvailableAmountCommand ??=
            ReactiveCommand.Create(() =>
                NavigationService?.ShowPopup(new AvailableAmountPopup(this)));

        protected void CopyAddress(string value)
        {
            Device.InvokeOnMainThreadAsync(() =>
            {
                if (value != null)
                {
                    _ = Clipboard.SetTextAsync(value);
                    NavigationService?.DisplaySnackBar(MessageType.Regular, AppResources.AddressCopied);
                }
                else
                    NavigationService?.ShowAlert(AppResources.Error, AppResources.CopyError,
                        AppResources.AcceptButton);
            });
        }

        protected void CopyTxId(string value)
        {
            Device.InvokeOnMainThreadAsync(() =>
            {
                if (value != null)
                {
                    _ = Clipboard.SetTextAsync(value);
                    NavigationService?.DisplaySnackBar(
                        MessageType.Regular,
                        AppResources.TransactionIdCopied);
                }
                else
                    NavigationService?.ShowAlert(AppResources.Error,
                        AppResources.CopyError,
                        AppResources.AcceptButton);
            });
        }

        private ReactiveCommand<string, Unit> _changeCurrencyTabCommand;

        public ReactiveCommand<string, Unit> ChangeCurrencyTabCommand => _changeCurrencyTabCommand ??=
            ReactiveCommand.Create<string>((value) =>
            {
                Enum.TryParse(value, out CurrencyTab selectedTab);
                SelectedTab = selectedTab;
            });

        private ReactiveCommand<Unit, Unit> _showCurrencyActionBottomSheet;

        public ReactiveCommand<Unit, Unit> ShowCurrencyActionBottomSheet => _showCurrencyActionBottomSheet ??=
            ReactiveCommand.Create(() =>
                NavigationService?.ShowPopup(new CurrencyActionBottomSheet(this)));

        protected virtual void OnReceiveClick()
        {
            NavigationService?.ClosePopup();
            var receiveViewModel = new ReceiveViewModel(
                app: App,
                currency: Currency,
                navigationService: NavigationService);
            
            NavigationService?.ShowPopup(new ReceiveBottomSheet(receiveViewModel));
        }

        protected virtual void OnSendClick()
        {
            if (TotalAmount <= 0) return;
            NavigationService?.ClosePopup();

            NavigationService?.SetInitiatedPage(TabNavigation.Portfolio);
            var sendViewModel = SendViewModelCreator.CreateViewModel(App, this, NavigationService);
            if (Currency is BitcoinBasedConfig)
            {
                var selectOutputsViewModel = sendViewModel.SelectFromViewModel as SelectOutputsViewModel;
                NavigationService?.ShowPage(new SelectOutputsPage(selectOutputsViewModel), TabNavigation.Portfolio);
            }
            else
            {
                var selectAddressViewModel = sendViewModel.SelectFromViewModel as SelectAddressViewModel;
                NavigationService?.ShowPage(new SelectAddressPage(selectAddressViewModel), TabNavigation.Portfolio);
            }
        }

        public ICommand LoadMoreTxsCommand => new Command(async () => await LoadMoreTxs());

        private async Task LoadMoreTxs()
        {
            if (IsTxsLoading ||
                QtyDisplayedTxs >= Transactions.Count) return;

            IsTxsLoading = true;

            try
            {
                if (Transactions == null)
                    return;

                var txs = Transactions
                    .Skip(QtyDisplayedTxs)
                    .Take(LoadingStepTxs)
                    .ToList();

                if (!txs.Any())
                    return;

                var groups = txs
                    .GroupBy(p => p.LocalTime.Date)
                    .Select(g => new Grouping<TransactionViewModel>(g.Key,
                        new ObservableCollection<TransactionViewModel>(g)));

                var resultGroups = MergeGroups(GroupedTransactions.ToList(), groups.ToList());

                await Device.InvokeOnMainThreadAsync(() =>
                    {
                        GroupedTransactions = new ObservableCollection<Grouping<TransactionViewModel>>(
                            resultGroups ?? new ObservableCollection<Grouping<TransactionViewModel>>());
                        QtyDisplayedTxs += txs.Count;
                    }
                );
            }
            catch (Exception e)
            {
                Log.Error(e, "Error loading more txs for {@Currency} error", CurrencyCode);
            }
            finally
            {
                IsTxsLoading = false;
            }
        }

        private IEnumerable<Grouping<TransactionViewModel>> MergeGroups(
            List<Grouping<TransactionViewModel>> group1,
            List<Grouping<TransactionViewModel>> group2)
        {
            var result = new List<Grouping<TransactionViewModel>>();

            var i = 0;
            var j = 0;
            while (i < group1.Count && j < group2.Count)
            {
                if (group1[i].Date > group2[j].Date)
                {
                    result.Add(group1[i++]);
                }
                else if (group1[i].Date < group2[j].Date)
                {
                    result.Add(group2[j++]);
                }
                else
                {
                    var txs = group1[i]
                        .Concat(group2[j])
                        .OrderByDescending(t => t.LocalTime);

                    result.Add(new Grouping<TransactionViewModel>(group1[i].Date, txs));
                    i++;
                    j++;
                }
            }

            while (i < group1.Count)
            {
                result.Add(group1[i++]);
            }

            while (j < group2.Count)
            {
                result.Add(group2[j++]);
            }

            return result;
        }

        public virtual void Reset()
        {
            try
            {
                if (Transactions == null)
                    return;

                var txs = Transactions
                    .Take(_defaultQtyDisplayedTxs)
                    .ToList();

                if (!txs.Any())
                    return;

                var groups = txs
                    .GroupBy(p => p.LocalTime.Date)
                    .Select(g => new Grouping<TransactionViewModel>(g.Key,
                        new ObservableCollection<TransactionViewModel>(g)));

                Device.InvokeOnMainThreadAsync(() =>
                    {
                        GroupedTransactions = new ObservableCollection<Grouping<TransactionViewModel>>(
                            groups ?? new ObservableCollection<Grouping<TransactionViewModel>>());
                        QtyDisplayedTxs = _defaultQtyDisplayedTxs;
                    }
                );
            }
            catch (Exception e)
            {
                Log.Error(e, "Reset QtyDisplayedTxs error");
            }
        }

        #region IDisposable Support

        private bool _disposedValue;

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    if (_account != null)
                    {
                        _account.BalanceUpdated -= OnBalanceUpdatedEventHandler;
                        _account.UnconfirmedTransactionAdded -= OnUnconfirmedTransactionAdded;
                    }

                    if (QuotesProvider != null)
                        QuotesProvider.QuotesUpdated -= OnQuotesUpdatedEventHandler;

                    if (AddressesViewModel != null)
                        AddressesViewModel.AddressesChanged -= OnAddresesChangedEventHandler;
                }

                _account = null;
                Currency = null;

                _disposedValue = true;
            }
        }

        ~CurrencyViewModel()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion
    }
}