using Xamarin.Forms;

namespace atomex
{
    public partial class MyWalletsPage : ContentPage
    {

        public MyWalletsPage()
        {
            InitializeComponent();
        }

        public MyWalletsPage(MyWalletsViewModel myWalletsViewModel)
        {
            InitializeComponent();
            BindingContext = myWalletsViewModel;
        }
    }
}
