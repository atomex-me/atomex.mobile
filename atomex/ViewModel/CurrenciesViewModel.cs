using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Atomex;
using Atomex.Abstract;
using Atomex.Common;
using Serilog;

namespace atomex.ViewModel
{
    public class CurrenciesViewModel : BaseViewModel
    {
        public event EventHandler QuotesUpdated;

        private IAtomexApp App { get; }

        private decimal _totalAmountInBase;
        public decimal TotalAmountInBase
        {
            get => _totalAmountInBase;
            set { _totalAmountInBase = value; OnPropertyChanged(nameof(TotalAmountInBase)); }
        }


        private ICurrencies Currencies
        {
            get
            {
                return App.Account.Currencies;
            }
        }

        public List<CurrencyViewModel> CurrencyViewModels { get; set; }

        public CurrenciesViewModel(IAtomexApp app, bool restore)
        {
            App = app;
            CurrencyViewModels = new List<CurrencyViewModel>();
            FillCurrenciesAsync(restore).FireAndForget();
            SubscribeToServices();
        }

        private void SubscribeToServices()
        {
            App.QuotesProvider.QuotesUpdated += CurrencyUpdatedEventHandler;
            CurrencyUpdatedEventHandler(this, EventArgs.Empty);
        }

        private async Task FillCurrenciesAsync(bool restore)
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
                currency.CurrencyUpdated += CurrencyUpdatedEventHandler;

                if (restore)
                    currency.UpdateCurrencyAsync().FireAndForget();
                else
                {
                    currency.UpdateBalanceAsync().FireAndForget();
                    currency.LoadTransactionsAsync().FireAndForget();
                }
            }));
        }

        private void CurrencyUpdatedEventHandler(object sender, EventArgs e)
        {
            try
            {
                decimal totalAmount = 0;
                foreach (var c in CurrencyViewModels)
                {
                    var quote = App.QuotesProvider.GetQuote(c.CurrencyCode, c.BaseCurrencyCode);
                    c.Price = quote.Bid;
                    c.AmountInBase = c.AvailableAmount * quote.Bid;
                    totalAmount += c.AmountInBase;
                }

                TotalAmountInBase = totalAmount;

                if (TotalAmountInBase != 0)
                {
                    foreach (var c in CurrencyViewModels)
                    {
                        c.PortfolioPercent = (float)(c.AmountInBase / TotalAmountInBase * 100);
                    }
                }

                QuotesUpdated?.Invoke(this, EventArgs.Empty);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "CurrencyUpdatedEventHandler error");
            }
        }
    }
}