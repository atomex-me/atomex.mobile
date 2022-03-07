using atomex.ViewModel;
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
        private void ItemSelected(object sender, SelectedItemChangedEventArgs e)
        {
            if (e.SelectedItem != null)
                ((ListView)sender).SelectedItem = null;
        }
    }
}
