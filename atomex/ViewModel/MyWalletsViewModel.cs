using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using atomex.Common;
using atomex.Resources;
using atomex.ViewModel;
using atomex.Views;
using Atomex;
using Serilog;
using Xamarin.Essentials;
using Xamarin.Forms;

namespace atomex
{
    public class MyWalletsViewModel : BaseViewModel
    {
        public IAtomexApp AtomexApp { get; private set; }

        public INavigation Navigation { get; set; }

        public List<WalletInfo> Wallets { get; set; }

        public MyWalletsViewModel(IAtomexApp app, INavigation navigation)
        {
            AtomexApp = app ?? throw new ArgumentNullException(nameof(AtomexApp));
            Navigation = navigation;
            Wallets = WalletInfo.AvailableWallets().ToList();
        }

        private ICommand _selectWalletCommand;
        public ICommand SelectWalletCommand => _selectWalletCommand ??= new Command<WalletInfo>(async (value) => await OnWalletTapped(value));

        private async Task OnWalletTapped(WalletInfo wallet)
        {
            try
            {
                string authType = await SecureStorage.GetAsync(wallet.Name + "-" + "AuthVersion");

                if (authType == "1.1")
                {
                    await Navigation.PushAsync(new AuthPage(new UnlockViewModel(AtomexApp, wallet, Navigation)));
                }
                else
                {
                    await Navigation.PushAsync(new UnlockWalletPage(new UnlockViewModel(AtomexApp, wallet, Navigation)));
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, AppResources.NotSupportSecureStorage);
                return;
            }
        }
    }
}
