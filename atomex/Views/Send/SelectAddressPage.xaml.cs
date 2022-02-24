using atomex.ViewModel.SendViewModels;
using Xamarin.Forms;

namespace atomex.Views.Send
{
    public partial class SelectAddressPage : ContentPage
    {
        public SelectAddressPage(SelectAddressViewModel selectAddressViewModel)
        {
            BindingContext = selectAddressViewModel;
            InitializeComponent();
        }
    }
}
