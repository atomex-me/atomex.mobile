using atomex.ViewModel.ConversionViewModels;
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

        private void ItemSelected(object sender, SelectedItemChangedEventArgs e)
        {
            if (e.SelectedItem != null)
                ((ListView)sender).SelectedItem = null;
        }
    }
}
