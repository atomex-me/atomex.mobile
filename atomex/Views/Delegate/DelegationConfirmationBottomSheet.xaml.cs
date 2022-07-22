using atomex.ViewModels;
using Rg.Plugins.Popup.Pages;

namespace atomex.Views.Delegate
{
    public partial class DelegationConfirmationBottomSheet : PopupPage
    {
        public DelegationConfirmationBottomSheet(DelegateViewModel delegateViewModel)
        {
            InitializeComponent();
            BindingContext = delegateViewModel;
        }

        public void OnClose()
        {
            if (BindingContext is DelegateViewModel)
            {
                var delegateViewModel = (DelegateViewModel)BindingContext;
                if (delegateViewModel.UndoConfirmStageCommand.CanExecute(null))
                    delegateViewModel.UndoConfirmStageCommand.Execute(null);
            }
        }
    }
}
