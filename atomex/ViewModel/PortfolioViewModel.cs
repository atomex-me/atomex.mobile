using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;
using atomex.Common;
using atomex.ViewModel.CurrencyViewModels;
using atomex.Views;
using Atomex;
using Atomex.Common;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Rg.Plugins.Popup.Services;
using Serilog;
using Xamarin.Essentials;
using Xamarin.Forms;

namespace atomex.ViewModel
{
    public class PortfolioCurrency : BaseViewModel
    {
        public CurrencyViewModel CurrencyViewModel { get; set; }
        [Reactive] public bool IsSelected { get; set; }
        public Action<string, bool> OnChanged { get; set; }

        public PortfolioCurrency()
        {
            this.WhenAnyValue(vm => vm.IsSelected)
                .SubscribeInMainThread(flag =>
                {
                    OnChanged?.Invoke(CurrencyViewModel.CurrencyCode, flag);
                });
        }
    }

    public class PortfolioViewModel : BaseViewModel
    {
        private IAtomexApp _app { get; }
        public INavigationService NavigationService { get; set; }

        [Reactive] public IList<PortfolioCurrency> AllCurrencies { get; set; }
        [Reactive] public IList<CurrencyViewModel> UserCurrencies { get; set; }
        [Reactive] public decimal PortfolioValue { get; set; }

        private string _defaultCurrencies = "BTC,LTC,ETH,XTZ";

        public PortfolioViewModel(IAtomexApp app)
        {
            _app = app ?? throw new ArgumentNullException(nameof(AtomexApp));
            GetCurrencies();

            SubscribeToUpdates();
        }

        private void SubscribeToUpdates()
        {
            AllCurrencies.ForEachDo(c =>
            {
                c.CurrencyViewModel.AmountUpdated += OnAmountUpdatedEventHandler;
            });

            OnAmountUpdatedEventHandler(this, EventArgs.Empty);
        }

        private void OnAmountUpdatedEventHandler(object sender, EventArgs args)
        {
            PortfolioValue = AllCurrencies.Sum(c => c.CurrencyViewModel.TotalAmountInBase);
        }

        private void GetCurrencies()
        {
            try
            {
                var currencies = Preferences.Get("Currencies", string.Empty);
                
                if (string.IsNullOrEmpty(currencies))
                    Preferences.Set("Currencies", _defaultCurrencies);

                List<string> result = new List<string>();
                result = currencies != string.Empty
                    ? currencies.Split(',').ToList()
                    : _defaultCurrencies.Split(',').ToList();

                AllCurrencies = _app.Account?.Currencies
                    .Select(c =>
                    {
                        var currency = CurrencyViewModelCreator.CreateViewModel(_app, c);
                        currency.AmountUpdated += OnAmountUpdatedEventHandler;
                        var vm = new PortfolioCurrency
                        {
                            CurrencyViewModel = currency,
                            IsSelected = result.Contains(currency.CurrencyCode),
                            OnChanged = ChangeUserCurrencies
                        };
                        return vm;
                    })
                    .ToList() ?? new List<PortfolioCurrency>();

                UserCurrencies = AllCurrencies
                    .Where(c => c.IsSelected)
                    .Select(vm => vm.CurrencyViewModel)
                    .ToList();

                var a = UserCurrencies;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Get portfolio currencies error");
            }
        }

        private void ChangeUserCurrencies(string currency, bool isSelected)
        {
            try
            {
                if (string.IsNullOrEmpty(currency)) return;

                var currencies = Preferences.Get("Currencies", string.Empty);
                List<string> currenciesList = new List<string>();
                currenciesList = currencies.Split(',').ToList();

                if (isSelected && currenciesList.Any(item => item == currency))
                    return;

                if (isSelected)
                    currenciesList.Add(currency);
                else
                    currenciesList.RemoveAll(c => c.Contains(currency));
                
                var result = string.Join(",", currenciesList);
                Preferences.Set("Currencies", result);

                AllCurrencies?.Select(c => c.IsSelected == currenciesList.Contains(c.CurrencyViewModel.CurrencyCode));
                
                UserCurrencies = AllCurrencies?
                    .Where(c => c.IsSelected)
                    .Select(vm => vm.CurrencyViewModel)
                    .ToList();
            }
            catch (Exception e)
            {
                Log.Error(e, "Change portfolio currencies error");
            }
        }

        private ICommand _manageAssetsCommand;
        public ICommand ManageAssetsCommand => _manageAssetsCommand ??= new Command(() =>
        {
            _ = PopupNavigation.Instance.PushAsync(new ManageAssetsBottomSheet(this));
        });

        private ICommand _closeBottomSheetCommand;
        public ICommand CloseBottomSheetCommand => _closeBottomSheetCommand ??= new Command(() =>
        {
            if (PopupNavigation.Instance.PopupStack.Count > 0)
                _ = PopupNavigation.Instance.PopAsync();
        });
    }
}
