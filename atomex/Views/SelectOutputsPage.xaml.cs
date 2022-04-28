using atomex.ViewModel;
using Xamarin.Forms;

namespace atomex.Views
{
    public partial class SelectOutputsPage : ContentPage
    {
        public SelectOutputsPage(SelectOutputsViewModel selectOutputsViewModel)
        {
            InitializeComponent();
            BindingContext = selectOutputsViewModel;
        }
    }
}
