using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Reactive;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using atomex.Resources;
using atomex.Services;
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
using Rg.Plugins.Popup.Services;
using Serilog;
using Xamarin.Forms;

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
        protected IAtomexApp _app { get; set; }
        protected IAccount _account { get; set; }

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

        [Reactive] public ActiveTab CurrencyActiveTab { get; set; }

        public CurrencyViewModel(IAtomexApp app, CurrencyConfig currency, bool loadTransaction = true)
        {
            _app = app ?? throw new ArgumentNullException(nameof(_app));
            Currency = currency ?? throw new ArgumentNullException(nameof(Currency));
            ToastService = DependencyService.Get<IToastService>();

            SubscribeToUpdates(_app.Account);
            SubscribeToRatesProvider(_app.QuotesProvider);

            if (loadTransaction)
                _ = UpdateTransactionsAsync();

            _ = UpdateBalanceAsync();

            CurrencyActiveTab = ActiveTab.Activity;
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
                if (_app.Account == null)
                    return;

                var transactions = (await _app.Account
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

            await Navigation.PopAsync();
        }

        private ICommand _receiveCommand;
        public ICommand ReceiveCommand => _receiveCommand ??= new Command(() =>
        {
            if (PopupNavigation.Instance.PopupStack.Count > 0)
                _ = PopupNavigation.Instance.PopAsync();
            var receiveViewModel = new ReceiveViewModel(_app, Currency, Navigation);
            _ = PopupNavigation.Instance.PushAsync(new ReceiveBottomSheet(receiveViewModel));
        });

        private ICommand _sendCommand;
        public ICommand SendCommand => _sendCommand ??= new Command(() =>
        {
            if (AvailableAmount <= 0) return;

            var sendViewModel = SendViewModelCreator.CreateViewModel(_app, this);
            if (Currency is BitcoinBasedConfig)
            {
                var selectOutputsViewModel = sendViewModel.SelectFromViewModel as SelectOutputsViewModel;
                Navigation?.PushAsync(new SelectOutputsPage(selectOutputsViewModel));
            }
            else
            {
                var selectAddressViewModel = sendViewModel.SelectFromViewModel as SelectAddressViewModel;
                Navigation?.PushAsync(new SelectAddressPage(selectAddressViewModel));
            }
        });

        protected ICommand _convertPageCommand;
        public ICommand ConvertPageCommand => _convertPageCommand ??= new Command(() => NavigationService.ConvertCurrency(Currency));

        protected ICommand _addressesPageCommand;
        public virtual ICommand AddressesPageCommand => _addressesPageCommand ??= new Command(async () => await OnAddressesButtonClicked());

        private async Task OnAddressesButtonClicked()
        {
            await Navigation.PushAsync(new AddressesPage(new AddressesViewModel(_app, Currency, Navigation)));
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
            IsLoading = true;

            try
            {
                var scanner = new HdWalletScanner(_app.Account);
                await scanner.ScanAsync(
                    currency: Currency.Name,
                    skipUsed: true,
                    cancellationToken: Cancellation.Token);

                await UpdateTransactionsAsync();

                ToastService?.Show(Currency.Description + " " + AppResources.HasBeenUpdated, ToastPosition.Top, Application.Current.RequestedTheme.ToString());
            }
            catch (Exception e)
            {
                Log.Error(e, "HdWalletScanner error for {@currency}", Currency?.Name);
            }

            IsLoading = false;
        }

        private ICommand _unconfirmedAmountTappedCommand;
        public ICommand UnconfirmedAmountTappedCommand => _unconfirmedAmountTappedCommand ??= new Command(() => UnconfirmedAmountTapped());

        private void UnconfirmedAmountTapped()
        {
            ToastService?.Show(AppResources.UnconfirmedAmountLabel, ToastPosition.Top, Application.Current.RequestedTheme.ToString());
        }

        private ReactiveCommand<string, Unit> _changeCurrencyTabCommand;
        public ReactiveCommand<string, Unit> ChangeCurrencyTabCommand =>
            _changeCurrencyTabCommand ??= (_changeCurrencyTabCommand = ReactiveCommand.Create<string>((value) =>
            {
                Enum.TryParse(value, out ActiveTab selectedTab);
                CurrencyActiveTab = selectedTab;
            }));

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

