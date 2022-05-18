using atomex.ViewModel;
using Xamarin.Forms;

namespace atomex.Views
{
    public partial class SearchBakerPage : ContentPage
    {
        public SearchBakerPage(DelegateViewModel delegateViewModel)
        {
            InitializeComponent();
            BindingContext = delegateViewModel;
        }
    }
}
