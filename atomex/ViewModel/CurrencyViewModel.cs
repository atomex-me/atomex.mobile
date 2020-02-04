using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using atomex.ViewModel.TransactionViewModels;
using Atomex;
using Atomex.Common;
using Atomex.Core;
using Serilog;
using Xamarin.Forms;

namespace atomex.ViewModel
{
    public class CurrencyViewModel : BaseViewModel
    {
        private IAtomexApp App;

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
            App = app;
        }

        public async Task LoadTransactionsAsync()
        {
            Log.Debug("LoadTransactionsAsync for {@currency}", Name);

            try
            {
                if (App.Account == null)
                    return;

                var transactions = (await App.Account
                    .GetTransactionsAsync(Name))
                    .ToList();

                await Device.InvokeOnMainThreadAsync(() =>
                {
                    Transactions = new ObservableCollection<TransactionViewModel>(
                        transactions.Select(t => TransactionViewModelCreator
                            .CreateViewModel(t))
                            .ToList()
                            .SortList((t1, t2) => t2.Time.CompareTo(t1.Time))
                            .ForEachDo(t =>
                            {
                                //t.UpdateClicked += UpdateTransactonEventHandler;
                                //t.RemoveClicked += RemoveTransactonEventHandler;
                            }));
                });
            }
            catch (Exception e)
            {
                Log.Error(e, "LoadTransactionAsync error for {@currency}", Name);
            }
        }
    }
}

