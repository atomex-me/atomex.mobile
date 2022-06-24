using atomex.ViewModel;
using Xamarin.Forms;

namespace atomex
{
    public partial class AddressInfoPage : ContentPage
    {
        public AddressInfoPage(AddressViewModel addressInfoViewModel)
        {
            InitializeComponent();
            BindingContext = addressInfoViewModel;
        }
    }
}
