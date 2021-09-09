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

        private SecureString _storagePassword;
        public SecureString StoragePassword
        {
            get => _storagePassword;
            set { _storagePassword = value; OnPropertyChanged(nameof(StoragePassword)); }
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

        public string Header => AppResources.EnterPin;

        public UnlockViewModel(IAtomexApp app, WalletInfo wallet, INavigation navigation)
        {
            AtomexApp = app ?? throw new ArgumentNullException(nameof(AtomexApp));
            Navigation = navigation ?? throw new ArgumentNullException(nameof(Navigation));
            StoragePassword = new SecureString();
            WalletName = wallet?.Name;
            _ = Auth();
        }

        private void AddChar(string str)
        {
            if (StoragePassword?.Length < 4)
            {
                foreach (char c in str)
                {
                    StoragePassword.AppendChar(c);
                }

                OnPropertyChanged(nameof(StoragePassword));

                if (StoragePassword.Length == 4)
                    _ = UnlockAsync();
            }
        }

        private void RemoveChar()
        {
            if (StoragePassword?.Length != 0)
            {
                StoragePassword.RemoveAt(StoragePassword.Length - 1);
                OnPropertyChanged(nameof(StoragePassword));
            }
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
            StoragePassword = secureString;
        }

        private ICommand _unlockCommand;
        public ICommand UnlockCommand => _unlockCommand ??= new Command(async () => await UnlockAsync());

        private ICommand _addCharCommand;
        public ICommand AddCharCommand => _addCharCommand ??= new Command<string>((value) => AddChar(value));

        private ICommand _deleteCharCommand;
        public ICommand DeleteCharCommand => _deleteCharCommand ??= new Command(() => RemoveChar());

        private async Task UnlockAsync()
        {
            IsLoading = true;

            Account account = null;

            if (StoragePassword == null || StoragePassword.Length == 0)
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
                        StoragePassword,
                        AtomexApp.CurrenciesProvider,
                        clientType);
                });

                if (account != null)
                {
                    string appTheme = Application.Current.RequestedTheme.ToString().ToLower();

                    MainViewModel mainViewModel = null;

                    await Task.Run(() =>
                    {
                        mainViewModel = new MainViewModel(AtomexApp, account, WalletName, appTheme);
                    });

                    Application.Current.MainPage = new MainPage(mainViewModel);
                }
                else
                {
                    IsLoading = false;
                    StoragePassword.Clear();
                    OnPropertyChanged(nameof(StoragePassword));
                    _ = ShakePage();
                }
            }
            catch (CryptographicException e)
            {
                IsLoading = false;
                StoragePassword.Clear();
                OnPropertyChanged(nameof(StoragePassword));
                _ = ShakePage();
                Log.Error(e, "Invalid password error");
            }
        }

        private async Task ShakePage()
        {
            try
            {
                Vibration.Vibrate();
            }
            catch (FeatureNotSupportedException ex)
            {
                Log.Error(ex, "Vibration not supported on device");
            }

            var view = Application.Current.MainPage;
            await view.TranslateTo(-15, 0, 50);
            await view.TranslateTo(15, 0, 50);
            await view.TranslateTo(-10, 0, 50);
            await view.TranslateTo(10, 0, 50);
            await view.TranslateTo(-5, 0, 50);
            await view.TranslateTo(5, 0, 50);
            view.TranslationX = 0;
        }

        public async Task Auth()
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

