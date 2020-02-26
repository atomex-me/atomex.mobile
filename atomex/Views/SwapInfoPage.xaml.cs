using Xamarin.Forms;

namespace atomex
{
    public partial class SwapInfoPage : ContentPage
    {
        public SwapInfoPage()
        {
            InitializeComponent();
        }
        public SwapInfoPage(SwapViewModel swapViewModel)
        {
            InitializeComponent();
            {
                BindingContext = swapViewModel;
            }
        }
    }
}
