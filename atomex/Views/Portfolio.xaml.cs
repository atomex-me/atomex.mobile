using Xamarin.Forms;
using atomex.ViewModels;

namespace atomex.Views
{
    public partial class Portfolio : ContentPage
    {
        public Portfolio(PortfolioViewModel portfolioViewModel)
        {
            InitializeComponent();
            BindingContext = portfolioViewModel;
        }
    }
}