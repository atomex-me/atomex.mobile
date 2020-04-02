using System.ComponentModel;
using Xamarin.Forms;
using atomex.ViewModel;
using atomex.CustomElements;
using Atomex;
using System;
using atomex.Views.CreateSwap;

namespace atomex
{
    // Learn more about making custom code visible in the Xamarin.Forms previewer
    // by visiting https://aka.ms/xamarinforms-previewer
    [DesignTimeVisible(false)]
    public partial class MainPage : CustomTabbedPage, INavigationService
    {

        private readonly NavigationPage navigationConversionPage;

        private MainViewModel _mainViewModel;

        public MainPage()
        {

            _mainViewModel = new MainViewModel();
            //IAtomexApp AtomexApp = _mainViewModel.GetAtomexApp();
            var navigationWalletsListPage = new NavigationPage(new CurrenciesListPage(_mainViewModel.CurrenciesViewModel, this));
            navigationWalletsListPage.IconImageSource = "NavBar__wallets";
            navigationWalletsListPage.Title = "Wallets";
            navigationWalletsListPage.BarBackgroundColor = Color.FromHex("#223a66");
            navigationWalletsListPage.BarTextColor = Color.White;

            var navigationPortfolio = new NavigationPage(new Portfolio(_mainViewModel.CurrenciesViewModel));
            navigationPortfolio.IconImageSource = "NavBar__portfolio";
            navigationPortfolio.Title = "Portfolio";
            navigationPortfolio.BarBackgroundColor = Color.FromHex("#223a66");
            navigationPortfolio.BarTextColor = Color.White;

            //navigationConversionPage = new NavigationPage(new ConversionPage(AtomexApp, _mainViewModel.ConversionViewModel));
            navigationConversionPage = new NavigationPage(new ConversionsListPage(_mainViewModel.ConversionViewModel));
            navigationConversionPage.IconImageSource = "NavBar__conversion";
            navigationConversionPage.Title = "Conversion";
            navigationConversionPage.BarBackgroundColor = Color.FromHex("#223a66");
            navigationConversionPage.BarTextColor = Color.White;

            var navigationSettingsPage = new NavigationPage(new SettingsPage(_mainViewModel.SettingsViewModel));
            navigationSettingsPage.IconImageSource = "NavBar__settings";
            navigationSettingsPage.Title = "Settings";
            navigationSettingsPage.BarBackgroundColor = Color.FromHex("#223a66");
            navigationSettingsPage.BarTextColor = Color.White;

            Children.Add(navigationPortfolio);
            Children.Add(navigationWalletsListPage);
            Children.Add(navigationConversionPage);
            Children.Add(navigationSettingsPage);

        }

        public void ShowConversionPage(CurrencyViewModel currency = null)
        {
            this.CurrentPage = navigationConversionPage;

            var conversionViewModel = navigationConversionPage.RootPage.BindingContext as ConversionViewModel;
            Console.WriteLine(conversionViewModel);
            if (conversionViewModel != null)
            {
                navigationConversionPage.PushAsync(new CurrenciesPage(conversionViewModel));

                Console.WriteLine(currency);
                Console.WriteLine(conversionViewModel);
                Console.WriteLine(conversionViewModel.FromCurrency);
                conversionViewModel.FromCurrency = currency;
            }
        }
    }
}
