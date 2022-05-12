using atomex.ViewModel;
using atomex.ViewModel.CurrencyViewModels;
using Rg.Plugins.Popup.Pages;

namespace atomex.Views.Popup
{
    public partial class AvailableAmountPopup : PopupPage
    {
        public AvailableAmountPopup(PortfolioViewModel portfolioViewModel)
        {
            InitializeComponent();
            BindingContext = portfolioViewModel;
        }

        public AvailableAmountPopup(CurrencyViewModel currencyViewModel)
        {
            InitializeComponent();
            BindingContext = currencyViewModel;
        }
    }
}
