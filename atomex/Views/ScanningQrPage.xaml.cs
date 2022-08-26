using atomex.ViewModels;
using atomex.ViewModels.ConversionViewModels;
using atomex.ViewModels.DappsViewModels;
using atomex.ViewModels.SendViewModels;
using Xamarin.Forms;

namespace atomex.Views
{
    public partial class ScanningQrPage : ContentPage
    {
        public ScanningQrPage(SelectAddressViewModel selectAddressViewModel)
        {
            InitializeComponent();
            BindingContext = selectAddressViewModel;
        }

        public ScanningQrPage(SelectOutputsViewModel selectAddressViewModel)
        {
            InitializeComponent();
            BindingContext = selectAddressViewModel;
        }

        public ScanningQrPage(TezosTokensSendViewModel tezosTokensSendViewModel)
        {
            InitializeComponent();
            BindingContext = tezosTokensSendViewModel;
        }

        public ScanningQrPage(ConversionViewModel conversionViewModel)
        {
            InitializeComponent();
            BindingContext = conversionViewModel;
        }
        
        public ScanningQrPage(ConnectDappViewModel connectDappViewModel)
        {
            InitializeComponent();
            BindingContext = connectDappViewModel;
        }
    }
}
