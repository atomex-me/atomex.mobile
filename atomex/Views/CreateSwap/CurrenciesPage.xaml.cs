using atomex.ViewModel;
using Xamarin.Forms;

namespace atomex.Views.CreateSwap
{
    public partial class CurrenciesPage : ContentPage
    {
        public CurrenciesPage()
        {
            InitializeComponent();
        }

        public CurrenciesPage(ConversionViewModel conversionViewModel)
        {
            InitializeComponent();
            BindingContext = conversionViewModel;
        }
    }
}
