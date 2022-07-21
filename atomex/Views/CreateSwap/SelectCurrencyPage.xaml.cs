using atomex.ViewModels.ConversionViewModels;
using Xamarin.Forms;

namespace atomex.Views.CreateSwap
{
    public partial class SelectCurrencyPage : ContentPage
    {
        public SelectCurrencyPage(SelectCurrencyViewModel selectCurrencyViewModel)
        {
            InitializeComponent();
            BindingContext = selectCurrencyViewModel;
        }
    }
}
