using System;
using System.IO;
using System.Security;
using System.Security.Cryptography;
using System.Threading.Tasks;
using System.Windows.Input;
using atomex.Common;
using atomex.Resources;
using atomex.ViewModel;
using atomex.Views;
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

        private SecureString _storagePasswordConfirmation;
        public SecureString StoragePasswordConfirmation
        {
            get => _storagePasswordConfirmation;
            set { _storagePasswordConfirmation = value; OnPropertyChanged(nameof(StoragePasswordConfirmation)); }
        }

        private bool _isEnteredStoragePassword = false;
        public bool IsEnteredStoragePassword
        {
            get => _isEnteredStoragePassword;
            set { _isEnteredStoragePassword = value; OnPropertyChanged(nameof(IsEnteredStoragePassword)); }
        }

        private string _header;
        public string Header
        {
            get => _header;
            set { _header = value; OnPropertyChanged(nameof(Header)); }
        }

        private float _opacity = 1f;
        public float Opacity
        {
            get => _opacity;
            set { _opacity = value; OnPropertyChanged(nameof(Opacity)); }
        }

        private bool _isPinExist;
        public bool IsPinExist
        { 
            get => _isPinExist;
            set { _isPinExist = value; OnPropertyChanged(nameof(IsPinExist)); }
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
            Navigation = navigation ?? throw new ArgumentNullException(nameof(Navigation));
            StoragePassword = new SecureString();
            WalletName = wallet?.Name;
            _ = CheckPin();
            _ = CheckBiometric();
        }

        private void AddChar(string str)
        {
            if (IsPinExist)
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
            else
            {
                if (!IsEnteredStoragePassword)
                {
                    if (StoragePassword?.Length < 4)
                    {
                        foreach (char c in str)
                        {
                            StoragePassword.AppendChar(c);
                        }

                        OnPropertyChanged(nameof(StoragePassword));

                        if (StoragePassword?.Length == 4)
                        {
                            IsEnteredStoragePassword = true;

                            Header = AppResources.ReEnterPin;
                            OnPropertyChanged(nameof(Header));
                        }
                    }
                }
                else
                {
                    if (StoragePasswordConfirmation?.Length < 4)
                    {
                        foreach (char c in str)
                        {
                            StoragePasswordConfirmation.AppendChar(c);
                        }

                        OnPropertyChanged(nameof(StoragePasswordConfirmation));

                        if (StoragePasswordConfirmation?.Length == 4)
                        {
                            if (IsValidStoragePassword())
                            {
                                // todo: await core encrypt with PIN
                                _ = EnablePin();
                                _ = UnlockAsync();
                            }
                            else
                            {
                                _ = ShakePage();
                                ClearStoragePswd();
                            }
                        }
                    }
                }
            }
        }

        private void RemoveChar()
        {
            if (!IsEnteredStoragePassword)
            {
                if (StoragePassword?.Length != 0)
                {
                    StoragePassword.RemoveAt(StoragePassword.Length - 1);
                    OnPropertyChanged(nameof(StoragePassword));
                }
            }
            else
            {
                if (StoragePasswordConfirmation?.Length != 0)
                {
                    StoragePasswordConfirmation.RemoveAt(StoragePasswordConfirmation.Length - 1);
                    OnPropertyChanged(nameof(StoragePasswordConfirmation));
                }
            }
        }

        private async Task EnablePin()
        {
            IsPinExist = true;

            try
            {
                await SecureStorage.SetAsync(WalletName + "-" + "AuthVersion", "1.1");
            }
            catch (Exception ex)
            {
                Log.Error(ex, AppResources.NotSupportSecureStorage);
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

        private void ClearStoragePswd()
        {
            IsEnteredStoragePassword = false;
            StoragePassword.Clear();
            StoragePasswordConfirmation.Clear();
            Header = AppResources.CreatePin;

            OnPropertyChanged(nameof(Header));
            OnPropertyChanged(nameof(StoragePassword));
            OnPropertyChanged(nameof(StoragePasswordConfirmation));
        }

        private bool IsValidStoragePassword()
        {
            if (StoragePassword != null &&
                StoragePasswordConfirmation != null &&
                !StoragePassword.SecureEqual(StoragePasswordConfirmation) || StoragePasswordConfirmation == null)
            {
                return false;
            }

            return true;
        }

        private ICommand _unlockCommand;
        public ICommand UnlockCommand => _unlockCommand ??= new Command(async () => await UnlockAsync());

        private ICommand _addCharCommand;
        public ICommand AddCharCommand => _addCharCommand ??= new Command<string>((value) => AddChar(value));

        private ICommand _deleteCharCommand;
        public ICommand DeleteCharCommand => _deleteCharCommand ??= new Command(() => RemoveChar());

        private ICommand _backCommand;
        public ICommand BackCommand => _backCommand ??= new Command(async () => await BackButtonClicked());

        private ICommand _textChangedCommand;
        public ICommand TextChangedCommand => _textChangedCommand ??= new Command<string>((value) => SetPassword(value));

        private async Task BackButtonClicked()
        {
            await Navigation.PopAsync();
        }

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
                    try
                    {
                        if (IsPinExist)
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
                            // todo: await core encrypt with PIN
                            // storagePassword to temp
                            //ClearStoragePswd(); 
                            await Navigation.PushAsync(new AuthPage(this));
                        }
                    }
                    catch (Exception ex)
                    {
                        // msg to user
                        Log.Error(ex, AppResources.NotSupportSecureStorage);
                        return;
                    }
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

        private async Task CheckPin()
        {
            string authType = await SecureStorage.GetAsync(WalletName + "-" + "AuthVersion");

            if (authType == "1.1")
            {
                _header = AppResources.EnterPin;
                _isPinExist = true;
            }
            else
            {
                _header = AppResources.CreatePin;
                _isPinExist = false;
            }

            OnPropertyChanged(nameof(Header));
            OnPropertyChanged(nameof(IsPinExist));
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

        public async Task CheckBiometric()
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

