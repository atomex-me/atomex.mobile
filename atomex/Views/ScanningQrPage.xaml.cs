using atomex.ViewModel;
using atomex.ViewModel.SendViewModels;
using Xamarin.Forms;

namespace atomex
{
    public partial class ScanningQrPage : ContentPage
    {
        public ScanningQrPage(SendViewModel sendViewModel)
        {
            InitializeComponent();
            BindingContext = sendViewModel;
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
    }
}
