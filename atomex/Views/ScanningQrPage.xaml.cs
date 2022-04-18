using atomex.ViewModel.SendViewModels;
using atomex.ViewModel.WalletBeacon;
using atomex.ViewModel;
using atomex.ViewModel.SendViewModels;
using Xamarin.Forms;

namespace atomex
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
        
        public ScanningQrPage(DappsViewModel dappsViewModel)
        {
            InitializeComponent();
            BindingContext = dappsViewModel;
        }
    }
}
