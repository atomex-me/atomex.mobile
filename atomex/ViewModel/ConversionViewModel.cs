using System.Collections.Generic;
using System.Linq;
using Atomex;
using Atomex.Core;
using System.Threading.Tasks;
using Atomex.Common;

namespace atomex.ViewModel
{
    public class ConversionViewModel : BaseViewModel
    {
        private List<Currency> coreCurrencies;
        private IAtomexApp App;

        private decimal amount;
        public decimal Amount
        {
            get => amount; set { amount = value; OnPropertyChanged(nameof(Amount)); }
        }
        private List<CurrencyViewModel> fromCurrencies;
        public List<CurrencyViewModel> FromCurrencies
        {
            get => fromCurrencies;
            private set { fromCurrencies = value; OnPropertyChanged(nameof(FromCurrencies)); }
        }

        private List<CurrencyViewModel> toCurrencies;
        public List<CurrencyViewModel> ToCurrencies
        {
            get => toCurrencies;
            private set { toCurrencies = value; OnPropertyChanged(nameof(ToCurrencies)); }
        }

        private CurrencyViewModel fromCurrency;
        public CurrencyViewModel FromCurrency
        {
            get { return fromCurrency; }
            set
            {
                if (fromCurrency != value)
                {
                    fromCurrency = value;
                    var _toCurrency = ToCurrency;
                    if (_toCurrency != null && _toCurrency != fromCurrency &&
                        ToCurrencies.FirstOrDefault(c => c.Name == _toCurrency.Name) != null)
                    {
                        ToCurrency = _toCurrency;
                    }
                    else
                    {
                        if (fromCurrencies != null)
                        {
                            ToCurrencies = fromCurrencies.Where(c => c.Name != fromCurrency.Name).ToList();
                        }
                    }
                    
                    OnPropertyChanged(nameof(FromCurrency));
                    Amount = 0;
                }
            }
        }

        private CurrencyViewModel toCurrency;
        public CurrencyViewModel ToCurrency
        {
            get { return toCurrency; }
            set
            {
                if (toCurrency != value)
                {
                    toCurrency = value;
                    //FromCurrencies = toCurrencies.Where(c => c.Name != toCurrency.Name).ToList();
                    OnPropertyChanged(nameof(ToCurrency));
                }
            }
        }

        public ConversionViewModel(IAtomexApp app)
        {
            App = app;
            coreCurrencies = app.Account.Currencies.ToList();
            FromCurrencies = ToCurrencies = new List<CurrencyViewModel>();
            FillCurrenciesAsync().FireAndForget();
            FromCurrency = FromCurrencies[0];
            ToCurrency = ToCurrencies[0];
        }

        private async Task FillCurrenciesAsync()
        {
            await Task.WhenAll(coreCurrencies.Select(async c =>
            {
                var balance = await App.Account.GetBalanceAsync(c.Name);
                var address = await App.Account.GetFreeExternalAddressAsync(c.Name);
                FromCurrencies.Add(new CurrencyViewModel(App)
                {
                    Amount = balance.Available,
                    Name = c.Name,
                    FullName = c.Description,
                    Address = address.Address
                });
                ToCurrencies = FromCurrencies;
            }));
        }
    }
}

