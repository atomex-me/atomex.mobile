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
    }
}
