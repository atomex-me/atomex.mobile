using atomex.ViewModels;
using Rg.Plugins.Popup.Pages;
using Xamarin.Forms.Xaml;

namespace atomex.Views.Dapps
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class SelectAddressBottomSheet : PopupPage
    {
        public SelectAddressBottomSheet(SelectAddressViewModel selectAddressViewModel)
        {
            BindingContext = selectAddressViewModel;
            InitializeComponent();
        }
        
        public void OnClose()
        {
            if (BindingContext is SelectAddressViewModel)
            {
                var vm = (SelectAddressViewModel)BindingContext;
                if (vm.CloseBottomSheetCommand.CanExecute(null))
                    vm.CloseBottomSheetCommand.Execute(null);
            }
        }
    }
}