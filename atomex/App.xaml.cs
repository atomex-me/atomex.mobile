using Xamarin.Forms;

namespace atomex
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();
            MainPage = new NavigationPage(new StartPage());
            ((NavigationPage)MainPage).BarBackgroundColor = Color.FromHex("#F5F5F5");
            //((NavigationPage)MainPage).BarBackgroundColor = Color.FromRgba(95, 158, 160, 127);
            //MainPage = new MainPage();
        }

        protected override void OnStart()
        {
            // Handle when your app starts
        }

        protected override void OnSleep()
        {
            // Handle when your app sleeps
        }

        protected override void OnResume()
        {
            // Handle when your app resumes
        }
    }
}
