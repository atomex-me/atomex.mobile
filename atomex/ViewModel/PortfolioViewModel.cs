using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Windows.Input;
using atomex.Common;
using atomex.ViewModel.CurrencyViewModels;
using atomex.ViewModel.SendViewModels;
using atomex.Views;
using atomex.Views.Send;
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
    public enum CurrencyActionType
    {
        [Description("Show")]
        Show,
        [Description("Send")]
        Send,
        [Description("Receive")]
        Receive
    }

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

        private ICommand _toggleCommand;
        public ICommand ToggleCommand => _toggleCommand ??= new Command(() => IsSelected = !IsSelected);
    }

    public class PortfolioViewModel : BaseViewModel
    {
        private IAtomexApp _app { get; }

        [Reactive] public INavigation Navigation { get; set; }
        [Reactive] public INavigationService NavigationService { get; set; }
        [Reactive] public IList<PortfolioCurrency> AllCurrencies { get; set; }
        [Reactive] public IList<CurrencyViewModel> UserCurrencies { get; set; }

        [Reactive] public decimal PortfolioValue { get; set; }

        public CurrencyActionType SelectCurrencyUseCase { get; set; }
        [Reactive] public CurrencyViewModel SelectedCurrency { get; set; }

        private string _defaultCurrencies = "BTC,LTC,ETH,XTZ";

        public PortfolioViewModel(IAtomexApp app)
        {
            _app = app ?? throw new ArgumentNullException(nameof(AtomexApp));
            GetCurrencies();

            this.WhenAnyValue(vm => vm.AllCurrencies)
                .WhereNotNull()
                .SubscribeInMainThread(_ => SubscribeToUpdates());

            this.WhenAnyValue(vm => vm.Navigation)
                .WhereNotNull()
                .SubscribeInMainThread(nav =>
                {
                    AllCurrencies
                        .Select(c =>
                        {
                            c.CurrencyViewModel.Navigation = nav;
                            return c;
                        })
                        .ToList();
                });

            this.WhenAnyValue(vm => vm.NavigationService)
                .WhereNotNull()
                .SubscribeInMainThread(service =>
                {
                    AllCurrencies
                        .Select(c =>
                        {
                            c.CurrencyViewModel.NavigationService = service;
                            return c;
                        })
                        .ToList();
                });

            this.WhenAnyValue(vm => vm.SelectedCurrency)
                .WhereNotNull()
                .SubscribeInMainThread(c =>
                {
                    switch (SelectCurrencyUseCase)
                    {
                        case CurrencyActionType.Show:
                            Navigation?.PushAsync(new CurrencyPage(c));
                            break;
                        case CurrencyActionType.Send:
                            if (PopupNavigation.Instance.PopupStack.Count > 0)
                                _ = PopupNavigation.Instance.PopAsync();
                            var sendViewModel = SendViewModelCreator.CreateViewModel(_app, c);
                            if (c.Currency is BitcoinBasedConfig)
                            {
                                var selectOutputsViewModel = sendViewModel.SelectFromViewModel as SelectOutputsViewModel;
                                Navigation?.PushAsync(new SelectOutputsPage(selectOutputsViewModel));
                            }
                            else
                            {
                                var selectAddressViewModel = sendViewModel.SelectFromViewModel as SelectAddressViewModel;
                                Navigation?.PushAsync(new SelectAddressPage(selectAddressViewModel));
                            }
                            SelectCurrencyUseCase = CurrencyActionType.Show;
                            break;
                        case CurrencyActionType.Receive:
                            if (PopupNavigation.Instance.PopupStack.Count > 0)
                                _ = PopupNavigation.Instance.PopAsync();
                            var receiveViewModel = new ReceiveViewModel(_app, c?.Currency, Navigation);
                            _ = PopupNavigation.Instance.PushAsync(new ReceiveBottomSheet(receiveViewModel));
                            SelectCurrencyUseCase = CurrencyActionType.Show;
                            break;
                        default:
                            Navigation?.PushAsync(new CurrencyPage(c));
                            break;
                    }
                });   
        }

        private void SubscribeToUpdates()
        {
            AllCurrencies.ForEachDo(c => c.CurrencyViewModel.AmountUpdated += OnAmountUpdatedEventHandler);
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
                        //currency.AmountUpdated += OnAmountUpdatedEventHandler;
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

        private ReactiveCommand<Unit, Unit> _manageAssetsCommand;
        public ReactiveCommand<Unit, Unit> ManageAssetsCommand => _manageAssetsCommand ??= ReactiveCommand.Create(() =>
        {
            _ = PopupNavigation.Instance.PushAsync(new ManageAssetsBottomSheet(this));
        });

        private ICommand _closeBottomSheetCommand;
        public ICommand CloseBottomSheetCommand => _closeBottomSheetCommand ??= new Command(() =>
        {
            if (PopupNavigation.Instance.PopupStack.Count > 0)
                _ = PopupNavigation.Instance.PopAsync();
        });


        private ReactiveCommand<Unit, Unit> _sendCommand;
        public ReactiveCommand<Unit, Unit> SendCommand => _sendCommand ??= ReactiveCommand.Create(() =>
        {
            SelectCurrencyUseCase = CurrencyActionType.Send;
            var currencies = AllCurrencies.Select(c => c.CurrencyViewModel);
            var selectCurrencyViewModel =
                new SelectCurrencyViewModel(CurrencyActionType.Send, currencies)
                {
                    OnSelected = currencyViewModel =>
                    {
                        SelectedCurrency = currencyViewModel;
                        SelectCurrencyUseCase = CurrencyActionType.Send;
                    }
                };

            SelectCurrencyUseCase = CurrencyActionType.Show;
            _ = PopupNavigation.Instance.PushAsync(new SelectCurrencyBottomSheet(selectCurrencyViewModel));
        });

        private ReactiveCommand<Unit, Unit> _receiveCommand;
        public ReactiveCommand<Unit, Unit> ReceiveCommand => _receiveCommand ??= ReactiveCommand.Create(() =>
        {
            SelectCurrencyUseCase = CurrencyActionType.Receive;
            var currencies = AllCurrencies.Select(c => c.CurrencyViewModel);
            var selectCurrencyViewModel =
                new SelectCurrencyViewModel(CurrencyActionType.Receive, currencies)
                {
                    OnSelected = currencyViewModel =>
                    {
                        SelectedCurrency = currencyViewModel;
                        SelectCurrencyUseCase = CurrencyActionType.Receive;
                    }
                };

            SelectCurrencyUseCase = CurrencyActionType.Show;
            _ = PopupNavigation.Instance.PushAsync(new SelectCurrencyBottomSheet(selectCurrencyViewModel));
        });
    }
}
