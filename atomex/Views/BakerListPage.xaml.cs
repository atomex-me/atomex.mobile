using atomex.ViewModel;
using Xamarin.Forms;

namespace atomex
{
    public partial class BakerListPage : ContentPage
    {
        public BakerListPage()
        {
            InitializeComponent();
        }

        public BakerListPage(DelegateViewModel delegateViewModel)
        {
            InitializeComponent();
            BindingContext = delegateViewModel;
        }
    }
}
