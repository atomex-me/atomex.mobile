using atomex.ViewModels.CurrencyViewModels;
using Xamarin.Forms;

namespace atomex.Views.Collectibles
{
    public partial class CollectiblePage : ContentPage
    {
        public CollectiblePage(CollectibleViewModel collectibleViewModel)
        {
            InitializeComponent();
            BindingContext = collectibleViewModel;
        }
    }
}