using atomex.ViewModel;
using Xamarin.Forms;

namespace atomex.Views.CreateSwap
{
    public partial class ConfirmationPage : ContentPage
    {

        public ConfirmationPage()
        {
            InitializeComponent();
        }

        public ConfirmationPage(ConversionViewModel conversionViewModel)
        {
            InitializeComponent();

            BindingContext = conversionViewModel;
        }
    }
}
