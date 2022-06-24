using atomex.ViewModel;
using Rg.Plugins.Popup.Pages;

namespace atomex.Views
{
    public partial class SelectCurrencyBottomSheet : PopupPage
    {
        public SelectCurrencyBottomSheet(SelectCurrencyViewModel selectCurrencyViewModel)
        {
            InitializeComponent();
            BindingContext = selectCurrencyViewModel;
        }

        public void OnClose()
        {
            if (BindingContext is SelectCurrencyViewModel)
            {
                var vm = (SelectCurrencyViewModel)BindingContext;
                if (vm.CloseBottomSheetCommand.CanExecute(null))
                    vm.CloseBottomSheetCommand.Execute(null);
            }
        }
    }
}
