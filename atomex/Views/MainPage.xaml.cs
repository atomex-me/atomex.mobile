using System.ComponentModel;
using Xamarin.Forms;
using Xamarin.Forms.PlatformConfiguration;
using Xamarin.Forms.PlatformConfiguration.AndroidSpecific;
using Application = Xamarin.Forms.Application;
using atomex.CustomElements;
using atomex.Resources;
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
using System.Threading;
using atomex.ViewModels;
using atomex.ViewModels.ConversionViewModels;
using atomex.ViewModels.CurrencyViewModels;
using atomex.Views.TezosTokens;
using static atomex.Models.SnackbarMessage;
using Rg.Plugins.Popup.Services;
using Rg.Plugins.Popup.Pages;
using Serilog;

namespace atomex.Views
{
    // Learn more about making custom code visible in the Xamarin.Forms previewer
    // by visiting https://aka.ms/xamarinforms-previewer
    [DesignTimeVisible(false)]
    public partial class MainPage : CustomTabbedPage, INavigationService
    {
        private readonly NavigationPage _navigationConversionPage;
        private readonly NavigationPage _navigationPortfolioPage;
        private readonly NavigationPage _navigationBuyPage;
        private readonly NavigationPage _navigationSettingsPage;

        public MainViewModel MainViewModel { get; }

        private int _initiatedPageNumber;

        public MainPage(MainViewModel mainViewModel)
        {
            InitializeComponent();

            On<Android>().SetToolbarPlacement(ToolbarPlacement.Bottom);

            MainViewModel = mainViewModel ?? throw new ArgumentNullException(nameof(mainViewModel));

            _navigationPortfolioPage = new NavigationPage(
                new Portfolio(MainViewModel.PortfolioViewModel))
            {
                IconImageSource = "ic_navbar__portfolio",
                Title = AppResources.PortfolioTab
            };

            _navigationConversionPage = new NavigationPage(
                new ExchangePage(MainViewModel.ConversionViewModel))
            {
                IconImageSource = "ic_navbar__dex",
                Title = AppResources.ConversionTab
            };

            _navigationSettingsPage = new NavigationPage(
                new SettingsPage(MainViewModel.SettingsViewModel))
            {
                IconImageSource = "ic_navbar__settings",
                Title = AppResources.SettingsTab
            };

            _navigationBuyPage = new NavigationPage(
                new CurrenciesPage(MainViewModel.BuyViewModel))
            {
                IconImageSource = "ic_navbar__buy",
                Title = AppResources.BuyTab
            };

            MainViewModel.SettingsViewModel.SetNavigationService(this);
            MainViewModel.BuyViewModel.SetNavigationService(this);
            MainViewModel.ConversionViewModel.SetNavigationService(this);
            MainViewModel.PortfolioViewModel.SetNavigationService(this);

            Children.Add(_navigationPortfolioPage);
            Children.Add(_navigationConversionPage);
            Children.Add(_navigationBuyPage);
            Children.Add(_navigationSettingsPage);

            mainViewModel.Locked += (s, a) => SignOut();
            
            _navigationPortfolioPage.Popped += (s, e) =>
            {
                switch (e.Page)
                {
                    case CurrencyPage page:
                    {
                        var vm = page.BindingContext as CurrencyViewModel;
                        vm?.Reset();
                        break;
                    }
                    case TokenPage tokenPage:
                    {
                        var vm = tokenPage.BindingContext as TezosTokenViewModel;
                        vm?.Reset();
                        break;
                    }
                }
            };

            LocalizationResourceManager.Instance.LanguageChanged += (s, a) =>
                Device.BeginInvokeOnMainThread(LocalizeNavTabs);
        }

        private void LocalizeNavTabs()
        {
            _navigationPortfolioPage.Title = AppResources.PortfolioTab;
            _navigationConversionPage.Title = AppResources.ConversionTab;
            _navigationSettingsPage.Title = AppResources.SettingsTab;
            _navigationBuyPage.Title = AppResources.BuyTab;
        }

        private void SignOut()
        {
            try
            {
                MainViewModel?.SignOut();
                var startViewModel = new StartViewModel(MainViewModel?.AtomexApp);
                var mainPage = new StartPage(startViewModel);
                Application.Current.MainPage = new NavigationPage(mainPage);
                startViewModel.SetNavigationService(mainPage);
            }
            catch (Exception e)
            {
                Log.Error(e, "Sign out error");
            }
        }

        public void GoToBuy(CurrencyConfig currency)
        {
            try
            {
                if (_navigationBuyPage.RootPage.BindingContext is BuyViewModel buyViewModel)
                {
                    _ = _navigationBuyPage.Navigation.PopToRootAsync(false);
                    CurrentPage = _navigationBuyPage;
                    buyViewModel.BuyCurrency(currency);
                }
            }
            catch (Exception e)
            {
                Log.Error(e, "Go to buy error");
            }
        }

        public void GoToExchange(CurrencyConfig currency)
        {
            try
            {
                if (_navigationConversionPage.RootPage.BindingContext is not ConversionViewModel conversionViewModel)
                    return;
                
                _ = _navigationConversionPage.Navigation.PopToRootAsync(false);
                CurrentPage = _navigationConversionPage;
                if (currency == null)
                    conversionViewModel.FromViewModel?.SelectCurrencyCommand.Execute(null);
                else
                    conversionViewModel.SetFromCurrency(currency);
            }
            catch (Exception e)
            {
                Log.Error(e, "Go to exchange error");
            }
        }

        public void ShowPage(
            Page page,
            TabNavigation tab = TabNavigation.None)
        {
            try
            {
                if (page == null)
                    return;

                switch (tab)
                {
                    case TabNavigation.Portfolio:
                        _navigationPortfolioPage.PushAsync(page);
                        break;
                    case TabNavigation.Exchange:
                        _navigationConversionPage.PushAsync(page);
                        break;
                    case TabNavigation.Buy:
                        _navigationBuyPage.PushAsync(page);
                        break;
                    case TabNavigation.Settings:
                        _navigationSettingsPage.PushAsync(page);
                        break;
                    case TabNavigation.None:
                        break;
                }
            }
            catch (Exception e)
            {
                Log.Error(e, "Show page error");
            }
        }

        public void ClosePage(TabNavigation tab)
        {
            try
            {
                switch (tab)
                {
                    case TabNavigation.Portfolio:
                        _navigationPortfolioPage.PopAsync();
                        break;
                    case TabNavigation.Exchange:
                        _navigationConversionPage.PopAsync();
                        break;
                    case TabNavigation.Buy:
                        _navigationBuyPage.PopAsync();
                        break;
                    case TabNavigation.Settings:
                        _navigationSettingsPage.PopAsync();
                        break;
                    case TabNavigation.None:
                        break;
                }
            }
            catch (Exception e)
            {
                Log.Error(e, "Close page error");
            }
        }

        public void RemovePreviousPage(TabNavigation tab)
        {
            try
            {
                switch (tab)
                {
                    case TabNavigation.Portfolio:
                        _navigationPortfolioPage.Navigation.RemovePage(
                            _navigationPortfolioPage.Navigation.NavigationStack[^2]);
                        break;
                    case TabNavigation.Exchange:
                        _navigationConversionPage.Navigation.RemovePage(
                            _navigationConversionPage.Navigation.NavigationStack[^2]);
                        break;
                    case TabNavigation.Buy:
                        _navigationBuyPage.Navigation.RemovePage(
                            _navigationBuyPage.Navigation.NavigationStack[^2]);
                        break;
                    case TabNavigation.Settings:
                        _navigationSettingsPage.Navigation.RemovePage(
                            _navigationSettingsPage.Navigation.NavigationStack[^2]);
                        break;
                    case TabNavigation.None:
                        break;
                }
            }
            catch (Exception e)
            {
                Log.Error(e, "Remove previous page error");
            }
        }

        public void ShowPopup(PopupPage popup, bool removePrevious = true)
        {
            try
            {
                if (removePrevious)
                {
                    for ( ; PopupNavigation.Instance.PopupStack.Count > 0 ; )
                        _ = PopupNavigation.Instance.PopAsync();
                }
                _ = PopupNavigation.Instance.PushAsync(popup);
            }
            catch (Exception e)
            {
                Log.Error(e, "Show bottom sheet error");
            }
        }

        public void ClosePopup()
        {
            try
            {
                if (PopupNavigation.Instance.PopupStack.Count > 0)
                    PopupNavigation.Instance.PopAsync();
            }
            catch (Exception e)
            {
                Log.Error(e, "Close bottom sheet error");
            }
        }

        public bool HasMultipleBottomSheets()
        {
            try
            {
                return PopupNavigation.Instance.PopupStack.Count > 1;
            }
            catch (Exception e)
            {
                Log.Error(e, "Has multiple bottom sheet error");
                return false;
            }
        }

        public async void DisplaySnackBar(
            MessageType messageType,
            string text,
            string buttonText = "OK",
            int duration = 3000,
            Func<Task> action = null,
            CancellationTokenSource cts = null)
        {
            try
            {
                string snackBarBgColorName;
                string snackBarTextColorName;

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
                Application.Current.Resources.TryGetValue(snackBarTextColorName, out var txtColor);

                txtColor ??= Color.Black;
                bgColor ??= Color.White;

                var textColor = (Color)txtColor;
                var backgroundColor = (Color)bgColor;

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
                        Action = async () =>
                        {
                            if (cts != null)
                            {
                                try
                                {
                                    cts.Cancel();
                                }
                                catch (Exception)
                                {
                                    // ignored
                                }
                            }

                            if (action != null)
                                await action();
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

                if (cts == null)
                {
                    await this.DisplaySnackBarAsync(options);
                }
                else
                {
                    var elapsed = 0;
                    var intervalMs = 1000;
                    var delayMs = 100;
                    options.Duration = TimeSpan.FromMilliseconds(intervalMs + delayMs);

                    while (elapsed < duration)
                    {
                        if (cts.IsCancellationRequested)
                            break;

                        _ = this.DisplaySnackBarAsync(options);

                        await Task.Delay(TimeSpan.FromMilliseconds(intervalMs), cts.Token);

                        elapsed += intervalMs;
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // nothing to do
            }
            catch (Exception e)
            {
                Log.Error(e, "Display SnackBar error");
            }
        }

        public async Task ShowAlert(
            string title,
            string text,
            string cancel)
        {
            try
            {
                await Application.Current.MainPage
                    .DisplayAlert(title, text, cancel);
            }
            catch (Exception e)
            {
                Log.Error(e, "Show alert error");
            }
        }

        public async Task<bool> ShowAlert(
            string title,
            string text,
            string accept,
            string cancel)
        {
            try
            {
                return await Application.Current.MainPage
                    .DisplayAlert(title, text, accept, cancel);
            }
            catch (Exception e)
            {
                Log.Error(e, "Show alert error");
                return false;
            }
        }

        public async Task<string> DisplayActionSheet(
            string cancel,
            string[] actions,
            string title = null)
        {
            try
            {
                return await DisplayActionSheet(title: title, cancel: cancel, destruction: null, actions);
            }
            catch (Exception e)
            {
                Log.Error(e, "Display action sheet error");
                return null;
            }
        }

        public void SetInitiatedPage(TabNavigation tab)
        {
            try
            {
                switch (tab)
                {
                    case TabNavigation.Portfolio:
                        _initiatedPageNumber = _navigationPortfolioPage.Navigation.NavigationStack.Count;
                        break;
                    case TabNavigation.Exchange:
                        _initiatedPageNumber = _navigationConversionPage.Navigation.NavigationStack.Count;
                        break;
                    case TabNavigation.Buy:
                        _initiatedPageNumber = _navigationBuyPage.Navigation.NavigationStack.Count;
                        break;
                    case TabNavigation.Settings:
                        _initiatedPageNumber = _navigationSettingsPage.Navigation.NavigationStack.Count;
                        break;
                    case TabNavigation.None:
                        break;
                }
            }
            catch (Exception e)
            {
                Log.Error(e, "Set initiated page error");
            }
        }

        public async Task ReturnToInitiatedPage(TabNavigation tab)
        {
            try
            {
                switch (tab)
                {
                    case TabNavigation.Portfolio:
                        for (int i = _navigationPortfolioPage.Navigation.NavigationStack.Count - 1;
                             i > _initiatedPageNumber;
                             i--)
                        {
                            _navigationPortfolioPage.Navigation.RemovePage(
                                _navigationPortfolioPage.Navigation.NavigationStack[i - 1]);
                        }

                        if (_navigationPortfolioPage.Navigation.NavigationStack.Count > _initiatedPageNumber)
                            await _navigationPortfolioPage.Navigation.PopAsync();
                        break;
                    case TabNavigation.Exchange:
                        for (int i = _navigationConversionPage.Navigation.NavigationStack.Count - 1;
                             i > _initiatedPageNumber;
                             i--)
                        {
                            _navigationConversionPage.Navigation.RemovePage(
                                _navigationConversionPage.Navigation.NavigationStack[i - 1]);
                        }

                        await _navigationConversionPage.Navigation.PopAsync();
                        break;
                    case TabNavigation.Buy:
                        for (int i = _navigationBuyPage.Navigation.NavigationStack.Count - 1;
                             i > _initiatedPageNumber;
                             i--)
                        {
                            _navigationBuyPage.Navigation.RemovePage(
                                _navigationBuyPage.Navigation.NavigationStack[i - 1]);
                        }

                        await _navigationBuyPage.Navigation.PopAsync();
                        break;
                    case TabNavigation.Settings:
                        for (int i = _navigationSettingsPage.Navigation.NavigationStack.Count - 1;
                             i > _initiatedPageNumber;
                             i--)
                        {
                            _navigationSettingsPage.Navigation.RemovePage(
                                _navigationSettingsPage.Navigation.NavigationStack[i - 1]);
                        }

                        await _navigationSettingsPage.Navigation.PopAsync();
                        break;
                    case TabNavigation.None:
                        break;
                }
            }
            catch (Exception e)
            {
                Log.Error(e, "Return to initiated page error");
            }
        }

        public async Task ConnectDappByDeepLink(string qrCode)
        {
            try
            { 
                await MainViewModel.ConnectDappByDeepLink(qrCode);
            }
            catch (Exception e)
            {
                Log.Error(e, "Connect dApp by deepLink error");
            }
        }

        public void AllowCamera()
        {
            try
            { 
                MainViewModel.AllowCamera();
            }
            catch (Exception e)
            {
                Log.Error(e, "Allow camera error");
            }
        }
    }
}