using atomex.ViewModels.ConversionViewModels;
using Xamarin.Forms;

namespace atomex.Views.CreateSwap
{
    public partial class ExchangePage : ContentPage
    {
        public ExchangePage(ConversionViewModel conversionViewModel)
        {
            InitializeComponent();
            BindingContext = conversionViewModel;
        }
    }
}
