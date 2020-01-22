using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using atomex.Models;
using Atomex;
using Atomex.Common;
using Atomex.Core;

namespace atomex.ViewModel
{
    public class WalletsViewModel : BaseViewModel
    {
        public event EventHandler QuotesUpdated;

        public decimal TotalCost { get; set; }
        private IAtomexApp App { get; }

        private List<Wallet> wallets;
        private List<Currency> currencies;

        public List<Wallet> Wallets
        {
            get { return wallets; }
            set
            {
                if (wallets != value)
                {
                    wallets = value;
                    OnPropertyChanged(nameof(Wallets));
                }
            }
        }

        public WalletsViewModel(IAtomexApp app)
        {
            //Wallets = WalletData.Wallets;
            App = app;

            //var terminal = app.Terminal;
            //app.Terminal.SwapUpdated += Terminal_SwapUpdated;

            currencies = App.Account.Currencies.ToList();

            Wallets = new List<Wallet>();
            FillCurrenciesAsync().FireAndForget();

            App.QuotesProvider.QuotesUpdated += QuotesProvider_QuotesUpdated;
        }

        private async Task FillCurrenciesAsync()
        {
            await Task.WhenAll(currencies.Select(async c =>
            {
                var balance = await App.Account.GetBalanceAsync(c.Name);
                var address = await App.Account.GetFreeExternalAddressAsync(c.Name);
                Wallets.Add(new Wallet()
                {
                    Amount = balance.Available,
                    Name = c.Name,
                    FullName = c.Description,
                    Address = address.Address
                });
            }));
        }

        //private async Task GetAllTransactionsAsync()
        //{
        //    var transactions = await App.Account.GetTransactionsAsync("XTZ");
        //    Console.WriteLine(transactions);
        //    await Task.WhenAll(Wallets.Select(async w =>
        //    {
        //        var transactions = await App.Account.GetTransactionsAsync("XTZ");
        //        foreach (var transaction in transactions)
        //        {
        //            w.Transactions.Add(new Transaction()
        //            {
        //                Amount = transaction.
        //            });
        //        }
        //    }));
        //}


        private void QuotesProvider_QuotesUpdated(object sender, EventArgs e)
        {
            foreach (var wallet in Wallets)
            {
                var quote = App.QuotesProvider.GetQuote(wallet.Name, "USD");
                wallet.Price = quote.Bid;
                wallet.Cost = wallet.Amount * quote.Bid;
            }

            QuotesUpdated?.Invoke(this, EventArgs.Empty);
        }

        private void Terminal_SwapUpdated(object sender, Atomex.Swaps.SwapEventArgs e)
        {
            //var swap = e.Swap;
            //e.Swap;
            //throw new System.NotImplementedException();
        }
    }
}