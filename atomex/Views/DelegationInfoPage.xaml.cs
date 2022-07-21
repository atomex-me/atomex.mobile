using atomex.ViewModels;
using Xamarin.Forms;

namespace atomex.Views
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