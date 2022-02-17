using atomex.ViewModel.SendViewModels;
using Xamarin.Forms;

namespace atomex.Views.Send
{
    public partial class FromOutputsPage : ContentPage
    {
        public FromOutputsPage(SelectOutputsViewModel selectOutputsViewModel)
        {
            InitializeComponent();
            BindingContext = selectOutputsViewModel;
        }
    }
}
