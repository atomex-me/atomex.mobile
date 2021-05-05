using Xamarin.Forms;
using atomex.ViewModel.SendViewModels;

namespace atomex
{
    public partial class SendingConfirmationPage : ContentPage
    {
        public SendingConfirmationPage(SendViewModel sendViewModel)
        {
            InitializeComponent();
            BindingContext = sendViewModel;
        }
    }
}
