using atomex.ViewModel;
using Xamarin.Forms;

namespace atomex.Views.Delegate
{
    public partial class DelegatePage : ContentPage
    {
        public DelegatePage(DelegateViewModel delegateViewModel)
        {
            InitializeComponent();
            BindingContext = delegateViewModel;
        }
    }
}
