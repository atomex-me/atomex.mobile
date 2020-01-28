using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Atomex;
using Atomex.Common;
using Atomex.Core;

namespace atomex.ViewModel
{
    public class CurrenciesViewModel : BaseViewModel
    {
        public event EventHandler QuotesUpdated;

        private IAtomexApp App { get; }

        private decimal _totalCost;
        public decimal TotalCost
        {
            get => _totalCost;
            set { _totalCost = value; OnPropertyChanged(nameof(TotalCost)); }
        }

        private List<Currency> coreCurrencies;

        private List<CurrencyViewModel> currencies;

        public List<CurrencyViewModel> Currencies
        {
            get { return currencies; }
            set
            {
                if (currencies != value)
                {
                    currencies = value;
                    OnPropertyChanged(nameof(Currencies));
                }
            }
        }

        public CurrenciesViewModel(IAtomexApp app)
        {
            //Currencies = WalletData.Wallets;
            App = app;

            //var terminal = app.Terminal;
            //app.Terminal.SwapUpdated += Terminal_SwapUpdated;

            coreCurrencies = App.Account.Currencies.ToList();
            Currencies = new List<CurrencyViewModel>();
            FillCurrenciesAsync().FireAndForget();

            App.QuotesProvider.QuotesUpdated += QuotesProvider_QuotesUpdated;
        }

        private async Task FillCurrenciesAsync()
        {
            await Task.WhenAll(coreCurrencies.Select(async c =>
            {
                var balance = await App.Account.GetBalanceAsync(c.Name);
                var address = await App.Account.GetFreeExternalAddressAsync(c.Name);

                CurrencyViewModel currency = new CurrencyViewModel(App)
                {
                    Amount = balance.Available,
                    Name = c.Name,
                    FullName = c.Description,
                    Address = address.Address
                };
                Currencies.Add(currency);
                currency.LoadTransactionsAsync().FireAndForget();
            }));
        }

        private void QuotesProvider_QuotesUpdated(object sender, EventArgs e)
        {
            TotalCost = 0;
            foreach (var c in Currencies)
            {
                var quote = App.QuotesProvider.GetQuote(c.Name, "USD");
                c.Price = quote.Bid;
                c.Cost = c.Amount * quote.Bid;
                TotalCost += c.Cost;
            }

            if (TotalCost != 0)
            {
                foreach (var c in Currencies)
                {
                    c.PortfolioPercent = (float)(c.Cost / TotalCost * 100);
                }
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