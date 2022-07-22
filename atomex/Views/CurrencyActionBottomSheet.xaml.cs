using atomex.ViewModels.CurrencyViewModels;
using Rg.Plugins.Popup.Pages;

namespace atomex.Views
{
    public partial class CurrencyActionBottomSheet : PopupPage
    {
        public CurrencyActionBottomSheet(CurrencyViewModel currencyViewModel)
        {
            InitializeComponent();
            BindingContext = currencyViewModel;
        }

        public void OnClose()
        {
            if (BindingContext is CurrencyViewModel)
            {
                var currencyViewModel = (CurrencyViewModel) BindingContext;
                if (currencyViewModel.CloseBottomSheetCommand.CanExecute(null))
                    currencyViewModel.CloseBottomSheetCommand.Execute(null);
            }
        }
    }
}