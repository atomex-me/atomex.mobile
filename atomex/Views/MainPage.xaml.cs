using System.ComponentModel;

using Xamarin.Forms;
using Xamarin.Forms.PlatformConfiguration;
using Xamarin.Forms.PlatformConfiguration.AndroidSpecific;
using Application = Xamarin.Forms.Application;

using atomex.CustomElements;
using atomex.Resources;
using atomex.ViewModel;
using atomex.Views.CreateSwap;

namespace atomex
{
    // Learn more about making custom code visible in the Xamarin.Forms previewer
    // by visiting https://aka.ms/xamarinforms-previewer
    [DesignTimeVisible(false)]
    public partial class MainPage : CustomTabbedPage, INavigationService
    {
        private readonly NavigationPage navigationConversionPage;

        private readonly NavigationPage navigationWalletsListPage;

        private readonly NavigationPage navigationPortfolioPage;

        private readonly NavigationPage navigationSettingsPage;

        public MainViewModel _mainViewModel { get; }

        public MainPage(MainViewModel mainViewModel)
        {
            InitializeComponent();

            On<Android>().SetToolbarPlacement(ToolbarPlacement.Bottom);

            _mainViewModel = mainViewModel;

            navigationPortfolioPage = new NavigationPage(new Portfolio(_mainViewModel.CurrenciesViewModel, this))
            {
                IconImageSource = "NavBarPortfolio",
                Title = AppResources.PortfolioTab
            };

            navigationWalletsListPage = new NavigationPage(new CurrenciesListPage(_mainViewModel.CurrenciesViewModel, _mainViewModel.AtomexApp, this))
            {
                IconImageSource = "NavBarWallets",
                Title = AppResources.WalletsTab
            };

            navigationConversionPage = new NavigationPage(new ConversionsListPage(_mainViewModel.ConversionViewModel))
            {
                IconImageSource = "NavBarConversion",
                Title = AppResources.ConversionTab
            };

            navigationSettingsPage = new NavigationPage(new SettingsPage(_mainViewModel.SettingsViewModel, this))
            {
                IconImageSource = "NavBarSettings",
                Title = AppResources.SettingsTab
            };

            SetAppTheme();

            Children.Add(navigationPortfolioPage);
            Children.Add(navigationWalletsListPage);
            Children.Add(navigationConversionPage);
            Children.Add(navigationSettingsPage);

            mainViewModel.Locked += (s, a) =>
            {
                SignOut();
            };
        }

        public void SetAppTheme()
        {
            // на этапе загрузки засетить все цвета

            string navBarBackgroundColorName = "NavigationBarBackgroundColor";
            string navBarTextColorName = "NavigationBarTextColor";
            string tabBarBackgroundColorName = "TabBarBackgroundColor";

            if (Application.Current.RequestedTheme == OSAppTheme.Dark)
            {
                navBarBackgroundColorName = "NavigationBarBackgroundColorDark";
                navBarTextColorName = "NavigationBarTextColorDark";
                tabBarBackgroundColorName = "TabBarBackgroundColorDark";
            }


            if (Application.Current.Resources.TryGetValue(navBarBackgroundColorName, out var navBarColor))
                navigationWalletsListPage.BarBackgroundColor =
                navigationPortfolioPage.BarBackgroundColor =
                navigationConversionPage.BarBackgroundColor =
                navigationSettingsPage.BarBackgroundColor =
                (Color)navBarColor;

            if (Application.Current.Resources.TryGetValue(navBarTextColorName, out var navBarTextColor))
                navigationWalletsListPage.BarTextColor =
                navigationPortfolioPage.BarTextColor =
                navigationConversionPage.BarTextColor =
                navigationSettingsPage.BarTextColor =
                (Color)navBarTextColor;

            if (Application.Current.Resources.TryGetValue(tabBarBackgroundColorName, out var tabBarBackgroundColor))
                navigationWalletsListPage.BackgroundColor =
                navigationPortfolioPage.BackgroundColor =
                navigationConversionPage.BackgroundColor =
                navigationSettingsPage.BackgroundColor =
                (Color)tabBarBackgroundColor;
        }

        private void SignOut()
        {
            _mainViewModel.SignOut();
            StartViewModel startViewModel = new StartViewModel(_mainViewModel.AtomexApp);
            Application.Current.MainPage = new NavigationPage(new StartPage(startViewModel));

            string navBarBackgroundColorName = "NavigationBarBackgroundColor";
            string navBarTextColorName = "NavigationBarTextColor";

            if (Application.Current.RequestedTheme == OSAppTheme.Dark)
            {
                navBarBackgroundColorName = "NavigationBarBackgroundColorDark";
                navBarTextColorName = "NavigationBarTextColorDark";
            }

            Application.Current.Resources.TryGetValue(navBarBackgroundColorName, out var navBarColor);
            Application.Current.Resources.TryGetValue(navBarTextColorName, out var navBarTextColor);

            ((NavigationPage)Application.Current.MainPage).BarBackgroundColor = (Color)navBarColor;
            ((NavigationPage)Application.Current.MainPage).BackgroundColor = (Color)navBarColor;
            ((NavigationPage)Application.Current.MainPage).BarTextColor = (Color)navBarTextColor;
        }

        public void ConvertCurrency(string currencyCode)
        {
            if (navigationConversionPage.RootPage.BindingContext is ConversionViewModel conversionViewModel)
            {
                conversionViewModel.SetFromCurrency(currencyCode);
                navigationConversionPage.Navigation.PopToRootAsync(false);
                navigationConversionPage.PushAsync(new CurrenciesPage(conversionViewModel));

                CurrentPage = navigationConversionPage;
            }
        }

        public void ShowCurrency(CurrencyViewModel currencyViewModel)
        {
            navigationWalletsListPage.Navigation.PopToRootAsync(false);
            navigationWalletsListPage.PushAsync(new CurrencyPage(currencyViewModel, _mainViewModel.AtomexApp, this));

            CurrentPage = navigationWalletsListPage;
        }
    }
}
