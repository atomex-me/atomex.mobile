using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using atomex.Resources;
using atomex.Services;
using atomex.ViewModel.SendViewModels;
using atomex.ViewModel.TransactionViewModels;
using atomex.Views.Send;
using Atomex;
using Atomex.Blockchain;
using Atomex.Common;
using Atomex.Core;
using Atomex.MarketData.Abstract;
using Atomex.Wallet;
using Atomex.Wallet.Abstract;
using Serilog;
using Xamarin.Forms;

namespace atomex.ViewModel.CurrencyViewModels
{
    public class CurrencyViewModel : BaseViewModel
    {
        protected IAtomexApp AtomexApp { get; set; }

        protected IAccount Account { get; set; }

        public INavigation Navigation { get; set; }

        public INavigationService NavigationService { get; set; }

        public IToastService ToastService { get; set; }

        public CurrencyConfig Currency { get; set; }

        public event EventHandler AmountUpdated;

        private ICurrencyQuotesProvider QuotesProvider { get; set; }

        public string CurrencyCode => Currency.Name;
        public string FeeCurrencyCode => Currency.FeeCode;
        public string BaseCurrencyCode => "USD";

        private decimal _totalAmount;
        public decimal TotalAmount
        {
            get => _totalAmount;
            set { _totalAmount = value; OnPropertyChanged(nameof(TotalAmount)); }
        }

        private decimal _totalAmountInBase;
        public decimal TotalAmountInBase
        {
            get => _totalAmountInBase;
            set { _totalAmountInBase = value; OnPropertyChanged(nameof(TotalAmountInBase)); }
        }

        private decimal _availableAmount;
        public decimal AvailableAmount
        {
            get => _availableAmount;
            set { _availableAmount = value; OnPropertyChanged(nameof(AvailableAmount)); }
        }

        private decimal _availableAmountInBase;
        public decimal AvailableAmountInBase
        {
            get => _availableAmountInBase;
            set { _availableAmountInBase = value; OnPropertyChanged(nameof(AvailableAmountInBase)); }
        }

        private decimal _unconfirmedAmount;
        public decimal UnconfirmedAmount
        {
            get => _unconfirmedAmount;
            set { _unconfirmedAmount = value; OnPropertyChanged(nameof(UnconfirmedAmount)); }
        }

        private decimal _unconfirmedAmountInBase;
        public decimal UnconfirmedAmountInBase
        {
            get => _unconfirmedAmountInBase;
            set { _unconfirmedAmountInBase = value; OnPropertyChanged(nameof(UnconfirmedAmountInBase)); }
        }

        public bool HasUnconfirmedAmount => UnconfirmedAmount != 0;

        private decimal _price;
        public decimal Price
        {
            get => _price;
            set { _price = value; OnPropertyChanged(nameof(Price)); }
        }

        private decimal _portfolioPercent;
        public decimal PortfolioPercent
        {
            get => _portfolioPercent;
            set { _portfolioPercent = value; OnPropertyChanged(nameof(PortfolioPercent)); }
        }

        private bool _isLoading = false;
        public bool IsLoading
        {
            get => _isLoading;
            set
            {
                if (_isLoading == value)
                    return;

                _isLoading = value;
                OnPropertyChanged(nameof(IsLoading));
            }
        }

        private ObservableCollection<TransactionViewModel> _transactions;
        public ObservableCollection<TransactionViewModel> Transactions
        {
            get => _transactions;
            set
            {
                _transactions = value;
                OnPropertyChanged(nameof(Transactions));
            }
        }

        private TransactionViewModel _selectedTransaction;
        public TransactionViewModel SelectedTransaction
        {
            get => _selectedTransaction;
            set
            {
                if (value == null) return;
                _selectedTransaction = value;

                Navigation.PushAsync(new TransactionInfoPage(_selectedTransaction));
            }
        }

        public class Grouping<K, T> : ObservableCollection<T>
        {
            public K Date { get; private set; }
            public Grouping(K date, IEnumerable<T> items)
            {
                Date = date;
                foreach (T item in items)
                    Items.Add(item);
            }
        }

        public ObservableCollection<Grouping<DateTime, TransactionViewModel>> GroupedTransactions { get; set; }

        private CancellationTokenSource Cancellation { get; set; }

        public CurrencyViewModel(IAtomexApp app, CurrencyConfig currency, bool loadTransaction = true)
        {
            AtomexApp = app ?? throw new ArgumentNullException(nameof(AtomexApp));
            Currency = currency ?? throw new ArgumentNullException(nameof(Currency));
            ToastService = DependencyService.Get<IToastService>();

            SubscribeToUpdates(AtomexApp.Account);
            SubscribeToRatesProvider(AtomexApp.QuotesProvider);

            if (loadTransaction)
                _ = UpdateTransactionsAsync();

            _ = UpdateBalanceAsync();
        }

        public void SubscribeToUpdates(IAccount account)
        {
            Account = account;
            Account.BalanceUpdated += OnBalanceChangedEventHandler;
            Account.UnconfirmedTransactionAdded += UnconfirmedTxAddedEventHandler;
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
                var balance = await AtomexApp.Account
                    .GetBalanceAsync(Currency.Name)
                    .ConfigureAwait(false);

                TotalAmount = balance.Confirmed;
                OnPropertyChanged(nameof(TotalAmount));

                AvailableAmount = balance.Available;
                OnPropertyChanged(nameof(AvailableAmount));

                UnconfirmedAmount = balance.UnconfirmedIncome + balance.UnconfirmedOutcome;
                OnPropertyChanged(nameof(UnconfirmedAmount));
                OnPropertyChanged(nameof(HasUnconfirmedAmount));

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
            var quote = quotesProvider.GetQuote(CurrencyCode, BaseCurrencyCode);

            Price = quote.Bid;
            TotalAmountInBase = TotalAmount * (quote?.Bid ?? 0m);
            AvailableAmountInBase = AvailableAmount * (quote?.Bid ?? 0m);
            UnconfirmedAmountInBase = UnconfirmedAmount * (quote?.Bid ?? 0m);

            OnPropertyChanged(nameof(Price));
            OnPropertyChanged(nameof(TotalAmountInBase));
            OnPropertyChanged(nameof(AvailableAmountInBase));
            OnPropertyChanged(nameof(UnconfirmedAmountInBase));

            AmountUpdated?.Invoke(this, EventArgs.Empty);
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
                if (AtomexApp.Account == null)
                    return;

                var transactions = (await AtomexApp.Account
                        .GetTransactionsAsync(Currency.Name))
                        .ToList();

                await Device.InvokeOnMainThreadAsync(() =>
                {
                    Transactions = new ObservableCollection<TransactionViewModel>(
                       transactions.Select(t => TransactionViewModelCreator
                           .CreateViewModel(t, Currency))
                           .ToList()
                           .SortList((t1, t2) => t2.LocalTime.CompareTo(t1.LocalTime))
                           .ForEachDo(t =>
                            {
                                t.RemoveClicked += RemoveTransactonEventHandler;
                            }));
                    var groups = Transactions.GroupBy(p => p.LocalTime.Date).Select(g => new Grouping<DateTime, TransactionViewModel>(g.Key, g));
                    GroupedTransactions = new ObservableCollection<Grouping<DateTime, TransactionViewModel>>(groups);

                    OnPropertyChanged(nameof(Transactions));
                    OnPropertyChanged(nameof(GroupedTransactions));
                });
            }
            catch (Exception e)
            {
                Log.Error(e, "LoadTransactionAsync error for {@currency}", Currency?.Name);
            }
        }

        public async void RemoveTransactonEventHandler(object sender, TransactionEventArgs args)
        {
            if (AtomexApp.Account == null)
                return;

            try
            {
                var txId = $"{args.Transaction.Id}:{Currency.Name}";

                var isRemoved = await AtomexApp.Account
                    .RemoveTransactionAsync(txId);

                if (isRemoved)
                    await UpdateTransactionsAsync();
            }
            catch (Exception e)
            {
                Log.Error(e, "Transaction remove error");
            }

            await Navigation.PopAsync();
        }

        protected ICommand _sendPageCommand;
        public ICommand SendPageCommand => _sendPageCommand ??= new Command(async () => await OnSendButtonClicked());

        protected ICommand _receivePageCommand;
        public virtual ICommand ReceivePageCommand => _receivePageCommand ??= new Command(async () => await OnReceiveButtonClicked());

        protected ICommand _convertPageCommand;
        public ICommand ConvertPageCommand => _convertPageCommand ??= new Command(async () => await OnConvertButtonClicked());

        protected ICommand _addressesPageCommand;
        public virtual ICommand AddressesPageCommand => _addressesPageCommand ??= new Command(async () => await OnAddressesButtonClicked());

        private async Task OnSendButtonClicked()
        {
            if (TotalAmount <= 0) return;

            var sendViewModel = SendViewModelCreator.CreateViewModel(AtomexApp, this);
            if (Currency is BitcoinBasedConfig)
            {
                var selectOutputsViewModel = sendViewModel.SelectFromViewModel as SelectOutputsViewModel;
                await Navigation.PushAsync(new SelectOutputsPage(selectOutputsViewModel));
            }
            else
            {
                var selectAddressViewModel = sendViewModel.SelectFromViewModel as SelectAddressViewModel;
                await Navigation.PushAsync(new SelectAddressPage(selectAddressViewModel));
            }
        }

        private async Task OnReceiveButtonClicked()
        {
            await Navigation.PushAsync(new ReceivePage(new ReceiveViewModel(AtomexApp, Currency, Navigation)));
        }

        private async Task OnConvertButtonClicked()
        {
            await NavigationService.ConvertCurrency(CurrencyCode);
        }

        private async Task OnAddressesButtonClicked()
        {
            await Navigation.PushAsync(new AddressesPage(new AddressesViewModel(AtomexApp, Currency, Navigation)));
        }

        private ICommand _selectTransactionCommand;
        public ICommand SelectTransactionCommand => _selectTransactionCommand ??= new Command<TransactionViewModel>(async (tx) => await OnTxItemTapped(tx));

        async Task OnTxItemTapped(TransactionViewModel tx)
        {
            if (tx != null)
                await Navigation.PushAsync(new TransactionInfoPage(tx));
        }

        private ICommand _updateCurrencyCommand;
        public ICommand UpdateCurrencyCommand => _updateCurrencyCommand ??= new Command(async () => await UpdateCurrencyAsync());

        public async Task UpdateCurrencyAsync()
        {
            Cancellation = new CancellationTokenSource();

            try
            {
                IsLoading = true;

                var scanner = new HdWalletScanner(AtomexApp.Account);

                await scanner.ScanAsync(
                    currency: Currency.Name,
                    skipUsed: true,
                    cancellationToken: Cancellation.Token);

                await UpdateTransactionsAsync();

                IsLoading = false;

                ToastService?.Show(Currency.Description + " " + AppResources.HasBeenUpdated, ToastPosition.Top, Application.Current.RequestedTheme.ToString());
            }
            catch (Exception e)
            {
                Log.Error(e, "HdWalletScanner error for {@currency}", Currency?.Name);
            }
        }

        private ICommand _unconfirmedAmountTappedCommand;
        public ICommand UnconfirmedAmountTappedCommand => _unconfirmedAmountTappedCommand ??= new Command(() => UnconfirmedAmountTapped());

        private void UnconfirmedAmountTapped()
        {
            ToastService?.Show(AppResources.UnconfirmedAmountLabel, ToastPosition.Top, Application.Current.RequestedTheme.ToString());
        }

        #region IDisposable Support
        private bool _disposedValue;

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    if (Account != null)
                    {
                        Account.BalanceUpdated -= OnBalanceChangedEventHandler;
                        Account.UnconfirmedTransactionAdded -= UnconfirmedTxAddedEventHandler;
                    }

                    if (QuotesProvider != null)
                        QuotesProvider.QuotesUpdated -= OnQuotesUpdatedEventHandler;
                }

                Account = null;
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

