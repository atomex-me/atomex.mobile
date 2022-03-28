using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows.Input;
using atomex.Common;
using atomex.ViewModel.CurrencyViewModels;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Rg.Plugins.Popup.Services;
using Xamarin.Forms;

namespace atomex.ViewModel
{
    public class SelectCurrencyViewModel : BaseViewModel
    {
        public CurrencyActionType Type { get; set; }
        public ObservableCollection<CurrencyViewModel> Currencies { get; set; }
        [Reactive] public CurrencyViewModel SelectedCurrency { get; set; }
        public Action<CurrencyViewModel> OnSelected { get; set; }

        public SelectCurrencyViewModel(CurrencyActionType type, IEnumerable<CurrencyViewModel> currencies)
        {
            Type = type;
            Currencies = new ObservableCollection<CurrencyViewModel>(currencies);

            this.WhenAnyValue(vm => vm.SelectedCurrency)
                .WhereNotNull()
                .SubscribeInMainThread(_ =>
                {
                    OnSelected?.Invoke(SelectedCurrency);
                });
        }

        private ICommand _closeBottomSheetCommand;
        public ICommand CloseBottomSheetCommand => _closeBottomSheetCommand ??= new Command(() =>
        {
            if (PopupNavigation.Instance.PopupStack.Count > 0)
                _ = PopupNavigation.Instance.PopAsync();
        });
    }
}