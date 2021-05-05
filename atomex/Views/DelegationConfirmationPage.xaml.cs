using atomex.ViewModel;
using Xamarin.Forms;

namespace atomex
{
    public partial class DelegationConfirmationPage : ContentPage
    {
        public DelegationConfirmationPage(DelegateViewModel delegateViewModel)
        {
            InitializeComponent();
            BindingContext = delegateViewModel;
        }
    }
}
