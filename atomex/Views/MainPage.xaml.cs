using System.ComponentModel;
using Xamarin.Forms;
using atomex.ViewModel;
using atomex.Views.CreateSwap;
using atomex.Resources;
using Xamarin.Forms.PlatformConfiguration;
using Xamarin.Forms.PlatformConfiguration.AndroidSpecific;
using Application = Xamarin.Forms.Application;
using TabbedPage = Xamarin.Forms.TabbedPage;

namespace atomex
{
    // Learn more about making custom code visible in the Xamarin.Forms previewer
    // by visiting https://aka.ms/xamarinforms-previewer
    [DesignTimeVisible(false)]
    public partial class MainPage : TabbedPage, INavigationService
    {

        private readonly NavigationPage navigationConversionPage;

        public MainViewModel _mainViewModel { get; }

        public MainPage(MainViewModel mainViewModel)
        {
            On<Android>().SetToolbarPlacement(ToolbarPlacement.Bottom);
            _mainViewModel = mainViewModel;

            var navigationPortfolioPage = new NavigationPage(new Portfolio(_mainViewModel.CurrenciesViewModel))
            {
                IconImageSource = "NavBarPortfolio",
                Title = AppResources.PortfolioTab
            };

            var navigationWalletsListPage = new NavigationPage(new CurrenciesListPage(_mainViewModel.CurrenciesViewModel, _mainViewModel.AtomexApp, this))
            {
                IconImageSource = "NavBarWallets",
                Title = AppResources.WalletsTab
            };

            navigationConversionPage = new NavigationPage(new ConversionsListPage(_mainViewModel.ConversionViewModel))
            {
                IconImageSource = "NavBarConversion",
                Title = AppResources.ConversionTab
            };

            var navigationSettingsPage = new NavigationPage(new SettingsPage(_mainViewModel.SettingsViewModel, this))
            {
                IconImageSource = "NavBarSettings",
                Title = AppResources.SettingsTab
            };

            if (Application.Current.Resources.TryGetValue("NavigationBarBackgroundColor", out var navBarColor))
                navigationWalletsListPage.BarBackgroundColor =
                    navigationPortfolioPage.BarBackgroundColor =
                    navigationConversionPage.BarBackgroundColor =
                    navigationSettingsPage.BarBackgroundColor =
                    (Color)navBarColor;
            if (Application.Current.Resources.TryGetValue("NavigationBarTextColor", out var navBarTextColor))
                navigationWalletsListPage.BarTextColor =
                    navigationPortfolioPage.BarTextColor =
                    navigationConversionPage.BarTextColor =
                    navigationSettingsPage.BarTextColor =
                    (Color)navBarTextColor;
            if (Application.Current.Resources.TryGetValue("TabBarBackgroundColor", out var tabBarBackgroundColor))
                navigationWalletsListPage.BackgroundColor =
                    navigationPortfolioPage.BackgroundColor=
                    navigationConversionPage.BackgroundColor =
                    navigationSettingsPage.BackgroundColor =
                    (Color)tabBarBackgroundColor;
            if (Application.Current.Resources.TryGetValue("SelectedTabColor", out var selectedTabColor))
                this.SelectedTabColor = (Color)selectedTabColor;
            if (Application.Current.Resources.TryGetValue("UnselectedTabColor", out var unSelectedTabColor))
                this.UnselectedTabColor = (Color)unSelectedTabColor;

            Children.Add(navigationPortfolioPage);
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

            if (Application.Current.Resources.TryGetValue("NavigationBarBackgroundColor", out var navBarColor))
                ((NavigationPage)Application.Current.MainPage).BarBackgroundColor = (Color)navBarColor;
            if (Application.Current.Resources.TryGetValue("NavigationBarTextColor", out var navBarTextColor))
                ((NavigationPage)Application.Current.MainPage).BarTextColor = (Color)navBarTextColor;
        }

        public void ConvertCurrency(string currencyCode)
        {
            this.CurrentPage = navigationConversionPage;
            var conversionViewModel = navigationConversionPage.RootPage.BindingContext as ConversionViewModel;
            if (conversionViewModel != null)
            {
                conversionViewModel.SetFromCurrency(currencyCode);
                navigationConversionPage.Navigation.PopToRootAsync();
                navigationConversionPage.PushAsync(new CurrenciesPage(conversionViewModel));
            }
        }
    }
}
