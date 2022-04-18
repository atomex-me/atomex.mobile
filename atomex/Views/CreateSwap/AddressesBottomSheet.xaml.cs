using atomex.ViewModel.ConversionViewModels;
using Rg.Plugins.Popup.Pages;

namespace atomex.Views.CreateSwap
{
    public partial class AddressesBottomSheet : PopupPage
    {
        public AddressesBottomSheet(SelectCurrencyViewModel selectCurrencyViewModel)
        {
            InitializeComponent();
            BindingContext = selectCurrencyViewModel;
        }

        public void OnClose()
        {
            if (BindingContext is SelectCurrencyViewModel)
            {
                var sendViewModel = (SelectCurrencyViewModel)BindingContext;
                if (sendViewModel.CloseBottomSheetCommand.CanExecute(null))
                    sendViewModel.CloseBottomSheetCommand.Execute(null);
            }
        }
    }
}
