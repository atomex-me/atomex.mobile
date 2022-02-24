using atomex.ViewModel;
using Rg.Plugins.Popup.Pages;

namespace atomex.Views.CreateSwap
{
    public partial class AddressesBottomSheet : PopupPage
    {
        public AddressesBottomSheet()
        {
            InitializeComponent();
        }

        public AddressesBottomSheet(ConversionViewModel conversionViewModel)
        {
            InitializeComponent();
            BindingContext = conversionViewModel;
        }
    }
}
