using atomex.ViewModel;
using Xamarin.Forms;

namespace atomex
{
    public partial class DelegationInfoPage : ContentPage
    {
        public DelegationInfoPage(DelegationViewModel delegationViewModel)
        {
            InitializeComponent();
            BindingContext = delegationViewModel;
        }
    }
}
