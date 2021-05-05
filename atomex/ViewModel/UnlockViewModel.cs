using System;
using System.IO;
using System.Security;
using System.Security.Cryptography;
using System.Threading.Tasks;
using System.Windows.Input;
using atomex.Common;
using atomex.Resources;
using atomex.ViewModel;
using Atomex;
using Atomex.Common;
using Atomex.Wallet;
using Plugin.Fingerprint;
using Plugin.Fingerprint.Abstractions;
using Serilog;
using Xamarin.Essentials;
using Xamarin.Forms;
using FileSystem = Atomex.Common.FileSystem;

namespace atomex
{
    public class UnlockViewModel : BaseViewModel
    {
        public IAtomexApp AtomexApp { get; }

        public INavigation Navigation { get; set; }

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

        private float _opacity = 1f;
        public float Opacity
        {
            get => _opacity;
            set { _opacity = value; OnPropertyChanged(nameof(Opacity)); }
        }

        private bool _isLoading = false;
        public bool IsLoading
        {
            get => _isLoading;
            set
            {
                if (_isLoading == value)
                    return;

                _isLoading = value;

                if (_isLoading)
                    Opacity = 0.3f;
                else
                    Opacity = 1f;

                OnPropertyChanged(nameof(IsLoading));
            }
        }

        public UnlockViewModel(IAtomexApp app, WalletInfo wallet, INavigation navigation)
        {
            AtomexApp = app ?? throw new ArgumentNullException(nameof(AtomexApp));
            Navigation = navigation;
            WalletName = wallet?.Name;
            _ = BiometricAuth();
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

        private void SetPassword(string pswd)
        {
            SecureString secureString = GenerateSecureString(pswd);
            Password = secureString;
        }

        private ICommand _unlockCommand;
        public ICommand UnlockCommand => _unlockCommand ??= new Command(async () => await UnlockAsync());

        private ICommand _textChangedCommand;
        public ICommand TextChangedCommand => _textChangedCommand ??= new Command<string>((value) => SetPassword(value));

        private async Task UnlockAsync()
        {
            IsLoading = true;

            Account account = null;

            if (Password == null || Password.Length == 0)
            {
                IsLoading = false;
                await Application.Current.MainPage.DisplayAlert(AppResources.Error, AppResources.InvalidPassword, AppResources.AcceptButton);
                return;
            }

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

                account = await Task.Run(() =>
                {
                    return Account.LoadFromFile(
                        walletPath,
                        Password,
                        AtomexApp.CurrenciesProvider,
                        clientType);
                });

                if (account != null)
                {
                    MainViewModel mainViewModel = null;

                    await Task.Run(() =>
                    {
                        mainViewModel = new MainViewModel(AtomexApp, account, WalletName);
                    });

                    Application.Current.MainPage = new MainPage(mainViewModel);
                }
                else
                {
                    IsLoading = false;
                    await Application.Current.MainPage.DisplayAlert(AppResources.Error, AppResources.InvalidPassword, AppResources.AcceptButton);
                }
            }
            catch (CryptographicException e)
            {
                IsLoading = false;
                await Application.Current.MainPage.DisplayAlert(AppResources.Error, AppResources.InvalidPassword, AppResources.AcceptButton);
                Log.Error(e, "Invalid password error");
            }

        }

        public async Task BiometricAuth()
        {
            try
            {
                string pswd = await SecureStorage.GetAsync(WalletName);
                if (string.IsNullOrEmpty(pswd))
                    return;

                bool isFingerprintAvailable = await CrossFingerprint.Current.IsAvailableAsync();
                if (isFingerprintAvailable)
                {
                    AuthenticationRequestConfiguration conf = new AuthenticationRequestConfiguration(
                            AppResources.Authentication,
                            AppResources.UseBiometric + $"'{WalletName}'");

                    var authResult = await CrossFingerprint.Current.AuthenticateAsync(conf);
                    if (authResult.Authenticated)
                    {
                        SetPassword(pswd);
                        _ = UnlockAsync();
                    }
                    else
                    {
                        await Application.Current.MainPage.DisplayAlert(AppResources.SorryLabel, AppResources.NotAuthenticated, AppResources.AcceptButton);
                    }
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

