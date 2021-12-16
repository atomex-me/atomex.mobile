using System.ComponentModel;

using Xamarin.Forms;
using Xamarin.Forms.PlatformConfiguration;
using Xamarin.Forms.PlatformConfiguration.AndroidSpecific;
using Application = Xamarin.Forms.Application;

using atomex.CustomElements;
using atomex.Resources;
using atomex.ViewModel;
using atomex.Helpers;
using System.Threading.Tasks;
using CurrenciesPage = atomex.Views.BuyCurrency.CurrenciesPage;
using atomex.ViewModel.CurrencyViewModels;
using atomex.Views.TezosTokens;
using atomex.Views.CreateSwap;

namespace atomex
{
    // Learn more about making custom code visible in the Xamarin.Forms previewer
    // by visiting https://aka.ms/xamarinforms-previewer
    [DesignTimeVisible(false)]
    public partial class MainPage : CustomTabbedPage, INavigationService
    {
        private readonly NavigationPage NavigationConversionPage;

        private readonly NavigationPage NavigationWalletsListPage;

        private readonly NavigationPage NavigationPortfolioPage;

        private readonly NavigationPage NavigationBuyPage;

        private readonly NavigationPage NavigationSettingsPage;

        public MainViewModel MainViewModel { get; }

        public MainPage(MainViewModel mainViewModel)
        {
            InitializeComponent();

            On<Android>().SetToolbarPlacement(ToolbarPlacement.Bottom);

            MainViewModel = mainViewModel;

            NavigationPortfolioPage = new NavigationPage(new Portfolio(MainViewModel.PortfolioViewModel))
            {
                IconImageSource = "NavBarPortfolio",
                Title = AppResources.PortfolioTab
            };

            NavigationWalletsListPage = new NavigationPage(new CurrenciesListPage(MainViewModel.CurrenciesViewModel))
            {
                IconImageSource = "NavBarWallets",
                Title = AppResources.WalletsTab
            };

            NavigationConversionPage = new NavigationPage(new ConversionsListPage(MainViewModel.ConversionViewModel))
            {
                IconImageSource = "NavBarConversion",
                Title = AppResources.ConversionTab
            };

            NavigationSettingsPage = new NavigationPage(new SettingsPage(MainViewModel.SettingsViewModel))
            {
                IconImageSource = "NavBarSettings",
                Title = AppResources.SettingsTab
            };

            NavigationBuyPage = new NavigationPage(new CurrenciesPage(MainViewModel.BuyViewModel))
            {
                IconImageSource = "NavBarBuy",
                Title = AppResources.BuyTab
            };

            MainViewModel.SettingsViewModel.Navigation = NavigationSettingsPage.Navigation;
            MainViewModel.CurrenciesViewModel.SetNavigation(NavigationWalletsListPage.Navigation, this);
            MainViewModel.ConversionViewModel.Navigation = NavigationConversionPage.Navigation;
            MainViewModel.PortfolioViewModel.NavigationService = this;
            MainViewModel.BuyViewModel.Navigation = NavigationBuyPage.Navigation;

            SetAppTheme();

            Children.Add(NavigationPortfolioPage);
            Children.Add(NavigationWalletsListPage);
            Children.Add(NavigationConversionPage);
            Children.Add(NavigationBuyPage);
            Children.Add(NavigationSettingsPage);

            mainViewModel.Locked += (s, a) =>
            {
                SignOut();
            };

            LocalizationResourceManager.Instance.LanguageChanged += (s, a) =>
            {
                Device.BeginInvokeOnMainThread(LocalizeNavTabs);
            };
        }

        public void LocalizeNavTabs()
        {
            NavigationPortfolioPage.Title = AppResources.PortfolioTab;
            NavigationWalletsListPage.Title = AppResources.WalletsTab;
            NavigationConversionPage.Title = AppResources.ConversionTab;
            NavigationSettingsPage.Title = AppResources.SettingsTab;
            NavigationBuyPage.Title = AppResources.BuyTab;
        }

        public void SetAppTheme()
        {
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
                NavigationWalletsListPage.BarBackgroundColor =
                NavigationPortfolioPage.BarBackgroundColor =
                NavigationConversionPage.BarBackgroundColor =
                NavigationBuyPage.BarBackgroundColor =
                NavigationSettingsPage.BarBackgroundColor =
                (Color)navBarColor;

            if (Application.Current.Resources.TryGetValue(navBarTextColorName, out var navBarTextColor))
                NavigationWalletsListPage.BarTextColor =
                NavigationPortfolioPage.BarTextColor =
                NavigationConversionPage.BarTextColor =
                NavigationBuyPage.BarTextColor =
                NavigationSettingsPage.BarTextColor =
                (Color)navBarTextColor;

            if (Application.Current.Resources.TryGetValue(tabBarBackgroundColorName, out var tabBarBackgroundColor))
                NavigationWalletsListPage.BackgroundColor =
                NavigationPortfolioPage.BackgroundColor =
                NavigationConversionPage.BackgroundColor =
                NavigationBuyPage.BackgroundColor =
                NavigationSettingsPage.BackgroundColor =
                (Color)tabBarBackgroundColor;
        }

        private void SignOut()
        {
            MainViewModel.SignOut();
            StartViewModel startViewModel = new StartViewModel(MainViewModel.AtomexApp);
            Application.Current.MainPage = new NavigationPage(new StartPage(startViewModel));
            startViewModel.Navigation = Application.Current.MainPage.Navigation;

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

        public async Task ConvertCurrency(string currencyCode)
        {
            if (NavigationConversionPage.RootPage.BindingContext is ConversionViewModel conversionViewModel)
            {
                conversionViewModel.SetFromCurrency(currencyCode);
                _ = NavigationConversionPage.Navigation.PopToRootAsync(false);
                CurrentPage = NavigationConversionPage;

                await NavigationConversionPage.PushAsync(new ExchangePage(conversionViewModel));
            }
        }

        public async Task ShowCurrency(CurrencyViewModel currencyViewModel)
        {
            if (currencyViewModel == null)
                return;

            _ = NavigationWalletsListPage.Navigation.PopToRootAsync(false);
            CurrentPage = NavigationWalletsListPage;

            await NavigationWalletsListPage.PushAsync(new CurrencyPage(currencyViewModel));
        }


        public async Task ShowTezosTokens(TezosTokensViewModel tezosTokensViewModel)
        {
            if (tezosTokensViewModel == null)
                return;

            _ = NavigationWalletsListPage.Navigation.PopToRootAsync(false);
            CurrentPage = NavigationWalletsListPage;

            await NavigationWalletsListPage.PushAsync(new TezosTokensListPage(tezosTokensViewModel));
        }
    }
}
