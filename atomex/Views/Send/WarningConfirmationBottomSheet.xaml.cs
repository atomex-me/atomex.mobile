using atomex.ViewModels.SendViewModels;
using Rg.Plugins.Popup.Pages;

namespace atomex.Views.Send
{
    public partial class WarningConfirmationBottomSheet : PopupPage
    {
        public WarningConfirmationBottomSheet(SendViewModel sendViewModel)
        {
            InitializeComponent();
            BindingContext = sendViewModel;
        }

        protected override void OnDisappearing()
        {
            if (BindingContext is SendViewModel)
            {
                var vm = (SendViewModel)BindingContext;
                if (vm.UndoConfirmStageCommand.CanExecute(null))
                    vm.UndoConfirmStageCommand.Execute(null);
            }
        }

        public void OnClose()
        {
            if (BindingContext is SendViewModel)
            {
                var sendViewModel = (SendViewModel)BindingContext;
                if (sendViewModel.CloseConfirmationCommand.CanExecute(null))
                    sendViewModel.CloseConfirmationCommand.Execute(null);
            }
        }
    }
}
