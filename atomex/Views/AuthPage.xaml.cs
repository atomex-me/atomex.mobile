using Xamarin.Forms;

namespace atomex.Views
{
    public partial class AuthPage : ContentPage
    {
        public AuthPage()
        {
            InitializeComponent();
        }

        public AuthPage(UnlockViewModel unlockViewModel)
        {
            InitializeComponent();
            BindingContext = unlockViewModel;
        }
    }
}
