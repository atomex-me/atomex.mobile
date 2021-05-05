using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Atomex;
using Atomex.Abstract;
using Xamarin.Forms;

namespace atomex.ViewModel
{
    public class CurrenciesViewModel : BaseViewModel
    {
        private IAtomexApp AtomexApp { get; }

        public INavigation Navigation { get; set; }

        private ICurrencies Currencies
        {
            get
            {
                return AtomexApp.Account.Currencies;
            }
        }

        public List<CurrencyViewModel> CurrencyViewModels { get; set; }

        public CurrenciesViewModel(IAtomexApp app, bool restore)
        {
            AtomexApp = app ?? throw new ArgumentNullException(nameof(AtomexApp));
            CurrencyViewModels = new List<CurrencyViewModel>();
            _ = FillCurrenciesAsync(restore);
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

                if (restore)
                    _ = currency.UpdateCurrencyAsync();
                else
                {
                    _ = currency.UpdateBalanceAsync();
                    _ = currency.UpdateTransactionsAsync();
                }
            }));
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