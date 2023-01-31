using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Windows.Input;
using atomex.Views.BuyCurrency;
using Atomex;
using Atomex.Common;
using Atomex.Core;
using Atomex.Cryptography.Abstract;
using atomex.Models;
using Atomex.ViewModels;
using Atomex.Wallet.Abstract;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Serilog;
using Xamarin.Essentials;
using Xamarin.Forms;

namespace atomex.ViewModels
{
    public class BuyViewModel : BaseViewModel
    {
        private IAtomexApp _app;
        private INavigationService NavigationService { get; set; }

        [Reactive] public bool IsLoading { get; set; }
        [Reactive] public Network Network { get; set; }
        [Reactive] public string Url { get; set; }

        private string _userId;

        public static ObservableCollection<string> Currencies { get; } =
            new ObservableCollection<string>
            {
                "BTC",
                "ETH",
                "XTZ"
            };

        public ObservableCollection<string> AvailableCurrencies { get; set; }

        public BuyViewModel(IAtomexApp app)
        {
            _app = app ?? throw new ArgumentNullException(nameof(app));
            AvailableCurrencies = new ObservableCollection<string>(Currencies);
            IsLoading = true;
            _userId = GetUserId();
            Network = app.Account.Network;
        }

        public void SetNavigationService(INavigationService navigationService)
        {
            NavigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));
        }

        private ReactiveCommand<string, Unit> _selectCurrencyCommand;

        public ReactiveCommand<string, Unit> SelectCurrencyCommand => _selectCurrencyCommand ??=
            ReactiveCommand.Create<string>((value) => LoadWebView(value));

        private void LoadWebView(string currency)
        {
            try
            {
                var appTheme = Application.Current.RequestedTheme.ToString().ToLower();
                var address = GetDefaultAddress(currency);
                var baseUri = Network == Network.MainNet
                    ? "https://widget.wert.io/atomex"
                    : "https://sandbox.wert.io/01F298K3HP4DY326AH1NS3MM3M";

                Url = $"{baseUri}/widget" +
                      $"?commodity={currency}" +
                      $"&address={address}" +
                      $"&click_id=user:{_userId}/network:{Network}" +
                      $"&theme={appTheme}";

                NavigationService?.ShowPage(new BuyPage(this), TabNavigation.Buy);
            }
            catch (Exception e)
            {
                Log.Error(e, "Load web view error");
            }
        }

        private ICommand _canExecuteCommand;

        public ICommand CanExecuteCommand => _canExecuteCommand ??=
            ReactiveCommand.Create<WebNavigatingEventArgs>((args) => CanExecute(args));

        private void CanExecute(WebNavigatingEventArgs args)
        {
            IsLoading = false;

            if (Constants.WertRedirectedUrls.Any(u => args.Url.StartsWith(u)))
            {
                Launcher.OpenAsync(new Uri(args.Url));
                args.Cancel = true;
            }
        }

        public void BuyCurrency(CurrencyConfig currency)
        {
            if (currency != null)
                LoadWebView(currency.Name);
        }

        private string GetUserId()
        {
            using var servicePublicKey =
                _app.Account.Wallet.GetServicePublicKey(_app.Account.UserData.AuthenticationKeyIndex);
            var publicKey = servicePublicKey.ToUnsecuredBytes();

            return HashAlgorithm.Sha256.Hash(publicKey).ToHexString();
        }

        public string GetDefaultAddress(string currency)
        {
            // get all addresses with tokens (if exists)
            var tokenAddresses = Atomex.Currencies.HasTokens(currency)
                ? (_app.Account
                    .GetCurrencyAccount(currency) as IHasTokens)
                ?.GetUnspentTokenAddressesAsync()
                .WaitForResult() ?? new List<WalletAddress>()
                : new List<WalletAddress>();

            // get all active addresses
            var activeAddresses = _app.Account
                .GetUnspentAddressesAsync(currency)
                .WaitForResult()
                .ToList();

            // get free external address
            var freeAddress = _app.Account
                .GetFreeExternalAddressAsync(currency)
                .WaitForResult();

            List<WalletAddressViewModel> fromAddressList = new List<WalletAddressViewModel>();

            fromAddressList = activeAddresses
                .Concat(tokenAddresses)
                .Concat(new WalletAddress[] {freeAddress})
                .GroupBy(w => w.Address)
                .Select(g =>
                {
                    // main address
                    var address = g.FirstOrDefault(w => w.Currency == currency);

                    var isFreeAddress = address?.Address == freeAddress.Address;

                    var hasTokens = g.Any(w => w.Currency != currency);

                    return new WalletAddressViewModel
                    {
                        Address = g.Key,
                        HasActivity = address?.HasActivity ?? hasTokens,
                        AvailableBalance = address?.AvailableBalance() ?? 0m,
                        CurrencyCode = currency,
                        IsFreeAddress = isFreeAddress,
                    };
                })
                .ToList();

            if (currency == "ETH" || currency == "XTZ")
            {
                var activeAddressViewModel = fromAddressList
                    .Where(vm => vm.HasActivity && vm.AvailableBalance > 0)
                    .MaxByOrDefault(vm => vm.AvailableBalance);

                if (activeAddressViewModel != null)
                    return activeAddressViewModel.Address;
            }

            return fromAddressList.First(vm => vm.IsFreeAddress).Address;
        }
    }
}