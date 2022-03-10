using atomex.ViewModel.SendViewModels;
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
            var vm = (SendViewModel)BindingContext;
            if (vm.UndoConfirmStageCommand.CanExecute(null))
                vm.UndoConfirmStageCommand.Execute(null);
        }
    }
}
