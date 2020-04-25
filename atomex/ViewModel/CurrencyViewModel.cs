using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
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
        private IAtomexApp App { get; set; }

        public Currency Currency { get; set; }

        public string CurrencyCode => Currency.Name;
        public string FeeCurrencyCode => Currency.FeeCode;
        public string BaseCurrencyCode => "USD";

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

        private string _freeExternalAddress;
        public string FreeExternalAddress
        {
            get => _freeExternalAddress;
            set { _freeExternalAddress = value; OnPropertyChanged(nameof(FreeExternalAddress)); }
        }

        private decimal _price;
        public decimal Price
        {
            get => _price;
            set { _price = value; OnPropertyChanged(nameof(Price)); }
        }

        private decimal _cost;
        public decimal Cost
        {
            get => _cost;
            set { _cost = value; OnPropertyChanged(nameof(Cost)); }
        }

        private float _portfolioPercent;
        public float PortfolioPercent
        {
            get => _portfolioPercent;
            set { _portfolioPercent = value; OnPropertyChanged(nameof(PortfolioPercent)); }
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
            App = app;
            SubscibeToServices();
        }

        private void SubscibeToServices()
        {
            App.Account.UnconfirmedTransactionAdded += UnconfirmedTransactionAdded;
            App.Account.BalanceUpdated += BalanceUpdated;
        }

        private async void BalanceUpdated(object sender, CurrencyEventArgs args)
        {
            try
            {
                if (Currency.Name.Equals(args.Currency))
                {
                    await UpdateBalanceAsync().ConfigureAwait(false);
                    await LoadTransactionsAsync().ConfigureAwait(false);
                    await UpdateFreeExternalAddressAsync().ConfigureAwait(false);
                }
            }
            catch (Exception e)
            {
                Log.Error(e, $"Error for currency {args.Currency}");
            }
        }

        public async Task UpdateBalanceAsync() {
            var balance = await App.Account
                .GetBalanceAsync(Currency.Name)
                .ConfigureAwait(false);

            TotalAmount = balance.Confirmed;
            OnPropertyChanged(nameof(TotalAmount));

            AvailableAmount = balance.Available;
            OnPropertyChanged(nameof(AvailableAmount));

            UnconfirmedAmount = balance.UnconfirmedIncome + balance.UnconfirmedOutcome;
            OnPropertyChanged(nameof(UnconfirmedAmount));
        }

        public async Task UpdateFreeExternalAddressAsync()
        {
            var walletAddress = await App.Account.GetFreeExternalAddressAsync(CurrencyCode);
            FreeExternalAddress = walletAddress.Address;
        }

        private void UnconfirmedTransactionAdded(
           object sender,
           TransactionEventArgs e)
        {
            try
            {
                if (e.Transaction.Currency.Name != Currency?.Name)
                    return;

                LoadTransactionsAsync().FireAndForget();
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
                if (App.Account == null)
                    return;

                var transactions = (await App.Account
                    .GetTransactionsAsync(Currency.Name))
                    .ToList();

                await Device.InvokeOnMainThreadAsync(() =>
                {
                    Transactions = new ObservableCollection<TransactionViewModel>(
                       transactions.Select(t => TransactionViewModelCreator
                           .CreateViewModel(t))
                           .ToList()
                           .SortList((t1, t2) => t2.Time.CompareTo(t1.Time)));
                    var groups = Transactions.GroupBy(p => p.Time.Date).Select(g => new Grouping<DateTime, TransactionViewModel>(g.Key, g));
                    GroupedTransactions = new ObservableCollection<Grouping<DateTime, TransactionViewModel>>(groups);
                    OnPropertyChanged(nameof(Transactions));
                    OnPropertyChanged(nameof(GroupedTransactions));
                });
            }
            catch (Exception e)
            {
                Log.Error(e, "LoadTransactionAsync error for {@currency}", Currency.Name);
            }
        }

        public IAtomexApp GetAtomexApp()
        {
            return App;
        }

        public async Task UpdateCurrencyAsync()
        {
            await new HdWalletScanner(App.Account).ScanAsync(Currency.Name);
        }
    }
}

