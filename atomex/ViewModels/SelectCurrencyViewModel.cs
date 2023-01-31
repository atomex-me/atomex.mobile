using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows.Input;
using atomex.Common;
using atomex.Models;
using atomex.ViewModels.CurrencyViewModels;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Xamarin.Forms;

namespace atomex.ViewModels
{
    public class SelectCurrencyViewModel : BaseViewModel
    {
        private INavigationService _navigationService;

        public CurrencyActionType Type { get; set; }
        public ObservableCollection<CurrencyViewModel> Currencies { get; set; }
        [Reactive] public CurrencyViewModel SelectedCurrency { get; set; }
        public Action<CurrencyViewModel> OnSelected { get; set; }

        public SelectCurrencyViewModel()
        {
        }

        public SelectCurrencyViewModel(
            CurrencyActionType type,
            IEnumerable<CurrencyViewModel> currencies,
            INavigationService navigationService)
        {
            _navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));

            Type = type;
            Currencies = new ObservableCollection<CurrencyViewModel>(currencies);

            this.WhenAnyValue(vm => vm.SelectedCurrency)
                .WhereNotNull()
                .SubscribeInMainThread(_ =>
                {
                    if (SelectedCurrency?.AvailableAmount <= 0 && Type == CurrencyActionType.Send)
                        return;

                    OnSelected?.Invoke(SelectedCurrency);
                });
        }

        private ICommand _closeBottomSheetCommand;

        public ICommand CloseBottomSheetCommand =>
            _closeBottomSheetCommand ??= new Command(() => _navigationService?.ClosePopup());
    }
}