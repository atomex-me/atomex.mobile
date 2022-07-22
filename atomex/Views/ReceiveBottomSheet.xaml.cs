using atomex.ViewModels;
using Rg.Plugins.Popup.Pages;

namespace atomex.Views
{
    public partial class ReceiveBottomSheet : PopupPage
    {
        public ReceiveBottomSheet(ReceiveViewModel receiveViewModel)
        {
            InitializeComponent();
            BindingContext = receiveViewModel;
        }

        public void OnClose()
        {
            if (BindingContext is ReceiveViewModel)
            {
                var vm = (ReceiveViewModel)BindingContext;
                if (vm.CloseBottomSheetCommand.CanExecute(null))
                    vm.CloseBottomSheetCommand.Execute(null);
            }
        }
    }
}
