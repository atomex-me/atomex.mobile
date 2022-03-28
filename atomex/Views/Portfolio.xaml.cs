using Xamarin.Forms;
using atomex.ViewModel;

namespace atomex
{
    public partial class Portfolio : ContentPage
    {
        public Portfolio(PortfolioViewModel portfolioViewModel)
        {
            InitializeComponent();
            BindingContext = portfolioViewModel;
        }

        private void ItemSelected(object sender, SelectedItemChangedEventArgs e)
        {
            if (e.SelectedItem != null)
                ((ListView)sender).SelectedItem = null;
        }
    }
}