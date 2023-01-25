using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Atomex.Core;
using atomex.ViewModels;
using Rg.Plugins.Popup.Pages;
using Serilog;
using Xamarin.CommunityToolkit.Extensions;
using Xamarin.CommunityToolkit.UI.Views.Options;
using Xamarin.Essentials;
using Xamarin.Forms;
using static atomex.Models.SnackbarMessage;

namespace atomex.Views
{
    public partial class StartPage : ContentPage, INavigationService
    {
        public StartPage(StartViewModel startViewModel)
        {
            InitializeComponent();
            NavigationPage.SetHasNavigationBar(this, false);
            BindingContext = startViewModel;
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

        public void ShowPage(Page page, TabNavigation tab = TabNavigation.None)
        {
            if (page == null)
                return;

            if (tab != TabNavigation.None)
                return;

            Navigation?.PushAsync(page);
        }

        public void ClosePage(TabNavigation tab = TabNavigation.None)
        {
            if (tab != TabNavigation.None)
                return;

            Navigation?.PopAsync();
        }

        public void GoToBuy(CurrencyConfig currencyConfig)
        {
            throw new NotImplementedException();
        }

        public void GoToExchange(CurrencyConfig currencyConfig)
        {
            throw new NotImplementedException();
        }

        public void RemovePreviousPage(TabNavigation tab)
        {
            throw new NotImplementedException();
        }

        public void ShowPopup(PopupPage popup, bool removePrevious)
        {
            throw new NotImplementedException();
        }

        public bool HasMultipleBottomSheets()
        {
            throw new NotImplementedException();
        }

        public void ClosePopup()
        {
            throw new NotImplementedException();
        }

        public async Task ShowAlert(string title, string text, string cancel)
        {
            await Application.Current.MainPage.DisplayAlert(title, text, cancel);
        }

        public async Task<bool> ShowAlert(string title, string text, string accept, string cancel)
        {
            return await Application.Current.MainPage.DisplayAlert(title, text, accept, cancel);
        }

        public void SetInitiatedPage(TabNavigation tab)
        {
            throw new NotImplementedException();
        }

        public Task ReturnToInitiatedPage(TabNavigation tab)
        {
            throw new NotImplementedException();
        }

        public async void ConnectDappByDeepLink(string qrCode)
        {
            try
            {
                await SecureStorage.SetAsync("DappDeepLink", qrCode);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Device doesn't support secure storage on device");
            }
        }

        public void AllowCamera()
        {
            throw new NotImplementedException();
        }

        public Task<string> DisplayActionSheet(string cancel, string[] actions, string title = null)
        {
            throw new NotImplementedException();
        }
    }
}