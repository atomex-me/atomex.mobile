using atomex.ViewModel;
using Xamarin.Forms;

namespace atomex
{
    public partial class BuyPage : ContentPage
    {
        public BuyPage()
        {
            InitializeComponent();
        }

        public BuyPage(BuyViewModel buyViewModel)
        {
            InitializeComponent();
            BindingContext = buyViewModel;
        }
    }
}
