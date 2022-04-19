using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using atomex.Common;
using atomex.Resources;
using atomex.ViewModel.SendViewModels;
using atomex.ViewModel.TransactionViewModels;
using atomex.Views;
using atomex.Views.Send;
using Atomex;
using Atomex.Blockchain;
using Atomex.Common;
using Atomex.Core;
using Atomex.MarketData.Abstract;
using Atomex.Wallet;
using Atomex.Wallet.Abstract;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Serilog;
using Xamarin.Essentials;
using Xamarin.Forms;
using static atomex.Models.SnackbarMessage;

namespace atomex.ViewModel.CurrencyViewModels
{
    public enum ActiveTab
    {
        [Description("Activity")]
        Activity,
        [Description("Collectibles")]
        Collectibles,
        [Description("Addresses")]
        Addresses
    }

    public class CurrencyViewModel : BaseViewModel
    {
        public const double DefaultGroupHeight = 36;

        protected IAtomexApp _app { get; set; }
        protected IAccount _account { get; set; }
        protected INavigationService _navigationService { get; set; }

        public CurrencyConfig Currency { get; set; }
        public event EventHandler AmountUpdated;
        private ICurrencyQuotesProvider QuotesProvider { get; set; }

        public AddressesViewModel AddressesViewModel { get; set; }

        public string CurrencyCode => Currency.Name;
        public string FeeCurrencyCode => Currency.FeeCode;
        public string BaseCurrencyCode => "USD";

        [Reactive] public decimal TotalAmount { get; set; }
        [Reactive] public decimal TotalAmountInBase { get; set; }
        [Reactive] public decimal AvailableAmount { get; set; }
        [Reactive] public decimal AvailableAmountInBase { get; set; }
        [Reactive] public decimal DailyChangePercent { get; set; }
        [Reactive] public decimal UnconfirmedAmount { get; set; }
        [Reactive] public decimal UnconfirmedAmountInBase { get; set; }
        public bool HasUnconfirmedAmount => UnconfirmedAmount != 0;
        [Reactive] public decimal Price { get; set; }

        [Reactive] public ObservableCollection<TransactionViewModel> Transactions { get; set; }
        [Reactive] public ObservableCollection<Grouping<DateTime, TransactionViewModel>> GroupedTransactions { get; set; }
        [Reactive] public TransactionViewModel SelectedTransaction { get; set; }

        [Reactive] public bool CanShowMoreTxs { get; set; }
        [Reactive] public bool IsAllTxsShowed { get; set; }
        public int TxsNumberPerPage = 3;

        public bool CanBuy { get; set; }

        public class Grouping<K, T> : ObservableCollection<T>
        {
            public double GroupHeight { get; set; } = DefaultGroupHeight;
            public K Date { get; private set; }
            public Grouping(K date, IEnumerable<T> items)
            {
                Date = date;
                foreach (T item in items)
                    Items.Add(item);
            }
        }

        [Reactive] public bool IsRefreshing { get; set; }
        [Reactive] public ActiveTab CurrencyActiveTab { get; set; }

        public CurrencyViewModel(
            IAtomexApp app,
            CurrencyConfig currency,
            INavigationService navigationService,
            bool loadTransaction = true)
        {
            _app = app ?? throw new ArgumentNullException(nameof(_app));
            _navigationService = navigationService ?? throw new ArgumentNullException(nameof(_navigationService));
            Currency = currency ?? throw new ArgumentNullException(nameof(Currency));

            SubscribeToUpdates(_app.Account);
            SubscribeToRatesProvider(_app.QuotesProvider);

            IsAllTxsShowed = false;

            if (loadTransaction)
                _ = UpdateTransactionsAsync();

            _ = UpdateBalanceAsync();

            this.WhenAnyValue(vm => vm.SelectedTransaction)
                .WhereNotNull()
                .SubscribeInMainThread(t =>
                {
                    _navigationService?.ShowPage(new TransactionInfoPage(t), TabNavigation.Portfolio);
                    SelectedTransaction = null;
                });

            this.WhenAnyValue(vm => vm.Transactions)
                .WhereNotNull()
                .SubscribeInMainThread(txs => CanShowMoreTxs = txs.Count > TxsNumberPerPage);

            GetAddresses();
            CurrencyActiveTab = ActiveTab.Activity;
            CanBuy = BuyViewModel.Currencies.Contains(Currency.Name);
        }

        protected void GetAddresses()
        {
            AddressesViewModel = new AddressesViewModel(_app, Currency, _navigationService);
        }

        public void SubscribeToUpdates(IAccount account)
        {
            _account = account;
            _account.BalanceUpdated += OnBalanceChangedEventHandler;
            _account.UnconfirmedTransactionAdded += UnconfirmedTxAddedEventHandler;
        }

        public void SubscribeToRatesProvider(ICurrencyQuotesProvider quotesProvider)
        {
            QuotesProvider = quotesProvider;
            QuotesProvider.QuotesUpdated += OnQuotesUpdatedEventHandler;
        }

        private async void OnBalanceChangedEventHandler(object sender, CurrencyEventArgs args)
        {
            try
            {
                if (Currency.Name.Equals(args.Currency))
                {
                    await UpdateBalanceAsync().ConfigureAwait(false);
                    await UpdateTransactionsAsync().ConfigureAwait(false);
                }
            }
            catch (Exception e)
            {
                Log.Error(e, $"Error for currency {args.Currency}");
            }
        }

        public async Task UpdateBalanceAsync()
        {
            try
            {
                var balance = await _app.Account
                    .GetBalanceAsync(Currency.Name)
                    .ConfigureAwait(false);

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
                Log.Error(e, "UpdateBalanceAsync error for {@currency}", Currency?.Name);
            }
        }

        private void OnQuotesUpdatedEventHandler(object sender, EventArgs args)
        {
            if (!(sender is ICurrencyQuotesProvider quotesProvider))
                return;

            UpdateQuotesInBaseCurrency(quotesProvider);
        }

        private void UpdateQuotesInBaseCurrency(ICurrencyQuotesProvider quotesProvider)
        {
            var quote = quotesProvider?.GetQuote(CurrencyCode, BaseCurrencyCode);

            Device.InvokeOnMainThreadAsync(() =>
            {
                Price = quote.Bid;
                DailyChangePercent = quote.DailyChangePercent;
                TotalAmountInBase = TotalAmount * (quote?.Bid ?? 0m);
                AvailableAmountInBase = AvailableAmount * (quote?.Bid ?? 0m);
                UnconfirmedAmountInBase = UnconfirmedAmount * (quote?.Bid ?? 0m);
                
                AmountUpdated?.Invoke(this, EventArgs.Empty);
            });
        }

        private async void UnconfirmedTxAddedEventHandler(object sender, TransactionEventArgs e)
        {
            try
            {
                if (e.Transaction.Currency != Currency?.Name)
                    return;

                await UpdateTransactionsAsync();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "UnconfirmedTxAddedEventHandler error for {@currency}", Currency?.Name);
            }
        }

        public virtual async Task UpdateTransactionsAsync()
        {
            Log.Debug("UpdateTransactionsAsync for {@currency}", Currency.Name);

            try
            {
                if (_app.Account == null)
                    return;

                var transactions = (await _app.Account
                        .GetTransactionsAsync(Currency.Name))
                        .ToList();

                await Device.InvokeOnMainThreadAsync(() =>
                {
                    Transactions = new ObservableCollection<TransactionViewModel>(
                       transactions.Select(t => TransactionViewModelCreator
                           .CreateViewModel(t, Currency, _navigationService))
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
                           .Select(g => new Grouping<DateTime, TransactionViewModel>(g.Key, new ObservableCollection<TransactionViewModel>(g.OrderByDescending(g => g.LocalTime))))
                        : Transactions
                            .GroupBy(p => p.LocalTime.Date)
                            .OrderByDescending(g => g.Key)
                            .Select(g => new Grouping<DateTime, TransactionViewModel>(g.Key, new ObservableCollection<TransactionViewModel>(g.OrderByDescending(g => g.LocalTime))));

                    GroupedTransactions = new ObservableCollection<Grouping<DateTime, TransactionViewModel>>(groups);
                });
            }
            catch (Exception e)
            {
                Log.Error(e, "LoadTransactionAsync error for {@currency}", Currency?.Name);
            }
        }

        public async void RemoveTransactonEventHandler(object sender, TransactionEventArgs args)
        {
            if (_app.Account == null)
                return;

            try
            {
                var txId = $"{args.Transaction.Id}:{Currency.Name}";

                var isRemoved = await _app.Account
                    .RemoveTransactionAsync(txId);

                if (isRemoved)
                    await UpdateTransactionsAsync();
            }
            catch (Exception e)
            {
                Log.Error(e, "Transaction remove error");
            }

            _navigationService?.ClosePage(TabNavigation.Portfolio);
        }

        private ReactiveCommand<Unit, Unit> _receiveCommand;
        public ReactiveCommand<Unit, Unit> ReceiveCommand => _receiveCommand ??= ReactiveCommand.Create(() =>
            {
                var receiveViewModel = new ReceiveViewModel(_app, Currency, _navigationService);
                _navigationService?.ShowBottomSheet(new ReceiveBottomSheet(receiveViewModel));
            });

        private ReactiveCommand<Unit, Unit> _sendCommand;
        public ReactiveCommand<Unit, Unit> SendCommand => _sendCommand ??= ReactiveCommand.Create(() =>
            {
                if (AvailableAmount <= 0) return;

                var sendViewModel = SendViewModelCreator.CreateViewModel(_app, this, _navigationService);
                if (Currency is BitcoinBasedConfig)
                {
                    var selectOutputsViewModel = sendViewModel.SelectFromViewModel as SelectOutputsViewModel;
                    _navigationService?.ShowPage(new SelectOutputsPage(selectOutputsViewModel), TabNavigation.Portfolio);
                }
                else
                {
                    var selectAddressViewModel = sendViewModel.SelectFromViewModel as SelectAddressViewModel;
                    _navigationService?.ShowPage(new SelectAddressPage(selectAddressViewModel), TabNavigation.Portfolio);
                }
            });

        protected ReactiveCommand<Unit, Unit> _convertCurrencyCommand;
        public ReactiveCommand<Unit, Unit> ConvertCurrencyCommand => _convertCurrencyCommand ??= ReactiveCommand.Create(() =>
            _navigationService?.GoToExchange(Currency));

        protected ReactiveCommand<Unit, Unit> _buyCurrencyCommand;
        public ReactiveCommand<Unit, Unit> BuyCurrencyCommand => _buyCurrencyCommand ??= ReactiveCommand.Create(() =>
            _navigationService?.GoToBuy(Currency));

        private ICommand _refreshCommand;
        public ICommand RefreshCommand => _refreshCommand ??= new Command(async () => await ScanCurrency());

        public async Task ScanCurrency()
        {
            var cancellation = new CancellationTokenSource();
            IsRefreshing = true;
            this.RaisePropertyChanged(nameof(IsRefreshing));

            try
            {
                var scanner = new HdWalletScanner(_app.Account);
                await scanner.ScanAsync(
                    currency: Currency.Name,
                    skipUsed: true,
                    cancellationToken: cancellation.Token);

                await UpdateTransactionsAsync();

                _navigationService?.DisplaySnackBar(MessageType.Regular, Currency.Description + " " + AppResources.HasBeenUpdated);
            }
            catch (Exception e)
            {
                Log.Error(e, "HdWalletScanner error for {@currency}", Currency?.Name);
            }

            IsRefreshing = false;
        }

        //private ReactiveCommand<Unit, Unit> _unconfirmedAmountTappedCommand;
        //public ReactiveCommand<Unit, Unit> UnconfirmedAmountTappedCommand => _unconfirmedAmountTappedCommand ??= ReactiveCommand.Create(() =>
        //    ToastService?.Show(AppResources.UnconfirmedAmountLabel, ToastPosition.Top, Application.Current.RequestedTheme.ToString()));

        private void CopyAddress(string value)
        {
            if (value != null)
            {
                _ = Clipboard.SetTextAsync(value);
                _navigationService?.DisplaySnackBar(MessageType.Regular, AppResources.AddressCopied);
            }
            else
            {
                _navigationService?.ShowAlert(AppResources.Error, AppResources.CopyError, AppResources.AcceptButton);
            }
        }

        private void CopyTxId(string value)
        {
            if (value != null)
            {
                _ = Clipboard.SetTextAsync(value);
                _navigationService?.DisplaySnackBar(MessageType.Regular, AppResources.TransactionIdCopied);
            }
            else
            {
                _navigationService?.ShowAlert(AppResources.Error, AppResources.CopyError, AppResources.AcceptButton);
            }
        }

        private ReactiveCommand<string, Unit> _changeCurrencyTabCommand;
        public ReactiveCommand<string, Unit> ChangeCurrencyTabCommand => _changeCurrencyTabCommand ??= ReactiveCommand.Create<string>((value) =>
            {
                Enum.TryParse(value, out ActiveTab selectedTab);
                CurrencyActiveTab = selectedTab;
            });


        public void ShowAllTxs()
        {
            IsAllTxsShowed = true;
            CanShowMoreTxs = false;

            var groups = Transactions
                .GroupBy(p => p.LocalTime.Date)
                .OrderByDescending(g => g.Key)
                .Select(g => new Grouping<DateTime, TransactionViewModel>(g.Key, new ObservableCollection<TransactionViewModel>(g.OrderByDescending(g => g.LocalTime))));

            GroupedTransactions = new ObservableCollection<Grouping<DateTime, TransactionViewModel>>(groups);
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
                        _account.BalanceUpdated -= OnBalanceChangedEventHandler;
                        _account.UnconfirmedTransactionAdded -= UnconfirmedTxAddedEventHandler;
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