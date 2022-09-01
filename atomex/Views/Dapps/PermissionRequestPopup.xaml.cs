using atomex.ViewModels.DappsViewModels;
using Rg.Plugins.Popup.Pages;
using Xamarin.Forms.Xaml;

namespace atomex.Views.Dapps
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class PermissionRequestPopup : PopupPage
    {
        public PermissionRequestPopup(PermissionRequestViewModel permissionRequestViewModel)
        {
            InitializeComponent();
            BindingContext = permissionRequestViewModel;
        }
    }
}