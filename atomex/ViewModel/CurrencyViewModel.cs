using System;
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
        public decimal TotalAmount { get; set; }
        public decimal AvailableAmount { get; set; }
        public decimal UnconfirmedAmount { get; set; }
        public string FreeExternalAddress { get; set; }
        public string CurrencyCode => Currency.Name;
        public decimal Price { get; set; }
        public decimal Cost { get; set; }

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

        private async Task UpdateBalanceAsync() {
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
                    OnPropertyChanged(nameof(Transactions));
                });
            }
            catch (Exception e)
            {
                Log.Error(e, "LoadTransactionAsync error for {@currency}", Currency.Name);
            }
        }

        public async Task UpdateCurrencyAsync()
        {
            await new HdWalletScanner(App.Account).ScanAsync(Currency.Name);
        }

        public async Task<(decimal, decimal, decimal)> EstimateMaxAmount(string address)
        {
            return await App.Account
               .EstimateMaxAmountToSendAsync(Currency.Name, address, Atomex.Blockchain.Abstract.BlockchainTransactionType.Output)
               .ConfigureAwait(false);
        }
    }
}

