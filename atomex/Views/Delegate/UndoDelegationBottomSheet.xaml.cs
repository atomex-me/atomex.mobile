using atomex.ViewModel;
using Rg.Plugins.Popup.Pages;

namespace atomex.Views.Delegate
{
    public partial class UndoDelegationBottomSheet : PopupPage
    {
        public UndoDelegationBottomSheet(DelegateViewModel delegateViewModel)
        {
            InitializeComponent();
            BindingContext = delegateViewModel;
        }

        public void OnClose()
        {
            if (BindingContext is DelegateViewModel)
            {
                var vm = (DelegateViewModel)BindingContext;
                if (vm.UndoConfirmStageCommand.CanExecute(null))
                    vm.UndoConfirmStageCommand.Execute(null);
            }
        }
    }
}
