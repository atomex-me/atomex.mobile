using atomex.ViewModels.DappsViewModels;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace atomex.Views.Dapps
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class ConnectDappPage : ContentPage
    {
        public ConnectDappPage(ConnectDappViewModel connectDappViewModel)
        {
            InitializeComponent();
            BindingContext = connectDappViewModel;
        }
    }
}