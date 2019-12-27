using System;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace atomex
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();

            //MainPage = new MainPage();
            //MainPage = new NavigationPage(new MainPage());
            //MainPage = new NavigationPage(new TabPage());
            //MainPage = new NavigationPage(new Portfolio());
            MainPage = new MainPage();
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
