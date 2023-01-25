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

        protected override void OnAppearing()
        {
            if (BindingContext is not ConnectDappViewModel vm) return;
            vm.Init();
        }

        protected override void OnDisappearing()
        {
            if (BindingContext is not ConnectDappViewModel vm) return;
            vm.Reset();
        }
    }
}