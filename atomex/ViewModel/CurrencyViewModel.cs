using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using atomex.Resources;
using atomex.Services;
using atomex.ViewModel.ReceiveViewModels;
using atomex.ViewModel.SendViewModels;
using atomex.ViewModel.TransactionViewModels;
using Atomex;
using Atomex.Blockchain;
using Atomex.Common;
using Atomex.Core;
using Atomex.Wallet;
using Serilog;
using Xamarin.Forms;

namespace atomex.ViewModel
{
    public class CurrencyViewModel : BaseViewModel
    {
        private IAtomexApp AtomexApp { get; set; }

        public INavigation Navigation { get; set; }

        public INavigationService NavigationService { get; set; }

        private IToastService ToastService;

        public Currency Currency { get; set; }

        public event EventHandler CurrencyUpdated;

        public string CurrencyCode => Currency.Name;
        public string FeeCurrencyCode => Currency.FeeCode;
        public string BaseCurrencyCode => "USD";
        public bool IsStakingAvailable => CurrencyCode == "XTZ";

        private decimal _totalAmount;
        public decimal TotalAmount
        {
            get => _totalAmount;
            set { _totalAmount = value; OnPropertyChanged(nameof(TotalAmount)); }
        }

        private decimal _availableAmount;
        public decimal AvailableAmount
        {
            get => _availableAmount;
            set { _availableAmount = value; OnPropertyChanged(nameof(AvailableAmount)); }
        }

        private decimal _unconfirmedAmount;
        public decimal UnconfirmedAmount
        {
            get => _unconfirmedAmount;
            set { _unconfirmedAmount = value; OnPropertyChanged(nameof(UnconfirmedAmount)); }
        }

        private decimal _price;
        public decimal Price
        {
            get => _price;
            set { _price = value; OnPropertyChanged(nameof(Price)); }
        }

        private decimal _amountInBase;
        public decimal AmountInBase
        {
            get => _amountInBase;
            set { _amountInBase = value; OnPropertyChanged(nameof(AmountInBase)); }
        }

        private float _portfolioPercent;
        public float PortfolioPercent
        {
            get => _portfolioPercent;
            set { _portfolioPercent = value; OnPropertyChanged(nameof(PortfolioPercent)); }
        }

        private float _opacity = 1f;
        public float Opacity
        {
            get => _opacity;
            set { _opacity = value; OnPropertyChanged(nameof(Opacity)); }
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

                if (_isLoading)
                    Opacity = 0.3f;
                else
                    Opacity = 1f;

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

        public CurrencyViewModel(IAtomexApp app)
        {
            AtomexApp = app ?? throw new ArgumentNullException(nameof(AtomexApp));
            ToastService = DependencyService.Get<IToastService>();
            SubscibeToServices();
        }

        private void SubscibeToServices()
        {
            AtomexApp.Account.UnconfirmedTransactionAdded += UnconfirmedTransactionAdded;
            AtomexApp.Account.BalanceUpdated += BalanceUpdated;
        }

        private async void BalanceUpdated(object sender, CurrencyEventArgs args)
        {
            try
            {
                if (Currency.Name.Equals(args.Currency))
                {
                    await UpdateBalanceAsync().ConfigureAwait(false);
                    await LoadTransactionsAsync().ConfigureAwait(false);
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

                AvailableAmount = balance.Available;

                UnconfirmedAmount = balance.UnconfirmedIncome + balance.UnconfirmedOutcome;

                var quote = AtomexApp.QuotesProvider.GetQuote(CurrencyCode, BaseCurrencyCode);
                Price = quote.Bid;
                AmountInBase = AvailableAmount * quote.Bid;
            }
            catch (Exception e)
            {
                Log.Error(e, "UpdateBalanceAsync error for {@currency}", Currency?.Name);
            }
        }

        private void UnconfirmedTransactionAdded(
           object sender,
           TransactionEventArgs e)
        {
            try
            {
                if (e.Transaction.Currency.Name != Currency?.Name)
                    return;

                _ = LoadTransactionsAsync();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "LoadTransactionAsync error for {@currency}", Currency?.Name);
            }
        }

        public async Task LoadTransactionsAsync()
        {
            Log.Debug("LoadTransactionsAsync for {@currency}", Currency.Name);

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
                           .CreateViewModel(t))
                           .ToList()
                           .SortList((t1, t2) => t2.LocalTime.CompareTo(t1.LocalTime)));
                    var groups = Transactions.GroupBy(p => p.LocalTime.Date).Select(g => new Grouping<DateTime, TransactionViewModel>(g.Key, g));
                    GroupedTransactions = new ObservableCollection<Grouping<DateTime, TransactionViewModel>>(groups);
                    OnPropertyChanged(nameof(Transactions));
                    OnPropertyChanged(nameof(GroupedTransactions));
                });

                CurrencyUpdated?.Invoke(this, EventArgs.Empty);
            }
            catch (Exception e)
            {
                Log.Error(e, "LoadTransactionAsync error for {@currency}", Currency.Name);
            }
        }

        public IAtomexApp GetAtomexApp()
        {
            return AtomexApp;
        }

        public async Task UpdateCurrencyAsync()
        {
            try
            {
                await new HdWalletScanner(AtomexApp.Account).ScanAsync(Currency.Name);
            }
            catch(Exception e)
            {
                Log.Error(e, "HdWalletScanner error for {@currency}", Currency?.Name);
            }
        }

        public async void RemoveTransactonAsync(string id)
        {
            if (AtomexApp.Account == null)
                return;

            try
            {
                var txId = $"{id}:{Currency.Name}";

                var isRemoved = await AtomexApp.Account
                    .RemoveTransactionAsync(txId);

                if (isRemoved)
                    await LoadTransactionsAsync();
            }
            catch (Exception e)
            {
                Log.Error(e, "Transaction remove error");
            }

            await Navigation.PopAsync();
        }

        private ICommand _sendPageCommand;
        public ICommand SendPageCommand => _sendPageCommand ??= new Command(async () => await OnSendButtonClicked());

        private ICommand _receivePageCommand;
        public ICommand ReceivePageCommand => _receivePageCommand ??= new Command(async () => await OnReceiveButtonClicked());

        private ICommand _stakingPageCommand;
        public ICommand StakingPageCommand => _stakingPageCommand ??= new Command(async () => await OnStakingButtonClicked());

        private ICommand _convertPageCommand;
        public ICommand ConvertPageCommand => _convertPageCommand ??= new Command(async () => await OnConvertButtonClicked());

        private async Task OnSendButtonClicked()
        {
            await Navigation.PushAsync(new SendPage(SendViewModelCreator.CreateViewModel(this)));
        }

        private async Task OnReceiveButtonClicked()
        {
            await Navigation.PushAsync(new ReceivePage(ReceiveViewModelCreator.CreateViewModel(this)));
        }

        private async Task OnStakingButtonClicked()
        {
            await Navigation.PushAsync(new DelegationsListPage(new DelegateViewModel(AtomexApp, Navigation)));
        }

        private async Task OnConvertButtonClicked()
        {
            await NavigationService.ConvertCurrency(CurrencyCode);
        }

        private ICommand _selectTransactionCommand;
        public ICommand SelectTransactionCommand => _selectTransactionCommand ??= new Command<TransactionViewModel>(async (tx) => await OnTxItemTapped(tx));

        async Task OnTxItemTapped(TransactionViewModel tx)
        {
            if (tx != null)
            {
                tx.CurrencyViewModel = this;
                await Navigation.PushAsync(new TransactionInfoPage(tx));
            }
        }

        private ICommand _updateCurrencyCommand;
        public ICommand UpdateCurrencyCommand => _updateCurrencyCommand ??= new Command(async () => await UpdateCurrency());

        private async Task UpdateCurrency()
        {
            try
            {
                IsLoading = true;
                await UpdateCurrencyAsync();
                IsLoading = false;
                ToastService?.Show(Currency.Description + " " + AppResources.HasBeenUpdated, ToastPosition.Top, Application.Current.RequestedTheme.ToString());
            }
            catch (Exception e)
            {
                Log.Error(e, "UpdateCurrencyAsync error");
            }
        }
    }
}

