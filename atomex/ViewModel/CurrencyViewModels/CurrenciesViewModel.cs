using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using atomex.Views.TezosTokens;
using Atomex;
using Atomex.Abstract;
using Xamarin.Forms;

namespace atomex.ViewModel.CurrencyViewModels
{
    public class CurrenciesViewModel : BaseViewModel
    {
        private IAtomexApp _app { get; }
        public INavigation Navigation { get; set; }

        private CurrencyViewModel _selectedCurrency;
        public CurrencyViewModel SelectedCurrency
        {
            get => _selectedCurrency;
            set
            {
                if (value == null) return;

                _selectedCurrency = value;

                if (_selectedCurrency.CurrencyCode == TezosConfig.Xtz)
                {
                    Navigation.PushAsync(new TezosTokensListPage(TezosTokensViewModel));
                    return;
                }

                Navigation.PushAsync(new CurrencyPage(_selectedCurrency));
            }
        }

        private ICurrencies Currencies
        {
            get
            {
                return _app.Account.Currencies;
            }
        }

        public List<CurrencyViewModel> CurrencyViewModels { get; set; }

        public TezosTokensViewModel TezosTokensViewModel { get; set; }

        public CurrenciesViewModel(IAtomexApp app, bool restore = false)
        {
            _app = app ?? throw new ArgumentNullException(nameof(AtomexApp));
            TezosTokensViewModel = new TezosTokensViewModel(app, restore);
            CurrencyViewModels = new List<CurrencyViewModel>();
            //_ = FillCurrenciesAsync(restore);
        }

        //public void SetNavigation(INavigation navigation, INavigationService navigationService = null)
        //{
        //    Navigation = navigation;
        //    foreach (var c in CurrencyViewModels)
        //    {
        //        c.Navigation = navigation;
        //        c.NavigationService = navigationService;
        //    }

        //    TezosTokensViewModel.Navigation = navigation;
        //    TezosTokensViewModel.NavigationService = navigationService;
        //}

        //private async Task FillCurrenciesAsync(bool restore)
        //{
        //    await Task.WhenAll(Currencies.Select(c =>
        //    {
        //        var currency = CurrencyViewModelCreator.CreateViewModel(_app, c);

        //        if (currency.CurrencyCode == TezosConfig.Xtz)
        //            TezosTokensViewModel.TezosViewModel = currency;

        //        CurrencyViewModels.Add(currency);

        //        if (restore)
        //            _ = currency.UpdateCurrencyAsync();

        //        return Task.CompletedTask;
        //    }));
        //}
    }
}