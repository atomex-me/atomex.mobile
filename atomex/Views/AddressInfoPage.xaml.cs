using atomex.ViewModels;
using Xamarin.Forms;

namespace atomex.Views
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
