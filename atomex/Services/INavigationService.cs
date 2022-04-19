﻿using System.Threading.Tasks;
using Atomex.Core;
using Rg.Plugins.Popup.Pages;
using Xamarin.Forms;
using static atomex.Models.SnackbarMessage;

namespace atomex
{
    public enum TabNavigation
    {
        None,
        Portfolio,
        Exchange,
        Buy,
        Settings
    }

    public interface INavigationService
    {
        void GoToBuy(CurrencyConfig currencyConfig);
        void GoToExchange(CurrencyConfig currencyConfig);
        void ShowPage(Page page, TabNavigation tab = TabNavigation.None);
        void ClosePage(TabNavigation tab = TabNavigation.None);
        void RemovePreviousPage(TabNavigation tab);
        void ShowBottomSheet(PopupPage popup);
        void CloseBottomSheet();
        bool HasMultipleBottomSheets();
        void DisplaySnackBar(MessageType messageType, string text, string btnTxt = "OK", int duration = 3000);
        Task ShowAlert(string title, string text, string cancel);
        Task<bool> ShowAlert(string title, string text, string accept, string cancel);
    }
}
