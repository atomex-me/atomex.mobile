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
using atomex.Views.Popup;
using Atomex;
using Atomex.Common;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Serilog;
using Xamarin.Essentials;

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
        public ICommand ToggleCommand => _toggleCommand ??= ReactiveCommand.Create(() => IsSelected = !IsSelected);
    }

    public class PortfolioViewModel : BaseViewModel
    {
        private IAtomexApp _app { get; }
        [Reactive] private INavigationService _navigationService { get; set; }

        [Reactive] public IList<PortfolioCurrency> AllCurrencies { get; set; }
        [Reactive] public IList<CurrencyViewModel> UserCurrencies { get; set; }

        [Reactive] public decimal AvailableAmountInBase { get; set; }
        [Reactive] public decimal UnconfirmedAmountInBase { get; set; }

        public CurrencyActionType SelectCurrencyUseCase { get; set; }
        [Reactive] public CurrencyViewModel SelectedCurrency { get; set; }

        private string _walletName;
        private string _defaultCurrencies = "BTC,LTC,ETH,XTZ";

        public PortfolioViewModel(
            IAtomexApp app,
            string walletName,
            bool restore = false)
        {
            _app = app ?? throw new ArgumentNullException(nameof(app));
            _walletName = walletName;
            
            this.WhenAnyValue(vm => vm.AllCurrencies)
                .WhereNotNull()
                .SubscribeInMainThread(_ => SubscribeToUpdates(restore));

            this.WhenAnyValue(vm => vm._navigationService)
                .WhereNotNull()
                .SubscribeInMainThread(service => GetCurrencies());

            this.WhenAnyValue(vm => vm.SelectedCurrency)
                .WhereNotNull()
                .SubscribeInMainThread(c =>
                {
                    switch (SelectCurrencyUseCase)
                    {
                        case CurrencyActionType.Show:
                            _navigationService?.ShowPage(new CurrencyPage(c), TabNavigation.Portfolio);
                            SelectedCurrency = null;
                            break;
                        case CurrencyActionType.Send:
                            _navigationService?.CloseBottomSheet();
                            var sendViewModel = SendViewModelCreator.CreateViewModel(_app, c, _navigationService);
                            if (c.Currency is BitcoinBasedConfig)
                            {
                                var selectOutputsViewModel = sendViewModel.SelectFromViewModel as SelectOutputsViewModel;
                                _navigationService?.ShowPage(new SelectOutputsPage(selectOutputsViewModel), TabNavigation.Portfolio);
                            }
                            else
                            {
                                var selectAddressViewModel = sendViewModel.SelectFromViewModel as SelectAddressViewModel;
                                _navigationService?.ShowPage(new SelectAddressPage(selectAddressViewModel), TabNavigation.Portfolio);
                            }
                            SelectCurrencyUseCase = CurrencyActionType.Show;
                            SelectedCurrency = null;
                            break;
                        case CurrencyActionType.Receive:
                            var receiveViewModel = new ReceiveViewModel(_app, c?.Currency, _navigationService);
                            _navigationService?.ShowBottomSheet(new ReceiveBottomSheet(receiveViewModel));
                            SelectCurrencyUseCase = CurrencyActionType.Show;
                            SelectedCurrency = null;
                            break;
                        default:
                            _navigationService?.ShowPage(new CurrencyPage(c), TabNavigation.Portfolio);
                            SelectedCurrency = null;
                            break;
                    }
                });   
        }

        private void SubscribeToUpdates(bool restore = false)
        {
            AllCurrencies.ForEachDo(c =>
                c.CurrencyViewModel.AmountUpdated += OnAmountUpdatedEventHandler);

            if (restore)
                AllCurrencies.ForEachDo(async c =>
                    await c.CurrencyViewModel.ScanCurrency());

            OnAmountUpdatedEventHandler(this, EventArgs.Empty);
        }

        private void OnAmountUpdatedEventHandler(object sender, EventArgs args)
        {
            AvailableAmountInBase = AllCurrencies.Sum(c => c.CurrencyViewModel.TotalAmountInBase);
            UnconfirmedAmountInBase = AllCurrencies.Sum(c => c.CurrencyViewModel.UnconfirmedAmountInBase);
        }

        private void GetCurrencies()
        {
            try
            {
                var currencies = Preferences.Get(_walletName + "-" + "Currencies", string.Empty);
                
                if (string.IsNullOrEmpty(currencies))
                    Preferences.Set(_walletName + "-" + "Currencies", _defaultCurrencies);

                List<string> result = new List<string>();
                result = currencies != string.Empty
                    ? currencies.Split(',').ToList()
                    : _defaultCurrencies.Split(',').ToList();

                AllCurrencies = _app.Account?.Currencies
                    .Select(c =>
                    {
                        var currency = CurrencyViewModelCreator.CreateViewModel(_app, c, _navigationService);
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

        public void SetNavigationService(INavigationService service)
        {
            _navigationService = service ?? throw new ArgumentNullException(nameof(service));
        }

        private void ChangeUserCurrencies(string currency, bool isSelected)
        {
            try
            {
                if (string.IsNullOrEmpty(currency)) return;

                var currencies = Preferences.Get(_walletName + "-" + "Currencies", string.Empty);
                List<string> currenciesList = new List<string>();
                currenciesList = currencies.Split(',').ToList();

                if (isSelected && currenciesList.Any(item => item == currency))
                    return;

                if (isSelected)
                    currenciesList.Add(currency);
                else
                    currenciesList.RemoveAll(c => c.Contains(currency));
                
                var result = string.Join(",", currenciesList);
                Preferences.Set(_walletName + "-" + "Currencies", result);

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
        public ReactiveCommand<Unit, Unit> ManageAssetsCommand => _manageAssetsCommand ??= ReactiveCommand.Create(() => _navigationService?.ShowBottomSheet(new ManageAssetsBottomSheet(this)));

        private ReactiveCommand<Unit, Unit> _showAvailableAmountCommand;
        public ReactiveCommand<Unit, Unit> ShowAvailableAmountCommand => _showAvailableAmountCommand ??= ReactiveCommand.Create(() =>
            _navigationService?.ShowBottomSheet(new AvailableAmountPopup(this)));

        private ICommand _closeBottomSheetCommand;
        public ICommand CloseBottomSheetCommand => _closeBottomSheetCommand ??= ReactiveCommand.Create(() => _navigationService?.CloseBottomSheet());

        private ReactiveCommand<Unit, Unit> _sendCommand;
        public ReactiveCommand<Unit, Unit> SendCommand => _sendCommand ??= ReactiveCommand.Create(() =>
        {
            _navigationService?.SetInitiatedPage(TabNavigation.Portfolio);
            SelectCurrencyUseCase = CurrencyActionType.Send;
            var currencies = AllCurrencies.Select(c => c.CurrencyViewModel);
            var selectCurrencyViewModel =
                new SelectCurrencyViewModel(
                    CurrencyActionType.Send,
                    currencies,
                    _navigationService)
                {
                    OnSelected = currencyViewModel =>
                    {
                        SelectedCurrency = currencyViewModel;
                        SelectCurrencyUseCase = CurrencyActionType.Send;
                    }
                };

            SelectCurrencyUseCase = CurrencyActionType.Show;
            _navigationService?.ShowBottomSheet(new SelectCurrencyBottomSheet(selectCurrencyViewModel));
        });

        private ReactiveCommand<Unit, Unit> _receiveCommand;
        public ReactiveCommand<Unit, Unit> ReceiveCommand => _receiveCommand ??= ReactiveCommand.Create(() =>
        {
            SelectCurrencyUseCase = CurrencyActionType.Receive;
            var currencies = AllCurrencies.Select(c => c.CurrencyViewModel);
            var selectCurrencyViewModel =
                new SelectCurrencyViewModel(
                    CurrencyActionType.Receive,
                    currencies,
                    _navigationService)
                {
                    OnSelected = currencyViewModel =>
                    {
                        SelectedCurrency = currencyViewModel;
                        SelectCurrencyUseCase = CurrencyActionType.Receive;
                    }
                };

            SelectCurrencyUseCase = CurrencyActionType.Show;
            _navigationService?.ShowBottomSheet(new SelectCurrencyBottomSheet(selectCurrencyViewModel));
        });

        private ReactiveCommand<Unit, Unit> _exchangeCommand;
        public ReactiveCommand<Unit, Unit> ExchangeCommand => _exchangeCommand ??= ReactiveCommand.Create(() => _navigationService?.GoToExchange(null));
    }
}
