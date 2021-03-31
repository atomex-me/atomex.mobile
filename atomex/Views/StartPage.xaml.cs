using Xamarin.Forms;

namespace atomex
{
    public partial class StartPage : ContentPage
    {

        public StartPage(StartViewModel startViewModel)
        {
            InitializeComponent();
            NavigationPage.SetHasNavigationBar(this, false);
            BindingContext = startViewModel;
        }
    }
}
