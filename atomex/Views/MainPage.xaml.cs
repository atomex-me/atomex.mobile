using System.ComponentModel;
using Xamarin.Forms;
using Xamarin.Forms.PlatformConfiguration;
using Xamarin.Forms.PlatformConfiguration.AndroidSpecific;
using Application = Xamarin.Forms.Application;
using atomex.CustomElements;
using atomex.Resources;
using atomex.ViewModel;
using atomex.Helpers;
using CurrenciesPage = atomex.Views.BuyCurrency.CurrenciesPage;
using NavigationPage = Xamarin.Forms.NavigationPage;
using atomex.Views.CreateSwap;
using Atomex.Core;

namespace atomex
{
    // Learn more about making custom code visible in the Xamarin.Forms previewer
    // by visiting https://aka.ms/xamarinforms-previewer
    [DesignTimeVisible(false)]
    public partial class MainPage : CustomTabbedPage, INavigationService
    {
        private readonly NavigationPage NavigationConversionPage;

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

            NavigationConversionPage = new NavigationPage(new ExchangePage(MainViewModel.ConversionViewModel))
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
            MainViewModel.ConversionViewModel.Navigation = NavigationConversionPage.Navigation;
            MainViewModel.PortfolioViewModel.Navigation = NavigationPortfolioPage.Navigation;
            MainViewModel.PortfolioViewModel.NavigationService = this;
            MainViewModel.BuyViewModel.Navigation = NavigationBuyPage.Navigation;

            Children.Add(NavigationPortfolioPage);
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
            NavigationConversionPage.Title = AppResources.ConversionTab;
            NavigationSettingsPage.Title = AppResources.SettingsTab;
            NavigationBuyPage.Title = AppResources.BuyTab;
        }

        private void SignOut()
        {
            MainViewModel?.SignOut();
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

        public void ConvertCurrency(CurrencyConfig currency)
        {
            if (NavigationConversionPage.RootPage.BindingContext is ConversionViewModel conversionViewModel)
            {
                _ = NavigationConversionPage.Navigation.PopToRootAsync(false);
                CurrentPage = NavigationConversionPage;
                conversionViewModel.SetFromCurrency(currency);
            }
        }

        public void BuyCurrency(CurrencyConfig currency)
        {
            if (NavigationBuyPage.RootPage.BindingContext is BuyViewModel buyViewModel)
            {
                _ = NavigationBuyPage.Navigation.PopToRootAsync(false);
                CurrentPage = NavigationBuyPage;
                buyViewModel.BuyCurrency(currency);
            }
        }
    }
}
