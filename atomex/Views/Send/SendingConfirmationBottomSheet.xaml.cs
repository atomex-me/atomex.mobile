using atomex.ViewModel.SendViewModels;
using Rg.Plugins.Popup.Pages;

namespace atomex.Views.Send
{
    public partial class SendingConfirmationBottomSheet : PopupPage
    {
        public SendingConfirmationBottomSheet(SendViewModel sendViewModel)
        {
            InitializeComponent();
            BindingContext = sendViewModel;
        }

        public SendingConfirmationBottomSheet(TezosTokensSendViewModel sendViewModel)
        {
            InitializeComponent();
            BindingContext = sendViewModel;
        }

        protected override void OnDisappearing()
        {
            var vm = (SendViewModel)BindingContext;
            if (vm.CloseConfirmationCommand.CanExecute(null))
                vm.CloseConfirmationCommand.Execute(null);

            base.OnDisappearing();
        }
    }
}
