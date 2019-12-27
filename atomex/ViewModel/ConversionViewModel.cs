using System;
using System.Collections.Generic;
using atomex.Models;
using System.Linq;

namespace atomex.ViewModel
{
    public class ConversionViewModel : BaseViewModel
    {
        private float amount;
        public float Amount
        {
            get => amount; set { amount = value; OnPropertyChanged(nameof(Amount)); }
        }
        private List<Wallet> fromCurrencies;
        public List<Wallet> FromCurrencies
        {
            get => fromCurrencies;
            private set { fromCurrencies = value; OnPropertyChanged(nameof(FromCurrencies)); }
        }

        private List<Wallet> toCurrencies;
        public List<Wallet> ToCurrencies
        {
            get => toCurrencies;
            private set { toCurrencies = value; OnPropertyChanged(nameof(ToCurrencies)); }
        }

        private Wallet fromCurrency;
        public Wallet FromCurrency
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
                        ToCurrencies = fromCurrencies.Where(c => c.Name != fromCurrency.Name).ToList();
                    }
                    
                    OnPropertyChanged(nameof(FromCurrency));
                    Amount = 0;
                }
            }
        }

        private Wallet toCurrency;
        public Wallet ToCurrency
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

        public ConversionViewModel()
        {
            FromCurrencies = WalletData.Wallets;
            ToCurrencies = WalletData.Wallets;
            FromCurrency = new Wallet();
            ToCurrency = new Wallet();
        }
    }
}

