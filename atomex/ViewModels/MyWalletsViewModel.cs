using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;
using atomex.Common;
using atomex.Resources;
using atomex.Views;
using Atomex;
using ReactiveUI.Fody.Helpers;
using Serilog;
using Xamarin.Essentials;
using Xamarin.Forms;

namespace atomex.ViewModels
{
    public class MyWalletsViewModel : BaseViewModel
    {
        private IAtomexApp _app { get; set; }
        private INavigationService _navigationService { get; set; }
        [Reactive] public List<WalletInfo> Wallets { get; set; }

        public MyWalletsViewModel(
            IAtomexApp app,
            INavigationService navigationService)
        {
            _app = app ?? throw new ArgumentNullException(nameof(app));
            _navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));
            ;
            Wallets = WalletInfo.AvailableWallets().ToList();
        }

        private ICommand _selectWalletCommand;

        public ICommand SelectWalletCommand => _selectWalletCommand ??= new Command<WalletInfo>(async (wallet) =>
        {
            try
            {
                string authType = await SecureStorage.GetAsync(wallet.Name + "-" + "AuthType");

                if (authType == "Pin")
                {
                    _navigationService?.ShowPage(new AuthPage(new UnlockViewModel(_app, wallet, _navigationService)));
                }
                else
                {
                    _navigationService?.ShowPage(
                        new UnlockWalletPage(new UnlockViewModel(_app, wallet, _navigationService)));
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Device doesn't support secure storage on device");
            }
        });
    }
}