using atomex.ViewModel.TransactionViewModels;
using Rg.Plugins.Popup.Pages;

namespace atomex.Views
{
    public partial class RemoveTxBottomSheet : PopupPage
    {
        public RemoveTxBottomSheet(TransactionViewModel transactionViewModel)
        {
            InitializeComponent();
            BindingContext = transactionViewModel;
        }

        public void OnClose()
        {
            if (BindingContext is TransactionViewModel)
            {
                var vm = (TransactionViewModel)BindingContext;
                if (vm.CloseBottomSheetCommand.CanExecute(null))
                    vm.CloseBottomSheetCommand.Execute(null);
            }
        }
    }
}
