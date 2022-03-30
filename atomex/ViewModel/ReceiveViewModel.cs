using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using atomex.Common;
using atomex.Resources;
using atomex.ViewModel.SendViewModels;
using atomex.Views;
using atomex.Views.Send;
using Atomex;
using Atomex.Common;
using Atomex.Core;
using Atomex.ViewModels;
using Atomex.Wallet.Abstract;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Rg.Plugins.Popup.Services;
using Xamarin.Essentials;
using Xamarin.Forms;

namespace atomex.ViewModel
{
    public class ReceiveViewModel : BaseViewModel
    {
        private IAtomexApp _app { get; }
        public INavigation _navigation { get; set; }

        [Reactive] public CurrencyConfig Currency { get; set; }
        [Reactive] public WalletAddressViewModel SelectedAddress { get; set; }
        [Reactive] public string ReceivingCurrencyAddressLabel { get; set; }
        [Reactive] public string MyCurrencyAddressesLabel { get; set; }
        [Reactive] public string CopyButtonName { get; set; }

        public string TokenContract { get; private set; }
        [Reactive] public bool IsCopied { get; set; }

        public ReceiveViewModel(IAtomexApp app, CurrencyConfig currency, INavigation navigation)
        {
            _app = app ?? throw new ArgumentNullException(nameof(_app));
            _navigation = navigation ?? throw new ArgumentNullException(nameof(_navigation));
            Currency = currency ?? throw new ArgumentNullException(nameof(Currency));

            this.WhenAnyValue(vm => vm.Currency)
                .WhereNotNull()
                .SubscribeInMainThread(_ =>
                {
                    MyCurrencyAddressesLabel = string.Format(AppResources.MyCurrencyAddresses, Currency.Name);
                    ReceivingCurrencyAddressLabel = string.Format(AppResources.ReceivingCurrencyAddress, Currency.Name);

                    //get all addresses with tokens(if exists)
                    var tokenAddresses = Currencies.HasTokens(Currency.Name)
                        ? (_app.Account
                            .GetCurrencyAccount(Currency.Name) as IHasTokens)
                            ?.GetUnspentTokenAddressesAsync()
                            .WaitForResult() ?? new List<WalletAddress>()
                        : new List<WalletAddress>();

                    // get all active addresses
                    var activeAddresses = _app.Account
                        .GetUnspentAddressesAsync(Currency.Name)
                        .WaitForResult()
                        .ToList();

                    // get free external address
                    var freeAddress = _app.Account
                        .GetFreeExternalAddressAsync(Currency.Name)
                        .WaitForResult();

                    var walletAddressList = activeAddresses
                        .Concat(tokenAddresses)
                        .Concat(new WalletAddress[] { freeAddress })
                        .GroupBy(w => w.Address)
                        .Select(g =>
                        {
                            // main address
                            var address = g.FirstOrDefault(w => w.Currency == Currency.Name);

                            var isFreeAddress = address?.Address == freeAddress.Address;

                            var hasTokens = g.Any(w => w.Currency != Currency.Name);

                            var tokenAddresses = TokenContract != null
                                ? g.Where(w => w.TokenBalance?.Contract == TokenContract)
                                : Enumerable.Empty<WalletAddress>();

                            var hasSeveralTokens = tokenAddresses.Count() > 1;

                            var tokenAddress = tokenAddresses.FirstOrDefault();

                            var tokenBalance = hasSeveralTokens
                                ? tokenAddresses.Count()
                                : tokenAddress?.Balance ?? 0m;

                            var showTokenBalance = hasSeveralTokens
                                ? tokenBalance != 0
                                : TokenContract != null && tokenAddress?.TokenBalance?.Symbol != null;

                            var tokenCode = hasSeveralTokens
                                ? "TOKENS"
                                : tokenAddress?.TokenBalance?.Symbol ?? "";

                            return new WalletAddressViewModel
                            {
                                Address = g.Key,
                                HasActivity = address?.HasActivity ?? hasTokens,
                                AvailableBalance = address?.AvailableBalance() ?? 0m,
                                CurrencyFormat = Currency.Format,
                                CurrencyCode = Currency.Name,
                                IsFreeAddress = isFreeAddress,
                                ShowTokenBalance = showTokenBalance,
                                TokenBalance = tokenBalance,
                                TokenFormat = "F8",
                                TokenCode = tokenCode
                            };
                        })
                        .ToList();

                    if (Currency is TezosConfig || Currency is EthereumConfig)
                    {
                        var activeAddressViewModel = walletAddressList
                            .Where(vm => vm.HasActivity && vm.AvailableBalance > 0)
                            .MaxByOrDefault(vm => vm.AvailableBalance);

                        if (activeAddressViewModel != null)
                            SelectedAddress = activeAddressViewModel;
                    }
                    else
                    {
                        SelectedAddress = walletAddressList.First(vm => vm.IsFreeAddress);
                    }
                });

            CopyButtonName = AppResources.CopyAddress;
        }

        public ReceiveViewModel(IAtomexApp app, CurrencyConfig currency, INavigation navigation, string tokenContract = null)
        {
            _app = app ?? throw new ArgumentNullException(nameof(_app));
            _navigation = navigation ?? throw new ArgumentNullException(nameof(_navigation));
            TokenContract = tokenContract;
            Currency = currency ?? throw new ArgumentNullException(nameof(Currency));
        }

        private ICommand _showReceiveAddressesCommand;
        public ICommand ShowReceiveAddressesCommand => _showReceiveAddressesCommand ??= new Command(() =>
            {
                var selectAddressViewModel = new SelectAddressViewModel(
                    account: _app.Account,
                    currency: Currency,
                    navigation: _navigation,
                    mode: SelectAddressMode.ChooseMyAddress)
                {
                    ConfirmAction = (selectAddressViewModel, walletAddressViewModel) =>
                    {
                        SelectedAddress = walletAddressViewModel;

                        if (selectAddressViewModel.SelectAddressFrom == SelectAddressFrom.InitSearch)
                            _navigation?.RemovePage(_navigation.NavigationStack[_navigation.NavigationStack.Count - 2]);
                        
                        _ = PopupNavigation.Instance.PushAsync(new ReceiveBottomSheet(this));
                        _navigation?.PopAsync();
                    }
                };

                if (PopupNavigation.Instance.PopupStack.Count > 0)
                    _ = PopupNavigation.Instance.PopAsync();
                _navigation?.PushAsync(new SelectAddressPage(selectAddressViewModel));
            });

        private ICommand _copyCommand;
        public ICommand CopyCommand => _copyCommand ??= new Command(async () =>
        {
            if (SelectedAddress != null)
            {
                IsCopied = true;
                CopyButtonName = AppResources.Copied;
                await Clipboard.SetTextAsync(SelectedAddress.Address);
                await Task.Delay(1500);
                IsCopied = false;
                CopyButtonName = AppResources.CopyAddress;
            }
            else
            {
                await Application.Current.MainPage.DisplayAlert(AppResources.Error, AppResources.CopyError, AppResources.AcceptButton);
            }
        });

        //private ICommand _shareCommand;
        //public ICommand ShareCommand => _shareCommand ??= new Command(async () =>
        //{
        //    IsLoading = true;

        //    await Share.RequestAsync(new ShareTextRequest
        //    {
        //        Text = AppResources.MyPublicAddress +
        //            " " +
        //            SelectedAddress.CurrencyCode +
        //            ":\r\n" +
        //            SelectedAddress.Address,

        //        Title = AppResources.AddressSharing
        //    });

        //    await Task.Delay(1000);

        //    IsLoading = false;
        //
        //});

        private ICommand _closeBottomSheetCommand;
        public ICommand CloseBottomSheetCommand => _closeBottomSheetCommand ??= new Command(() =>
        {
            if (PopupNavigation.Instance.PopupStack.Count > 0)
                _ = PopupNavigation.Instance.PopAsync();
        });
    }
}

