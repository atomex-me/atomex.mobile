using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Atomex;
using Atomex.Abstract;
using Atomex.Common;

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


        private ICurrencies Currencies
        {
            get
            {
                return App.Account.Currencies;
            }
        }

        public List<CurrencyViewModel> CurrencyViewModels { get; set; }

        public CurrenciesViewModel(IAtomexApp app)
        {
            App = app;
            CurrencyViewModels = new List<CurrencyViewModel>();
            FillCurrenciesAsync().FireAndForget();
            SubscribeToServices();
        }

        private void SubscribeToServices()
        {
            App.QuotesProvider.QuotesUpdated += QuotesUpdatedEventHandler;
            QuotesUpdatedEventHandler(this, EventArgs.Empty);
        }

        private async Task FillCurrenciesAsync()
        {
            await Task.WhenAll(Currencies.Select(async c =>
            {
                var balance = await App.Account.GetBalanceAsync(c.Name);

                CurrencyViewModel currency = new CurrencyViewModel(App)
                {
                    Currency = c,
                    TotalAmount = balance.Confirmed,
                    AvailableAmount = balance.Available,
                    UnconfirmedAmount = balance.UnconfirmedIncome + balance.UnconfirmedOutcome,
                };
                CurrencyViewModels.Add(currency);
                currency.UpdateBalanceAsync().FireAndForget();
                currency.LoadTransactionsAsync().FireAndForget();
            }));
        }

        private void QuotesUpdatedEventHandler(object sender, EventArgs e)
        {
            TotalCost = 0;
            foreach (var c in CurrencyViewModels)
            {
                var quote = App.QuotesProvider.GetQuote(c.CurrencyCode, c.BaseCurrencyCode);
                c.Price = quote.Bid;
                c.AmountInBase = c.AvailableAmount * quote.Bid;
                TotalCost += c.AmountInBase;
            }

            if (TotalCost != 0)
            {
                foreach (var c in CurrencyViewModels)
                {
                    c.PortfolioPercent = (float)(c.AmountInBase / TotalCost * 100);
                }
            }

            QuotesUpdated?.Invoke(this, EventArgs.Empty);
        }
    }
}