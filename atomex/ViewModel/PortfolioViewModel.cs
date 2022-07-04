using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;
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

    public class PortfolioCurrencyViewModel : BaseViewModel
    {
        public CurrencyViewModel CurrencyViewModel { get; set; }
        [Reactive] public bool IsSelected { get; set; }
        public Action<string, bool> OnChanged { get; set; }

        public PortfolioCurrencyViewModel(
            CurrencyViewModel currencyViewModel,
            bool isSelected)
        {
            CurrencyViewModel = currencyViewModel;
            IsSelected = isSelected;

            this.WhenAnyValue(vm => vm.IsSelected)
                .WhereNotNull()
                .Skip(1)
                .SubscribeInMainThread(flag =>
                    OnChanged?.Invoke(CurrencyViewModel.CurrencyCode, flag));
        }

        private ICommand _toggleCommand;
        public ICommand ToggleCommand => _toggleCommand ??= ReactiveCommand.Create(() =>
            IsSelected = !IsSelected);
    }

    public class PortfolioViewModel : BaseViewModel
    {
        private IAtomexApp _app { get; }
        [Reactive] private INavigationService _navigationService { get; set; }

        [Reactive] public IList<PortfolioCurrencyViewModel> AllCurrencies { get; set; }
        [Reactive] public IList<CurrencyViewModel> UserCurrencies { get; set; }

        [Reactive] public decimal AvailableAmountInBase { get; set; }
        [Reactive] public decimal UnconfirmedAmountInBase { get; set; }

        public CurrencyActionType SelectCurrencyUseCase { get; set; }
        [Reactive] public CurrencyViewModel SelectedCurrency { get; set; }

        private bool _startCurrenciesScan = false;
        private string[] _currenciesForScan { get; set; }

        [Reactive] public bool IsRestoring { get; set; }

        public PortfolioViewModel(IAtomexApp app)
        {
            _app = app ?? throw new ArgumentNullException(nameof(app));
            
            this.WhenAnyValue(vm => vm.AllCurrencies)
                .WhereNotNull()
                .SubscribeInMainThread(async _ =>
                {
                    SubscribeToUpdates();

                    if (_startCurrenciesScan)
                        await StartCurrenciesScan();
                });

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

        private void SubscribeToUpdates()
        {
            AllCurrencies.ForEachDo(c =>
                c.CurrencyViewModel.AmountUpdated += OnAmountUpdatedEventHandler);

            OnAmountUpdatedEventHandler(this, EventArgs.Empty);
        }

        public void InitCurrenciesScan(string[] currenciesArr = null)
        {
            _startCurrenciesScan = true;
            _currenciesForScan = currenciesArr;
        }

        private async Task StartCurrenciesScan()
        {
            IsRestoring = true;

            var currencies = _currenciesForScan == null
                ? AllCurrencies
                    .Select(c => c.CurrencyViewModel)
                : AllCurrencies
                    .Where(curr => _currenciesForScan.Contains(curr.CurrencyViewModel.CurrencyCode))
                    .Select(c => c.CurrencyViewModel)
                    .ToList();
            try
            {
                var primaryCurrencies = currencies
                    .Where(curr => !curr.Currency.IsToken);

                var tokenCurrencies = currencies
                    .Where(curr => curr.Currency.IsToken);

                await Task.Run(() =>
                        Task.WhenAll(primaryCurrencies
                            .Select(currency => currency.ScanCurrency())));

                var tezosTokens = primaryCurrencies
                    .Where(c => c.HasTokens && c.CurrencyCode == "XTZ")
                    ?.Cast<TezosCurrencyViewModel>()
                    ?.FirstOrDefault()
                    ?.TezosTokensViewModel;

                await tezosTokens?.UpdateTokens();

                await Task.Run(() =>
                        Task.WhenAll(tokenCurrencies
                            .Select(currency => currency.ScanCurrency())));

            }
            catch (Exception e)
            {
                Log.Error(e, "Restore currencies error");
            }
            finally
            {
                IsRestoring = false;
            }
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
                var disabledCurrencies = _app.Account.UserData?.DisabledCurrencies ?? Array.Empty<string>();

                Device.InvokeOnMainThreadAsync(() =>
                {
                    AllCurrencies = _app.Account?.Currencies
                        .Select(c =>
                        {
                            var currency = CurrencyViewModelCreator.CreateOrGet(
                                currencyConfig: c,
                                navigationService: _navigationService,
                                subscribeToUpdates: true);

                            var vm = new PortfolioCurrencyViewModel(
                                currencyViewModel: currency,
                                isSelected: !disabledCurrencies.Contains(currency.CurrencyCode))
                            {
                                OnChanged = ChangeUserCurrencies
                            };
                            return vm;
                        })
                        .ToList() ?? new List<PortfolioCurrencyViewModel>();

                    UserCurrencies = AllCurrencies
                        .Where(c => c.IsSelected)
                        .Select(vm => vm.CurrencyViewModel)
                        .ToList();
                });
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
                
                var disabledCurrencies = AllCurrencies
                    .Where(c => !c.IsSelected)
                    .Select(currency => currency.CurrencyViewModel.CurrencyCode)
                    .ToArray();

                Device.InvokeOnMainThreadAsync(() =>
                {
                    UserCurrencies = AllCurrencies?
                        .Where(c => c.IsSelected)
                        .Select(vm => vm.CurrencyViewModel)
                        .ToList();
                });

                _app.Account.UserData.DisabledCurrencies = disabledCurrencies;
                _app.Account.UserData.SaveToFile(_app.Account.SettingsFilePath);
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

        private ReactiveCommand<Unit, Unit> _hideRestoringCommand;
        public ReactiveCommand<Unit, Unit> HideRestoringCommand => _hideRestoringCommand ??= ReactiveCommand.Create(() =>
        {
            IsRestoring = false;
        });
    }
}
