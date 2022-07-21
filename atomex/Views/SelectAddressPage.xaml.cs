using atomex.ViewModels;
using Xamarin.Forms;

namespace atomex.Views
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
