﻿using System;
using System.Reactive;
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
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Xamarin.Essentials;

namespace atomex.ViewModel
{
    public class ReceiveViewModel : BaseViewModel
    {
        private IAtomexApp _app { get; }
        private INavigationService _navigationService { get; }

        [Reactive] public CurrencyConfig Currency { get; set; }
        [Reactive] public WalletAddressViewModel SelectedAddress { get; set; }
        [Reactive] public string ReceivingAddressLabel { get; set; }
        [Reactive] public string MyAddressesButtonName { get; set; }
        [Reactive] public string CopyButtonName { get; set; }
        [Reactive] public bool IsCopied { get; set; }
        public SelectAddressViewModel SelectAddressViewModel { get; set; }

        public string TokenContract { get; private set; }
        public string TokenType { get; private set; }

        public ReceiveViewModel(
            IAtomexApp app,
            CurrencyConfig currency,
            INavigationService navigationService,
            string tokenContract = null,
            string tokenType = null)
        {
            _app = app ?? throw new ArgumentNullException(nameof(_app));
            _navigationService = navigationService ?? throw new ArgumentNullException(nameof(_navigationService));
            Currency = currency ?? throw new ArgumentNullException(nameof(Currency));
            TokenContract = tokenContract;
            TokenType = tokenType;

            _navigationService?.SetInitiatedPage(TabNavigation.Portfolio);
            SelectAddressViewModel = new SelectAddressViewModel(
                account: _app.Account,
                currency: Currency,
                navigationService: _navigationService,
                tab: TabNavigation.Portfolio,
                mode: SelectAddressMode.ChooseMyAddress)
            {
                ConfirmAction = (selectAddressViewModel, walletAddressViewModel) =>
                {
                    SelectedAddress = walletAddressViewModel;

                    _navigationService.ReturnToInitiatedPage(TabNavigation.Portfolio);
                    _navigationService?.ShowBottomSheet(new ReceiveBottomSheet(this));
                }
            };

            SelectedAddress = SelectAddressViewModel?.SelectDefaultAddress()!;

            this.WhenAnyValue(vm => vm.Currency)
                .WhereNotNull()
                .SubscribeInMainThread(_ =>
                {
                    MyAddressesButtonName = string.Format(AppResources.MyCurrencyAddresses, Currency.Name);
                    ReceivingAddressLabel = string.Format(AppResources.ReceivingCurrencyAddress, Currency.Name);
                });

            CopyButtonName = AppResources.CopyAddress;
        }

        private ReactiveCommand<Unit, Unit> _showReceiveAddressesCommand;
        public ReactiveCommand<Unit, Unit> ShowReceiveAddressesCommand => _showReceiveAddressesCommand ??= ReactiveCommand.Create(() =>
            {
                _navigationService?.CloseBottomSheet();
                _navigationService?.ShowPage(new SelectAddressPage(SelectAddressViewModel), TabNavigation.Portfolio);
            });

        private ReactiveCommand<Unit, Unit> _copyCommand;
        public ReactiveCommand<Unit, Unit> CopyCommand => _copyCommand ??= ReactiveCommand.CreateFromTask(async () =>
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
                _navigationService?.ShowAlert(AppResources.Error, AppResources.CopyError, AppResources.AcceptButton);
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
        public ICommand CloseBottomSheetCommand => _closeBottomSheetCommand ??= ReactiveCommand.Create(() =>_navigationService?.CloseBottomSheet());
    }
}

