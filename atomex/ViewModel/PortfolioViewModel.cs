using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using atomex.ViewModel.CurrencyViewModels;
using atomex.Views;
using Atomex;
using Atomex.Abstract;
using Atomex.Common;
using ReactiveUI.Fody.Helpers;
using Rg.Plugins.Popup.Services;
using Xamarin.Forms;

namespace atomex.ViewModel
{
    public class PortfolioViewModel : BaseViewModel
    {
        private IAtomexApp _app { get; }
        public INavigationService NavigationService { get; set; }

        private ICurrencies Currencies
        {
            get
            {
                return _app.Account.Currencies;
            }
        }

        [Reactive] public decimal PortfolioValue { get; set; }
        public List<CurrencyViewModel> CurrencyViewModels { get; set; }

        public PortfolioViewModel(IAtomexApp app)
        {
            _app = app ?? throw new ArgumentNullException(nameof(AtomexApp));
            CurrencyViewModels = new List<CurrencyViewModel>();
            _ = FillCurrenciesAsync();
        }

        private void SubscribeToUpdates()
        {
            CurrencyViewModels.ForEach(c =>
            {
                c.AmountUpdated += OnAmountUpdatedEventHandler;
            });

            OnAmountUpdatedEventHandler(this, EventArgs.Empty);
        }

        private void OnAmountUpdatedEventHandler(object sender, EventArgs args)
        {
            PortfolioValue = CurrencyViewModels.Sum(c => c.TotalAmountInBase);

            CurrencyViewModels.ForEachDo(c => c.PortfolioPercent = PortfolioValue != 0
                ? c.TotalAmountInBase / PortfolioValue
                : 0);
        }

        private async Task FillCurrenciesAsync()
        {
            await Task.WhenAll(Currencies.Select(c =>
            {
                CurrencyViewModels.Add(CurrencyViewModelCreator.CreateViewModel(_app, c));
                return Task.CompletedTask;
            }));

            SubscribeToUpdates();
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
