using atomex.ViewModel.SendViewModels;
using Xamarin.Forms;

namespace atomex.Views.Send
{
    public partial class FromAddressPage : ContentPage
    {
        public FromAddressPage(SelectAddressViewModel selectAddressViewModel)
        {
            InitializeComponent();
            BindingContext = selectAddressViewModel;
        }
    }
}
