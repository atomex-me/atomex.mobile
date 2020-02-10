using System.Collections.Generic;
using System.Linq;
using Atomex;
using Atomex.Core;
using System.Threading.Tasks;
using Atomex.Common;
using System;

namespace atomex.ViewModel
{
    public class ConversionViewModel : BaseViewModel
    {
        private List<Currency> _coreCurrencies;
        private IAtomexApp _app;

        private decimal _amount;
        public decimal Amount
        {
            get => _amount; set { _amount = value; OnPropertyChanged(nameof(Amount)); }
        }
        private decimal _targetAmount;
        public decimal TargetAmount
        {
            get => _targetAmount; set { _targetAmount = value; OnPropertyChanged(nameof(TargetAmount)); }
        }
        private List<CurrencyViewModel> _currencyViewModels;
        private List<CurrencyViewModel> _fromCurrencies;
        public List<CurrencyViewModel> FromCurrencies
        {
            get => _fromCurrencies;
            private set { _fromCurrencies = value; OnPropertyChanged(nameof(FromCurrencies)); }
        }

        private List<CurrencyViewModel> _toCurrencies;
        public List<CurrencyViewModel> ToCurrencies
        {
            get => _toCurrencies;
            private set { _toCurrencies = value; OnPropertyChanged(nameof(ToCurrencies)); }
        }

        private CurrencyViewModel _fromCurrency;
        public CurrencyViewModel FromCurrency
        {
            get { return _fromCurrency; }
            set
            {
                _fromCurrency = value;
                OnPropertyChanged(nameof(FromCurrency));

                if (_fromCurrency == null)
                    return;

                var oldToCurrency = ToCurrency;

                ToCurrencies = _currencyViewModels
                    .Where(c => _app.Account.Symbols.SymbolByCurrencies(c.Currency, _fromCurrency.Currency) != null)
                    .ToList();

                if (oldToCurrency != null && oldToCurrency != _fromCurrency &&
                    ToCurrencies.FirstOrDefault(c => c.Name == oldToCurrency.Name) != null)
                {
                    ToCurrency = oldToCurrency;
                }
                else
                {
                    ToCurrency = ToCurrencies.First();  
                }
                Amount = 0;
            }
        }

        private CurrencyViewModel _toCurrency;
        public CurrencyViewModel ToCurrency
        {
            get { return _toCurrency; }
            set
            {
                _toCurrency = value;
                OnPropertyChanged(nameof(ToCurrency));
            }
        }

        public ConversionViewModel(IAtomexApp app)
        {
            _app = app;
            _coreCurrencies = app.Account.Currencies.ToList();
            FromCurrencies = ToCurrencies = _currencyViewModels = new List<CurrencyViewModel>();
            FillCurrenciesAsync().FireAndForget();
        }

        private async Task FillCurrenciesAsync()
        {
            await Task.WhenAll(_coreCurrencies.Select(async c =>
            {
                var balance = await _app.Account.GetBalanceAsync(c.Name);
                var address = await _app.Account.GetFreeExternalAddressAsync(c.Name);
                _currencyViewModels.Add(new CurrencyViewModel(_app)
                {
                    Currency = c,
                    Amount = balance.Available,
                    Name = c.Name,
                    FullName = c.Description,
                    Address = address.Address
                });
                //FromCurrencies = _currencyViewModels.ToList();
                FromCurrency = _currencyViewModels.FirstOrDefault();
            }));
        }
    }
}

