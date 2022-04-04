using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Reactive;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using atomex.Common;
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

        [Reactive] public decimal TotalAmount { get; set; }
        [Reactive] public decimal TotalAmountInBase { get; set; }
        [Reactive] public decimal AvailableAmount { get; set; }
        [Reactive] public decimal AvailableAmountInBase { get; set; }
        [Reactive] public decimal UnconfirmedAmount { get; set; }
        [Reactive] public decimal UnconfirmedAmountInBase { get; set; }
        public bool HasUnconfirmedAmount => UnconfirmedAmount != 0;
        [Reactive] public decimal Price { get; set; }

        [Reactive] public ObservableCollection<TransactionViewModel> Transactions { get; set; }
        [Reactive] public ObservableCollection<Grouping<DateTime, TransactionViewModel>> GroupedTransactions { get; set; }
        [Reactive] public TransactionViewModel SelectedTransaction { get; set; }

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

        [Reactive] public bool IsLoading { get; set; }
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

            this.WhenAnyValue(vm => vm.SelectedTransaction)
                .WhereNotNull()
                .SubscribeInMainThread(t =>
                    Navigation?.PushAsync(new TransactionInfoPage(t)));

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
            var quote = quotesProvider.GetQuote(CurrencyCode, BaseCurrencyCode);

            Device.InvokeOnMainThreadAsync(() =>
            {
                Price = quote.Bid;
                TotalAmountInBase = TotalAmount * (quote?.Bid ?? 0m);
                AvailableAmountInBase = AvailableAmount * (quote?.Bid ?? 0m);
                UnconfirmedAmountInBase = UnconfirmedAmount * (quote?.Bid ?? 0m);
            });

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

            await Navigation?.PopAsync();
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

        private ReactiveCommand<Unit, Unit> _addressesPageCommand;
        public ReactiveCommand<Unit, Unit> AddressesPageCommand =>
            _addressesPageCommand ??= (_addressesPageCommand = ReactiveCommand.CreateFromTask(() => Navigation?.PushAsync(new AddressesPage(new AddressesViewModel(_app, Currency, Navigation)))));

        private ICommand _updateCurrencyCommand;
        public ICommand UpdateCurrencyCommand => _updateCurrencyCommand ??= new Command(async () => await UpdateCurrencyAsync());

        public async Task UpdateCurrencyAsync()
        {
            var cancellation = new CancellationTokenSource();
            IsLoading = true;

            try
            {
                var scanner = new HdWalletScanner(_app.Account);
                await scanner.ScanAsync(
                    currency: Currency.Name,
                    skipUsed: true,
                    cancellationToken: cancellation.Token);

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

