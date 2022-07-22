using atomex.ViewModels;
using Rg.Plugins.Popup.Pages;

namespace atomex.Views
{
    public partial class ExportKeyBottomSheet : PopupPage
    {
        public ExportKeyBottomSheet(AddressViewModel addressesViewModel)
        {
            InitializeComponent();
            BindingContext = addressesViewModel;
        }

        public void OnClose()
        {
            if (BindingContext is AddressViewModel)
            {
                var vm = (AddressViewModel)BindingContext;
                if (vm.CloseBottomSheetCommand.CanExecute(null))
                    vm.CloseBottomSheetCommand.Execute(null);
            }
        }
    }
}
