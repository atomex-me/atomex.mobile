using atomex.ViewModels;
using Rg.Plugins.Popup.Pages;

namespace atomex.Views
{
    public partial class DelegationActionBottomSheet : PopupPage
    {
        public DelegationActionBottomSheet(DelegationViewModel delegationViewModel)
        {
            InitializeComponent();
            BindingContext = delegationViewModel;
        }

        public void OnClose()
        {
            if (BindingContext is DelegationViewModel)
            {
                var vm = (DelegationViewModel)BindingContext;
                if (vm.CloseBottomSheetCommand.CanExecute(null))
                    vm.CloseBottomSheetCommand.Execute(null);
            }
        }
    }
}
