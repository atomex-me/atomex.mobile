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

        public virtual CurrencyConfig Currency { get; set; }
        public event EventHandler AmountUpdated;
        private IQuotesProvider QuotesProvider { get; set; }

        public string CurrencyCode => Currency?.Name;
        public string CurrencyName => Currency?.DisplayedName;
        public string FeeCurrencyCode => Currency?.FeeCode;
        public string BaseCurrencyCode => "USD";
        public bool IsBitcoinBased => Currency is BitcoinBasedConfig;
        public bool IsStakingAvailable => Currency?.Name == "XTZ";
        public bool HasCollectibles => Currency?.Name == "XTZ";
        public bool HasTokens => Currency?.Name == "XTZ";
        public bool HasDapps { get; set; }
        public bool CanBuy { get; set; }

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

        [Reactive] public ObservableCollection<TransactionViewModel> Transactions { get; set; }

        [Reactive]
        public ObservableCollection<Grouping<TransactionViewModel>> GroupedTransactions { get; set; }

        [Reactive] public TransactionViewModel SelectedTransaction { get; set; }

        [Reactive] public AddressesViewModel AddressesViewModel { get; set; }
        [Reactive] public ObservableCollection<AddressViewModel> Addresses { get; set; }

        [Reactive] public bool CanShowMoreTxs { get; set; }
        [Reactive] public bool IsAllTxsShowed { get; set; }
        [Reactive] public bool CanShowMoreAddresses { get; set; }

        public int TxsNumberPerPage = 3;
        public int AddressesNumberPerPage = 3;

        public const double DefaultTxGroupHeight = 36;
        public double DefaultTxRowHeight => IsBitcoinBased ? 52 : 76;
        public const double DefaultAddressRowHeight = 64;
        public const double ListViewFooterHeight = 72;
        [Reactive] public double TxListViewHeight { get; set; }
        [Reactive] public double AddressListViewHeight { get; set; }

        public class Grouping<T> : ObservableCollection<T>
        {
            public double GroupHeight { get; set; } = DefaultTxGroupHeight;
            public DateTime Date { get; private set; }

            public Grouping(DateTime date, IEnumerable<T> items)
            {
                Date = date;

                foreach (T item in items)
                    Items.Add(item);
            }

            public string DateString => Date.ToString(AppResources.Culture.DateTimeFormat.MonthDayPattern, AppResources.Culture);
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

            this.WhenAnyValue(vm => vm.GroupedTransactions)
                .WhereNotNull()
                .SubscribeInMainThread(_ =>
                {
                    CanShowMoreTxs = Transactions!.Count > TxsNumberPerPage;

                    TxListViewHeight = IsAllTxsShowed
                        ? Transactions.Count * DefaultTxRowHeight +
                          GroupedTransactions!.Count * DefaultTxGroupHeight +
                          ListViewFooterHeight
                        : TxsNumberPerPage * DefaultTxRowHeight +
                          (GroupedTransactions!.Count + 1) * DefaultTxGroupHeight;
                });

            this.WhenAnyValue(vm => vm.Addresses)
                .WhereNotNull()
                .SubscribeInMainThread(_ =>
                {
                    CanShowMoreAddresses = AddressesViewModel?.Addresses?.Count > AddressesNumberPerPage;

                    AddressListViewHeight = AddressesNumberPerPage * DefaultAddressRowHeight +
                                            ListViewFooterHeight;
                });

            this.WhenAnyValue(vm => vm.AddressesViewModel)
                .WhereNotNull()
                .SubscribeInMainThread(_ =>
                {
                    AddressesViewModel!.AddressesChanged += OnAddresesChangedEventHandler;
                    OnAddresesChangedEventHandler();
                });

            LoadAddresses();
            _ = LoadTransactionsAsync();
            _ = UpdateBalanceAsync();

            IsRefreshing = false;
            IsAllTxsShowed = false;
            SelectedTab = CurrencyTab.Activity;
            CanBuy = BuyViewModel.Currencies.Contains(Currency.Name);
            HasDapps = false;
        }

        protected virtual void LoadAddresses()
        {
            AddressesViewModel?.Dispose();

            AddressesViewModel = new AddressesViewModel(
                app: App,
                currency: Currency,
                navigationService: NavigationService);
        }

        public virtual void SubscribeToServices()
        {
            App.Account.BalanceUpdated += OnBalanceUpdatedEventHandler;
            App.Account.UnconfirmedTransactionAdded += OnUnconfirmedTransactionAdded;
        }

        public void SubscribeToRatesProvider(IQuotesProvider quotesProvider)
        {
            QuotesProvider = quotesProvider;
            QuotesProvider.QuotesUpdated += OnQuotesUpdatedEventHandler;
        }

        protected virtual void OnAddresesChangedEventHandler()
        {
            try
            {
                Device.InvokeOnMainThreadAsync(() =>
                {
                    Addresses = new ObservableCollection<AddressViewModel>(
                        AddressesViewModel?.Addresses
                            .OrderByDescending(a => a.Balance)
                            .Take(AddressesNumberPerPage) ?? new List<AddressViewModel>());
                });
            }
            catch (Exception e)
            {
                Log.Error(e, "Error for currency {@Currency}", CurrencyCode);
            }
        }

        protected virtual async void OnBalanceUpdatedEventHandler(object sender, CurrencyEventArgs args)
        {
            try
            {
                if (args.Currency != Currency.Name)
                    return;

                await UpdateBalanceAsync();
                await LoadTransactionsAsync();
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

                await LoadTransactionsAsync();
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
                if (App.Account == null)
                    return;

                var transactions = (await App.Account
                    .GetTransactionsAsync(Currency?.Name))
                    .ToList();

                await Device.InvokeOnMainThreadAsync(() =>
                {
                    Transactions = new ObservableCollection<TransactionViewModel>(
                        transactions.Select(t => TransactionViewModelCreator
                                .CreateViewModel(t, Currency, NavigationService))
                            .ToList()
                            .SortList((t1, t2) => t2.LocalTime.CompareTo(t1.LocalTime))
                            .ForEachDo(t =>
                            {
                                t.RemoveClicked += RemoveTransactonEventHandler;
                                t.CopyAddress = CopyAddress;
                                t.CopyTxId = CopyTxId;
                            }));

                    var groups = !IsAllTxsShowed
                        ? Transactions
                            .OrderByDescending(p => p.LocalTime.Date)
                            .Take(TxsNumberPerPage)
                            .GroupBy(p => p.LocalTime.Date)
                            .Select(g => new Grouping<TransactionViewModel>(g.Key,
                                new ObservableCollection<TransactionViewModel>(g.OrderByDescending(t => t.LocalTime))))
                        : Transactions
                            .GroupBy(p => p.LocalTime.Date)
                            .OrderByDescending(g => g.Key)
                            .Select(g => new Grouping<TransactionViewModel>(g.Key,
                                new ObservableCollection<TransactionViewModel>(g.OrderByDescending(t => t.LocalTime))));

                    GroupedTransactions = new ObservableCollection<Grouping<TransactionViewModel>>(groups);
                });
            }
            catch (Exception e)
            {
                Log.Error(e, "LoadTransactionAsync error for {@Currency}", CurrencyCode);
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
                    await LoadTransactionsAsync();
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

        public ReactiveCommand<Unit, Unit> ConvertCurrencyCommand => _convertCurrencyCommand ??= ReactiveCommand.Create(() =>
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

                await LoadTransactionsAsync();
                
                if (CancellationTokenSource is {IsCancellationRequested: false})
                    await Device.InvokeOnMainThreadAsync(() =>
                    {
                        NavigationService?.DisplaySnackBar(
                            MessageType.Regular,
                            Currency.Description + " " + AppResources.HasBeenUpdated);
                    });
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
            catch (Exception )
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
                {
                    NavigationService?.ShowAlert(AppResources.Error, AppResources.CopyError,
                        AppResources.AcceptButton);
                }
            });
        }

        protected void CopyTxId(string value)
        {
            Device.InvokeOnMainThreadAsync(() =>
            {
                if (value != null)
                {
                    _ = Clipboard.SetTextAsync(value);
                    NavigationService?.DisplaySnackBar(MessageType.Regular, AppResources.TransactionIdCopied);
                }
                else
                {
                    NavigationService?.ShowAlert(AppResources.Error, AppResources.CopyError,
                        AppResources.AcceptButton);
                }
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

        private ReactiveCommand<Unit, Unit> _showAllTxsCommand;

        public ReactiveCommand<Unit, Unit> ShowAllTxsCommand => _showAllTxsCommand ??= ReactiveCommand.Create(() =>
        {
            IsAllTxsShowed = true;
            CanShowMoreTxs = false;
            TxsNumberPerPage = Transactions.Count;

            var groups = Transactions
                .GroupBy(p => p.LocalTime.Date)
                .OrderByDescending(g => g.Key)
                .Select(g => new Grouping<TransactionViewModel>(g.Key,
                    new ObservableCollection<TransactionViewModel>(g.OrderByDescending(t => t.LocalTime))));

            GroupedTransactions = new ObservableCollection<Grouping<TransactionViewModel>>(groups);
        });

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