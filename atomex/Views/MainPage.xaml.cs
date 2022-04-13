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
using Xamarin.CommunityToolkit.UI.Views.Options;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xamarin.CommunityToolkit.Extensions;
using System;
using static atomex.Models.SnackbarMessage;

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

        public async void DisplaySnackBar(
            MessageType messageType,
            string text,
            string buttonText = "OK",
            int duration = 3000)
        {
            string snackBarBgColorName;
            string snackBarTextColorName;
            Color backgroundColor = Color.White;
            Color textColor = Color.Black;

            switch (messageType)
            {
                case MessageType.Error:
                    snackBarBgColorName = "ErrorSnackBarBgColor";
                    snackBarTextColorName = "ErrorSnackBarTextColor";
                    break;

                case MessageType.Warning:
                    snackBarBgColorName = "WarningSnackBarBgColor";
                    snackBarTextColorName = "WarningSnackBarTextColor";
                    break;

                case MessageType.Success:
                    snackBarBgColorName = "SuccessSnackBarBgColor";
                    snackBarTextColorName = "SuccessSnackBarTextColor";
                    break;

                case MessageType.Regular:
                    snackBarBgColorName = "RegularSnackBarBgColor";
                    snackBarTextColorName = "RegularSnackBarTextColor";
                    break;

                default:
                    snackBarBgColorName = "RegularSnackBarBgColor";
                    snackBarTextColorName = "RegularSnackBarTextColor";
                    break;
            }

            snackBarBgColorName = Application.Current.RequestedTheme == OSAppTheme.Dark
                ? snackBarBgColorName + "Dark"
                : snackBarBgColorName;
            snackBarTextColorName = Application.Current.RequestedTheme == OSAppTheme.Dark
                ? snackBarTextColorName + "Dark"
                : snackBarTextColorName;

            Application.Current.Resources.TryGetValue(snackBarBgColorName, out var bgColor);
            backgroundColor = (Color)bgColor;
            Application.Current.Resources.TryGetValue(snackBarTextColorName, out var txtColor);
            textColor = (Color)txtColor;

            var messageOptions = new MessageOptions
            {
                Foreground = textColor,
                Font = Font.SystemFontOfSize(14),
                Padding = new Thickness(20, 14),
                Message = text
            };

            var actionOptions = new List<SnackBarActionOptions>
            {
                new SnackBarActionOptions
                {
                    ForegroundColor = textColor,
                    BackgroundColor = Color.Transparent,
                    Font = Font.SystemFontOfSize(17),
                    Text = buttonText,
                    Padding = new Thickness(20, 16),
                    Action = () => 
                    {
                        return Task.CompletedTask;
                    }
                }
            };

            var options = new SnackBarOptions
            {
                MessageOptions = messageOptions,
                Duration = TimeSpan.FromMilliseconds(duration),
                BackgroundColor = backgroundColor,
                IsRtl = false,
                CornerRadius = 4,
                Actions = actionOptions
            };            

            var result = await this.DisplaySnackBarAsync(options);
        }
    }
}
