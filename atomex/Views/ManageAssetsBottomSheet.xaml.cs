using atomex.ViewModels;
using Rg.Plugins.Popup.Pages;

namespace atomex.Views
{
    public partial class ManageAssetsBottomSheet : PopupPage
    {
        public ManageAssetsBottomSheet(PortfolioViewModel portfolioViewModel)
        {
            InitializeComponent();
            BindingContext = portfolioViewModel;
        }

        public void OnClose()
        {
            if (BindingContext is PortfolioViewModel)
            {
                var vm = (PortfolioViewModel)BindingContext;
                if (vm.CloseBottomSheetCommand.CanExecute(null))
                    vm.CloseBottomSheetCommand.Execute(null);
            }
        }
    }
}
