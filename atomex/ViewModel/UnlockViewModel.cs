using System;
using System.IO;
using System.Security;
using System.Security.Cryptography;
using atomex.Common;
using atomex.ViewModel;
using Atomex;
using Atomex.Common;
using Atomex.Wallet;
using Serilog;
using Xamarin.Forms;

namespace atomex
{
    public class UnlockViewModel : BaseViewModel
    {
        public IAtomexApp AtomexApp { get; }

        private string _walletName;
        public string WalletName
        {
            get => _walletName;
            set { _walletName = value; OnPropertyChanged(nameof(WalletName)); }
        }

        private SecureString _password;
        public SecureString Password
        {
            get => _password;
            set { _password = value; OnPropertyChanged(nameof(Password)); }
        }

        public UnlockViewModel(IAtomexApp app, WalletInfo wallet)
        {
            AtomexApp = app ?? throw new ArgumentNullException(nameof(AtomexApp));
            WalletName = wallet.Name;
        }

        private SecureString GenerateSecureString(string str)
        {
            var secureString = new SecureString();
            foreach (char c in str)
            {
                secureString.AppendChar(c);
            }
            return secureString;
        }

        public void SetPassword(string pswd)
        {
            SecureString secureString = GenerateSecureString(pswd);
            Password = secureString;
        }

        public Account Unlock()
        {
            if (Password == null || Password.Length == 0)
                return null;
            try
            {
                var fileSystem = FileSystem.Current;

                ClientType clientType;

                switch (Device.RuntimePlatform)
                {
                    case Device.iOS:
                        clientType = ClientType.iOS;
                        break;
                    case Device.Android:
                        clientType = ClientType.Android;
                        break;
                    default:
                        clientType = ClientType.Unknown;
                        break;
                }

                var walletPath = Path.Combine(
                    fileSystem.PathToDocuments,
                    WalletInfo.DefaultWalletsDirectory,
                    WalletName,
                    WalletInfo.DefaultWalletFileName);

                return Account.LoadFromFile(
                    walletPath,
                    Password,
                    AtomexApp.CurrenciesProvider,
                    AtomexApp.SymbolsProvider,
                    clientType);
            }
            catch (CryptographicException e)
            {
                Log.Error(e, "Invalid password error");
                return null;
            }
        }
    }
}

