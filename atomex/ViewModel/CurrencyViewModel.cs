using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using atomex.ViewModel.TransactionViewModels;
using Atomex;
using Atomex.Blockchain;
using Atomex.Common;
using Atomex.Core;
using Serilog;
using Xamarin.Forms;

namespace atomex.ViewModel
{
    public class CurrencyViewModel : BaseViewModel
    {
        private IAtomexApp _app;

        public Currency Currency { get; set; }
        public string Name { get; set; }
        public string FullName { get; set; }
        public decimal Amount { get; set; }
        public decimal Price { get; set; }
        public decimal Cost { get; set; }
        public string Address { get; set; }

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
            set { _transactions = value; OnPropertyChanged(nameof(Transactions)); }
        }

        public CurrencyViewModel(IAtomexApp app)
        {
            _app = app;

            _app.Account.UnconfirmedTransactionAdded += UnconfirmedTransactionAdded;
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
            Log.Debug("LoadTransactionsAsync for {@currency}", Name);

            try
            {
                if (_app.Account == null)
                    return;

                var transactions = (await _app.Account
                    .GetTransactionsAsync(Name))
                    .ToList();

                await Device.InvokeOnMainThreadAsync(() =>
                {
                    Transactions = new ObservableCollection<TransactionViewModel>(
                        transactions.Select(t => TransactionViewModelCreator
                            .CreateViewModel(t))
                            .ToList()
                            .SortList((t1, t2) => t2.Time.CompareTo(t1.Time)));
                });
            }
            catch (Exception e)
            {
                Log.Error(e, "LoadTransactionAsync error for {@currency}", Name);
            }
        }
    }
}

