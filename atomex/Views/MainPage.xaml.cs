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
using Rg.Plugins.Popup.Services;
using Rg.Plugins.Popup.Pages;

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

        private int _initiatedPageNumber;

        public MainPage(MainViewModel mainViewModel)
        {
            InitializeComponent();

            On<Android>().SetToolbarPlacement(ToolbarPlacement.Bottom);

            MainViewModel = mainViewModel;

            NavigationPortfolioPage = new NavigationPage(new Portfolio(MainViewModel.PortfolioViewModel))
            {
                IconImageSource = "ic_navbar__portfolio",
                Title = AppResources.PortfolioTab
            };

            NavigationConversionPage = new NavigationPage(new ExchangePage(MainViewModel.ConversionViewModel))
            {
                IconImageSource = "ic_navbar__dex",
                Title = AppResources.ConversionTab
            };

            NavigationSettingsPage = new NavigationPage(new SettingsPage(MainViewModel.SettingsViewModel))
            {
                IconImageSource = "ic_navbar__settings",
                Title = AppResources.SettingsTab
            };

            NavigationBuyPage = new NavigationPage(new CurrenciesPage(MainViewModel.BuyViewModel))
            {
                IconImageSource = "ic_navbar__buy",
                Title = AppResources.BuyTab
            };

            MainViewModel?.SettingsViewModel?.SetNavigationService(this);
            MainViewModel?.BuyViewModel?.SetNavigationService(this);
            MainViewModel?.ConversionViewModel?.SetNavigationService(this);
            MainViewModel?.PortfolioViewModel?.SetNavigationService(this);

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
            var mainPage = new StartPage(startViewModel);
            Application.Current.MainPage = new NavigationPage(mainPage);
            startViewModel?.SetNavigationService(mainPage);
        }

        public void GoToBuy(CurrencyConfig currency)
        {
            if (NavigationBuyPage.RootPage.BindingContext is BuyViewModel buyViewModel)
            {
                _ = NavigationBuyPage.Navigation.PopToRootAsync(false);
                CurrentPage = NavigationBuyPage;
                buyViewModel.BuyCurrency(currency);
            }
        }

        public void GoToExchange(CurrencyConfig currency)
        {
            if (NavigationConversionPage.RootPage.BindingContext is ConversionViewModel conversionViewModel)
            {
                _ = NavigationConversionPage.Navigation.PopToRootAsync(false);
                CurrentPage = NavigationConversionPage;
                if (currency == null)
                    conversionViewModel?.FromViewModel?.SelectCurrencyCommand.Execute(null);
                else
                    conversionViewModel.SetFromCurrency(currency);
            }
        }

        public void ShowPage(
            Page page,
            TabNavigation tab = TabNavigation.None)
        {
            if (page == null)
                return;

            switch (tab)
            {
                case TabNavigation.Portfolio:
                    NavigationPortfolioPage.PushAsync(page);
                    break;
                case TabNavigation.Exchange:
                    NavigationConversionPage.PushAsync(page);
                    break;
                case TabNavigation.Buy:
                    NavigationBuyPage.PushAsync(page);
                    break;
                case TabNavigation.Settings:
                    NavigationSettingsPage.PushAsync(page);
                    break;
                case TabNavigation.None:
                    break;

                default:
                    break;
            }
        }

        public void ClosePage(TabNavigation tab)
        {
            switch (tab)
            {
                case TabNavigation.Portfolio:
                    NavigationPortfolioPage.PopAsync();
                    break;
                case TabNavigation.Exchange:
                    NavigationConversionPage.PopAsync();
                    break;
                case TabNavigation.Buy:
                    NavigationBuyPage.PopAsync();
                    break;
                case TabNavigation.Settings:
                    NavigationSettingsPage.PopAsync();
                    break;
                case TabNavigation.None:
                    break;

                default:
                    break;
            }
        }

        public void RemovePreviousPage(TabNavigation tab)
        {
            switch (tab)
            {
                case TabNavigation.Portfolio:
                    NavigationPortfolioPage.Navigation.RemovePage(
                        NavigationPortfolioPage.Navigation.NavigationStack[NavigationPortfolioPage.Navigation.NavigationStack.Count - 2]);
                    break;
                case TabNavigation.Exchange:
                    NavigationConversionPage.Navigation.RemovePage(
                        NavigationConversionPage.Navigation.NavigationStack[NavigationConversionPage.Navigation.NavigationStack.Count - 2]);
                    break;
                case TabNavigation.Buy:
                    NavigationBuyPage.Navigation.RemovePage(
                        NavigationBuyPage.Navigation.NavigationStack[NavigationBuyPage.Navigation.NavigationStack.Count - 2]);
                    break;
                case TabNavigation.Settings:
                    NavigationSettingsPage.Navigation.RemovePage(
                        NavigationSettingsPage.Navigation.NavigationStack[NavigationSettingsPage.Navigation.NavigationStack.Count - 2]);
                    break;
                case TabNavigation.None:
                    break;

                default:
                    break;
            }
        }

        public void ShowBottomSheet(PopupPage popup)
        {
            if (PopupNavigation.Instance.PopupStack.Count > 0)
                _ = PopupNavigation.Instance.PopAsync();

            _ = PopupNavigation.Instance.PushAsync(popup);
        }

        public void CloseBottomSheet()
        {
            if (PopupNavigation.Instance.PopupStack.Count > 0)
                PopupNavigation.Instance.PopAsync();
        }

        public bool HasMultipleBottomSheets()
        {
            return PopupNavigation.Instance.PopupStack.Count > 1;
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

        public async Task ShowAlert(
            string title,
            string text,
            string cancel)
        {
            await Application.Current.MainPage
                .DisplayAlert(title, text, cancel);
        }

        public async Task<bool> ShowAlert(
            string title,
            string text,
            string accept,
            string cancel)
        {
            return await Application.Current.MainPage
                .DisplayAlert(title, text, accept, cancel);
        }

        public async Task<string> DisplayActionSheet(
            string cancel,
            string[] actions,
            string title = null)
        {
            return await DisplayActionSheet(title: title, cancel: cancel, destruction: null, actions);
        }

        public void SetInitiatedPage(TabNavigation tab)
        {
            switch (tab)
            {
                case TabNavigation.Portfolio:
                    _initiatedPageNumber = NavigationPortfolioPage.Navigation.NavigationStack.Count;
                    break;
                case TabNavigation.Exchange:
                     _initiatedPageNumber = NavigationConversionPage.Navigation.NavigationStack.Count;
                    break;
                case TabNavigation.Buy:
                    _initiatedPageNumber = NavigationBuyPage.Navigation.NavigationStack.Count;
                    break;
                case TabNavigation.Settings:
                    _initiatedPageNumber = NavigationSettingsPage.Navigation.NavigationStack.Count;
                    break;
                case TabNavigation.None:
                    break;

                default:
                    break;
            }
        }

        public async Task ReturnToInitiatedPage(TabNavigation tab)
        {
            switch (tab)
            {
                case TabNavigation.Portfolio:
                    for (int i = NavigationPortfolioPage.Navigation.NavigationStack.Count - 1; i > _initiatedPageNumber; i--)
                    {
                        NavigationPortfolioPage.Navigation.RemovePage(
                            NavigationPortfolioPage.Navigation.NavigationStack[i - 1]);
                    }
                    await NavigationPortfolioPage.Navigation.PopAsync();
                    break;
                case TabNavigation.Exchange:
                    for (int i = NavigationConversionPage.Navigation.NavigationStack.Count - 1; i > _initiatedPageNumber; i--)
                    {
                        NavigationConversionPage.Navigation.RemovePage(
                            NavigationConversionPage.Navigation.NavigationStack[i - 1]);
                    }
                    await NavigationConversionPage.Navigation.PopAsync();
                    break;
                case TabNavigation.Buy:
                    for (int i = NavigationBuyPage.Navigation.NavigationStack.Count - 1; i > _initiatedPageNumber; i--)
                    {
                        NavigationBuyPage.Navigation.RemovePage(
                            NavigationBuyPage.Navigation.NavigationStack[i - 1]);
                    }
                    await NavigationBuyPage.Navigation.PopAsync();
                    break;
                case TabNavigation.Settings:
                    for (int i = NavigationSettingsPage.Navigation.NavigationStack.Count - 1; i > _initiatedPageNumber; i--)
                    {
                        NavigationSettingsPage.Navigation.RemovePage(
                            NavigationSettingsPage.Navigation.NavigationStack[i - 1]);
                    }
                    await NavigationSettingsPage.Navigation.PopAsync();
                    break;
                case TabNavigation.None:
                    break;

                default:
                    break;
            }
        }
    }
}
