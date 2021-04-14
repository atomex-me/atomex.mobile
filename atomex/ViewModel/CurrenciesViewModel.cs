using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Atomex;
using Atomex.Abstract;
using Serilog;
using Xamarin.Forms;

namespace atomex.ViewModel
{
    public class CurrenciesViewModel : BaseViewModel
    {
        public event EventHandler QuotesUpdated;

        private IAtomexApp AtomexApp { get; }

        public INavigation Navigation { get; set; }

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
                return AtomexApp.Account.Currencies;
            }
        }

        public List<CurrencyViewModel> CurrencyViewModels { get; set; }

        public CurrencyViewModel SelectedCurrency { get; set; }

        public CurrenciesViewModel(IAtomexApp app, bool restore)
        {
            AtomexApp = app ?? throw new ArgumentNullException(nameof(AtomexApp));
            CurrencyViewModels = new List<CurrencyViewModel>();
            _ = FillCurrenciesAsync(restore);
            SubscribeToServices();
        }

        private void SubscribeToServices()
        {
            AtomexApp.QuotesProvider.QuotesUpdated += CurrencyUpdatedEventHandler;
            CurrencyUpdatedEventHandler(this, EventArgs.Empty);
        }

        public void SetNavigation(INavigation navigation, INavigationService navigationService = null)
        {
            Navigation = navigation;
            foreach (var c in CurrencyViewModels)
            {
                c.Navigation = navigation;
                c.NavigationService = navigationService;
            }
        }

        private async Task FillCurrenciesAsync(bool restore)
        {
            await Task.WhenAll(Currencies.Select(async c =>
            {
                var balance = await AtomexApp.Account.GetBalanceAsync(c.Name);

                CurrencyViewModel currency = new CurrencyViewModel(AtomexApp)
                {
                    Currency = c,
                    TotalAmount = balance.Confirmed,
                    AvailableAmount = balance.Available,
                    UnconfirmedAmount = balance.UnconfirmedIncome + balance.UnconfirmedOutcome,
                };
                CurrencyViewModels.Add(currency);
                currency.CurrencyUpdated += CurrencyUpdatedEventHandler;

                if (restore)
                    _ = currency.UpdateCurrencyAsync();
                else
                {
                    _ = currency.UpdateBalanceAsync();
                    _ = currency.LoadTransactionsAsync();
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
                    var quote = AtomexApp.QuotesProvider.GetQuote(c.CurrencyCode, c.BaseCurrencyCode);
                    c.Price = quote.Bid;
                    c.AmountInBase = c.TotalAmount * quote.Bid;
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

        private ICommand _selectCurrencyCommand;
        public ICommand SelectCurrencyCommand => _selectCurrencyCommand ??= new Command<CurrencyViewModel>(async (value) => await OnCurrencyTapped(value));

        private async Task OnCurrencyTapped(CurrencyViewModel currency)
        {
            if (currency != null)
                await Navigation.PushAsync(new CurrencyPage(currency));
        }
    }
}