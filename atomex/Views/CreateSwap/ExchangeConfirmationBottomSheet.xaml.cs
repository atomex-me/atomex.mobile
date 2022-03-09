using atomex.ViewModel;
using Rg.Plugins.Popup.Pages;

namespace atomex.Views.CreateSwap
{
    public partial class ExchangeConfirmationBottomSheet : PopupPage
    {
        public ExchangeConfirmationBottomSheet(ConversionConfirmationViewModel conversionConfirmationViewModel)
        {
            InitializeComponent();
            BindingContext = conversionConfirmationViewModel;
        }
    }
}
