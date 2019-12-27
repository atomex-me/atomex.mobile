using System.Collections.Generic;
using System.ComponentModel;
using Xamarin.Forms;
using atomex.Models;
using atomex.CustomElements;
using atomex.ViewModel;

namespace atomex
{
    // Learn more about making custom code visible in the Xamarin.Forms previewer
    // by visiting https://aka.ms/xamarinforms-previewer
    [DesignTimeVisible(false)]
    public partial class MainPage : CustomTabbedPage, INavigationService
    {

        private readonly NavigationPage navigationConversionPage;

        private MainViewModel MainViewModel;

        public MainPage()
        {

            MainViewModel = new MainViewModel();
            var navigationWalletsListPage = new NavigationPage(new WalletsListPage(MainViewModel.WalletsViewModel, MainViewModel.TransactionsViewModel, this));
            navigationWalletsListPage.IconImageSource = "NavBar__wallets";
            navigationWalletsListPage.Title = "Wallets";

            var navigationPortfolio = new NavigationPage(new Portfolio(MainViewModel.WalletsViewModel));
            navigationPortfolio.IconImageSource = "NavBar__portfolio";
            navigationPortfolio.Title = "Portfolio";

            navigationConversionPage = new NavigationPage(new ConversionPage(MainViewModel.ConversionViewModel));
            navigationConversionPage.IconImageSource = "NavBar__conversion";
            navigationConversionPage.Title = "Conversion";

            var navigationSettingsPage = new NavigationPage(new SettingsPage(MainViewModel.SettingsViewModel));
            navigationSettingsPage.IconImageSource = "NavBar__settings";
            navigationSettingsPage.Title = "Settings";

            Children.Add(navigationPortfolio);
            Children.Add(navigationWalletsListPage);
            Children.Add(navigationConversionPage);
            Children.Add(navigationSettingsPage);

        }

        public void ShowConversionPage(Wallet wallet = null)
        {
            this.CurrentPage = navigationConversionPage;

            var conversionViewModel = navigationConversionPage.RootPage.BindingContext as ConversionViewModel;

            if (conversionViewModel != null)
            {
                conversionViewModel.FromCurrency = wallet;
                conversionViewModel.ToCurrency = null;
            }
        }
    }
}
