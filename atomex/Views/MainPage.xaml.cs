using System.ComponentModel;
using Xamarin.Forms;
using atomex.ViewModel;
using atomex.CustomElements;
using atomex.Views.CreateSwap;

namespace atomex
{
    // Learn more about making custom code visible in the Xamarin.Forms previewer
    // by visiting https://aka.ms/xamarinforms-previewer
    [DesignTimeVisible(false)]
    public partial class MainPage : CustomTabbedPage, INavigationService
    {

        private readonly NavigationPage navigationConversionPage;

        public MainViewModel _mainViewModel { get; }

        public MainPage(MainViewModel mainViewModel)
        {

            _mainViewModel = mainViewModel;
            var navigationWalletsListPage = new NavigationPage(new CurrenciesListPage(_mainViewModel.CurrenciesViewModel, _mainViewModel.DelegateViewModel, this));
            navigationWalletsListPage.IconImageSource = "NavBar-wallets";
            navigationWalletsListPage.Title = "Wallets";
            navigationWalletsListPage.BarBackgroundColor = Color.FromHex("#2B5286");
            navigationWalletsListPage.BarTextColor = Color.White;

            var navigationPortfolio = new NavigationPage(new Portfolio(_mainViewModel.CurrenciesViewModel));
            navigationPortfolio.IconImageSource = "NavBar-portfolio";
            navigationPortfolio.Title = "Portfolio";
            navigationPortfolio.BarBackgroundColor = Color.FromHex("#2B5286");
            navigationPortfolio.BarTextColor = Color.White;

            navigationConversionPage = new NavigationPage(new ConversionsListPage(_mainViewModel.ConversionViewModel));
            navigationConversionPage.IconImageSource = "NavBar-conversion";
            navigationConversionPage.Title = "Conversion";
            navigationConversionPage.BarBackgroundColor = Color.FromHex("#2B5286");
            navigationConversionPage.BarTextColor = Color.White;

            var navigationSettingsPage = new NavigationPage(new SettingsPage(_mainViewModel.SettingsViewModel, this));
            navigationSettingsPage.IconImageSource = "NavBar-settings";
            navigationSettingsPage.Title = "Settings";
            navigationSettingsPage.BarBackgroundColor = Color.FromHex("#2B5286");
            navigationSettingsPage.BarTextColor = Color.White;

            Children.Add(navigationPortfolio);
            Children.Add(navigationWalletsListPage);
            Children.Add(navigationConversionPage);
            Children.Add(navigationSettingsPage);

            mainViewModel.Locked += (s, a) =>
            {
                SignOut();
            };
        }

        private void SignOut()
        {
            _mainViewModel.SignOut();
            StartViewModel startViewModel = new StartViewModel(_mainViewModel.AtomexApp);
            Application.Current.MainPage = new NavigationPage(new StartPage(startViewModel));
            ((NavigationPage)Application.Current.MainPage).BarBackgroundColor = Color.FromHex("#2B5286");
            ((NavigationPage)Application.Current.MainPage).BarTextColor = Color.White;
        }

        public void ConvertCurrency(string currencyCode)
        {
            this.CurrentPage = navigationConversionPage;
            var conversionViewModel = navigationConversionPage.RootPage.BindingContext as ConversionViewModel;
            if (conversionViewModel != null)
            {
                navigationConversionPage.PushAsync(new CurrenciesPage(conversionViewModel));
                conversionViewModel.SetFromCurrency(currencyCode);
            }
        }
    }
}
