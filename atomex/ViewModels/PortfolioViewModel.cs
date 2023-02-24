using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using atomex.Common;
using atomex.Views;
using atomex.Views.Popup;
using Atomex;
using Atomex.Common;
using atomex.Models;
using atomex.ViewModels.CurrencyViewModels;
using atomex.ViewModels.SendViewModels;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Serilog;
using Xamarin.Forms;

namespace atomex.ViewModels
{
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
        private readonly IAtomexApp _app;
        [Reactive] private INavigationService NavigationService { get; set; }

        [Reactive] public IList<PortfolioCurrencyViewModel> AllCurrencies { get; set; }
        [Reactive] public IList<CurrencyViewModel> UserCurrencies { get; set; }

        [Reactive] public decimal AvailableAmountInBase { get; set; }
        [Reactive] public decimal UnconfirmedAmountInBase { get; set; }

        public CurrencyActionType SelectCurrencyUseCase { get; set; }
        [Reactive] public CurrencyViewModel SelectedCurrency { get; set; }

        [Reactive] public string[] CurrenciesForScan { get; set; }
        [Reactive] public bool Restore { get; set; }
        [Reactive] public bool IsRestoring { get; set; }

        public event EventHandler CurrenciesLoaded;

        public PortfolioViewModel(IAtomexApp app)
        {
            _app = app ?? throw new ArgumentNullException(nameof(app));

            this.WhenAnyValue(vm => vm.AllCurrencies)
                .WhereNotNull()
                .SubscribeInMainThread(async _ =>
                {
                    SubscribeToUpdates();
                    CurrenciesLoaded?.Invoke(this, EventArgs.Empty);
                    if (!Restore && CurrenciesForScan == null)
                        return;
                    await ScanCurrencies(Restore ? null : CurrenciesForScan);
                    
                    OnAmountUpdatedEventHandler(this, EventArgs.Empty);
                });

            this.WhenAnyValue(vm => vm.NavigationService)
                .WhereNotNull()
                .SubscribeInMainThread(service => GetCurrencies());

            this.WhenAnyValue(vm => vm.SelectedCurrency)
                .WhereNotNull()
                .SubscribeInMainThread(async c =>
                {
                    switch (SelectCurrencyUseCase)
                    {
                        case CurrencyActionType.Show:
                            NavigationService?.ShowPage(new CurrencyPage(c), TabNavigation.Portfolio);
                            NavigationService?.SetInitiatedPage(TabNavigation.Portfolio);
                            
                            c.IsOpenCurrency = true;
                            await Task.Run(async () =>
                            {
                                await c.LoadTransactionsAsync();
                                c.LoadAddresses();
                            });
                            SelectedCurrency = null;
                            break;
                        
                        case CurrencyActionType.Send:
                            NavigationService?.ClosePopup();
                            var sendViewModel = SendViewModelCreator.CreateViewModel(_app, c, NavigationService);
                            if (c.Currency is BitcoinBasedConfig)
                            {
                                var selectOutputsViewModel =
                                    sendViewModel.SelectFromViewModel as SelectOutputsViewModel;
                                NavigationService?.ShowPage(new SelectOutputsPage(selectOutputsViewModel),
                                    TabNavigation.Portfolio);
                            }
                            else
                            {
                                var selectAddressViewModel =
                                    sendViewModel.SelectFromViewModel as SelectAddressViewModel;
                                NavigationService?.ShowPage(new SelectAddressPage(selectAddressViewModel),
                                    TabNavigation.Portfolio);
                            }

                            SelectCurrencyUseCase = CurrencyActionType.Show;
                            SelectedCurrency = null;
                            break;
                        
                        case CurrencyActionType.Receive:
                            var receiveViewModel = new ReceiveViewModel(_app, c?.Currency, NavigationService);
                            NavigationService?.ShowPopup(new ReceiveBottomSheet(receiveViewModel));
                            SelectCurrencyUseCase = CurrencyActionType.Show;
                            SelectedCurrency = null;
                            break;
                        
                        default:
                            NavigationService?.ShowPage(new CurrencyPage(c), TabNavigation.Portfolio);
                            NavigationService?.SetInitiatedPage(TabNavigation.Portfolio);
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

        private async Task ScanCurrencies(string[] currenciesForScan = null)
        {
            IsRestoring = true;

            var currencies = currenciesForScan == null
                ? AllCurrencies
                    .Select(c => c.CurrencyViewModel)
                : AllCurrencies
                    .Where(curr => currenciesForScan.Contains(curr.CurrencyViewModel.CurrencyCode))
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
                    .Where(c => c.HasTokens && c.CurrencyCode == TezosConfig.Xtz)
                    .Cast<TezosCurrencyViewModel>()
                    .FirstOrDefault()?
                    .TezosTokensViewModel;

                if (tezosTokens != null)
                    await tezosTokens.UpdateTokens();

                await Task.Run(() =>
                    Task.WhenAll(tokenCurrencies
                        .Where(c => c.Currency is not TezosConfig)
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
            try
            {
                Device.InvokeOnMainThreadAsync(() =>
                {
                    AvailableAmountInBase = AllCurrencies.Sum(c => c.CurrencyViewModel.TotalAmountInBase);
                    UnconfirmedAmountInBase = AllCurrencies.Sum(c => c.CurrencyViewModel.UnconfirmedAmountInBase);
                });
            }
            catch (Exception e)
            {
                Log.Error(e, "Portfolio amount error");
            }
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
                                navigationService: NavigationService,
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
            NavigationService = service ?? throw new ArgumentNullException(nameof(service));
        }

        private void ChangeUserCurrencies(string currency, bool isSelected)
        {
            try
            {
                if (string.IsNullOrEmpty(currency)) return;

                var disabledCurrencies = AllCurrencies
                    .Where(c => !c.IsSelected)
                    .Select(cur => cur.CurrencyViewModel.CurrencyCode)
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

        public ReactiveCommand<Unit, Unit> ManageAssetsCommand => _manageAssetsCommand ??=
            ReactiveCommand.Create(() => NavigationService?.ShowPopup(new ManageAssetsBottomSheet(this)));

        private ReactiveCommand<Unit, Unit> _showAvailableAmountCommand;

        public ReactiveCommand<Unit, Unit> ShowAvailableAmountCommand => _showAvailableAmountCommand ??=
            ReactiveCommand.Create(() =>
                NavigationService?.ShowPopup(new AvailableAmountPopup(this)));

        private ICommand _closeBottomSheetCommand;

        public ICommand CloseBottomSheetCommand => _closeBottomSheetCommand ??=
            ReactiveCommand.Create(() => NavigationService?.ClosePopup());

        private ReactiveCommand<Unit, Unit> _sendCommand;

        public ReactiveCommand<Unit, Unit> SendCommand => _sendCommand ??= ReactiveCommand.Create(() =>
        {
            NavigationService?.SetInitiatedPage(TabNavigation.Portfolio);
            SelectCurrencyUseCase = CurrencyActionType.Send;
            var currencies = AllCurrencies.Select(c => c.CurrencyViewModel);
            var selectCurrencyViewModel =
                new SelectCurrencyViewModel(
                    CurrencyActionType.Send,
                    currencies,
                    NavigationService)
                {
                    OnSelected = currencyViewModel =>
                    {
                        SelectedCurrency = currencyViewModel;
                        SelectCurrencyUseCase = CurrencyActionType.Send;
                    }
                };

            SelectCurrencyUseCase = CurrencyActionType.Show;
            NavigationService?.ShowPopup(new SelectCurrencyBottomSheet(selectCurrencyViewModel));
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
                    NavigationService)
                {
                    OnSelected = currencyViewModel =>
                    {
                        SelectedCurrency = currencyViewModel;
                        SelectCurrencyUseCase = CurrencyActionType.Receive;
                    }
                };

            SelectCurrencyUseCase = CurrencyActionType.Show;
            NavigationService?.ShowPopup(new SelectCurrencyBottomSheet(selectCurrencyViewModel));
        });

        private ReactiveCommand<Unit, Unit> _exchangeCommand;

        public ReactiveCommand<Unit, Unit> ExchangeCommand => _exchangeCommand ??=
            ReactiveCommand.Create(() => NavigationService?.GoToExchange(null));

        private ReactiveCommand<Unit, Unit> _hideRestoringCommand;

        public ReactiveCommand<Unit, Unit> HideRestoringCommand =>
            _hideRestoringCommand ??= ReactiveCommand.Create(() => { IsRestoring = false; });
    }
}