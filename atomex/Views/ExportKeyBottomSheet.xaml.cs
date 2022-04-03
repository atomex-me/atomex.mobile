using atomex.ViewModel;
using Rg.Plugins.Popup.Pages;

namespace atomex.Views
{
    public partial class ExportKeyBottomSheet : PopupPage
    {
        public ExportKeyBottomSheet(AddressesViewModel addressesViewModel)
        {
            InitializeComponent();
            BindingContext = addressesViewModel;
        }

        public void OnClose()
        {
            if (BindingContext is AddressesViewModel)
            {
                var vm = (AddressesViewModel)BindingContext;
                if (vm.CloseBottomSheetCommand.CanExecute(null))
                    vm.CloseBottomSheetCommand.Execute(null);
            }
        }
    }
}
