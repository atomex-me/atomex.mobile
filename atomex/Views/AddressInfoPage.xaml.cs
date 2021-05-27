using atomex.ViewModel;
using Xamarin.Forms;

namespace atomex
{
    public partial class AddressInfoPage : ContentPage
    {
        public AddressInfoPage(AddressesViewModel addressesViewModel)
        {
            InitializeComponent();
            BindingContext = addressesViewModel;
        }
    }
}
