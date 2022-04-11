using System;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.Cryptography;
using System.Threading;
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
                OnPropertyChanged(nameof(IsLoading));
            }
        }

        private bool _isLocked;
        public bool IsLocked
        {
            get => _isLocked;
            set { _isLocked = value; OnPropertyChanged(nameof(IsLocked)); }
        }

        private string _warning;
        public string Warning
        {
            get => _warning;
            set { _warning = value; OnPropertyChanged(nameof(Warning)); }
        }

        private CancellationTokenSource Cancellation { get; set; }

        private static TimeSpan CheckLockInterval = TimeSpan.FromSeconds(5);
        private static TimeSpan LockTime = TimeSpan.FromMinutes(2);
        private readonly int DefaultAttemptsCount = 5;

        private Account _userAccount;

        public UnlockViewModel(IAtomexApp app, WalletInfo wallet, INavigation navigation)
        {
            AtomexApp = app ?? throw new ArgumentNullException(nameof(AtomexApp));
            Navigation = navigation ?? throw new ArgumentNullException(nameof(Navigation));
            StoragePassword = new SecureString();
            WalletName = wallet?.Name;
            CheckWalletLock();
            _ = CheckPinExist();
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
                await SecureStorage.SetAsync(WalletName + "-" + "AuthType", "Pin");
                await SecureStorage.SetAsync(WalletName + "-" + "PinAttempts", DefaultAttemptsCount.ToString());
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
            StoragePassword?.Clear();
            StoragePasswordConfirmation?.Clear();
            Header = AppResources.SetPinIsteadOfPswd;
            Warning = string.Empty;

            OnPropertyChanged(nameof(Header));
            OnPropertyChanged(nameof(Warning));
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
        public ICommand BackCommand => _backCommand ??= new Command(() => OnBackButtonTapped());

        private ICommand _textChangedCommand;
        public ICommand TextChangedCommand => _textChangedCommand ??= new Command<string>((value) => SetPassword(value));

        private ICommand _cancelCommand;
        public ICommand CancelCommand => _cancelCommand ??= new Command(async () => await OnCancelButtonTapped());

        private async Task OnCancelButtonTapped()
        {
            Cancellation?.Cancel();
            _userAccount = null;
            await Navigation.PopAsync();
        }

        private void OnBackButtonTapped()
        {
            _userAccount = null;
            Cancellation?.Cancel();
        }

        private async Task UnlockAsync()
        {
            IsLoading = true;

            Account account = null;

            try
            {
                if (_userAccount == null)
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
                }
                else
                {
                    account = _userAccount;

                    try
                    {
                        account.ChangePassword(StoragePassword);
                        await SecureStorage.SetAsync(WalletName, SecureStringToString(StoragePassword));
                    }
                    catch (Exception e)
                    {
                        Log.Error(e, "Change password error");
                    }
                }

                if (account != null)
                {
                    try
                    {
                        if (IsPinExist)
                        {
                            MainViewModel mainViewModel = null;

                            await Task.Run(() =>
                            {
                                mainViewModel = new MainViewModel(AtomexApp, account, WalletName);
                            });

                            Application.Current.MainPage = new MainPage(mainViewModel);

                            try
                            {
                                await SecureStorage.SetAsync(WalletName + "-" + "PinAttempts", DefaultAttemptsCount.ToString());
                            }
                            catch (Exception ex)
                            {
                                Log.Error(ex, AppResources.NotSupportSecureStorage);
                            }
                        }
                        else
                        {
                            IsLoading = false;
                            _userAccount = account;
                            StoragePasswordConfirmation = new SecureString();
                            ClearStoragePswd(); 
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
                    UpdateAttemptsCounter();
                    _ = ShakePage();
                }
            }
            catch (CryptographicException e)
            {
                IsLoading = false;
                StoragePassword.Clear();
                OnPropertyChanged(nameof(StoragePassword));
                UpdateAttemptsCounter();
                _ = ShakePage();
                Log.Error(e, "Invalid password error");
            }
        }

        private async Task CheckPinExist()
        {
            string authType = await SecureStorage.GetAsync(WalletName + "-" + "AuthType");

            if (authType == "Pin")
            {
                _header = AppResources.EnterPin;
                _isPinExist = true;
            }
            else
            {
                _header = AppResources.SetPinIsteadOfPswd;
                _isPinExist = false;
            }

            OnPropertyChanged(nameof(Header));
            OnPropertyChanged(nameof(IsPinExist));
        }

        private String SecureStringToString(SecureString value)
        {
            IntPtr valuePtr = IntPtr.Zero;
            try
            {
                valuePtr = Marshal.SecureStringToGlobalAllocUnicode(value);
                return Marshal.PtrToStringUni(valuePtr);
            }
            finally
            {
                Marshal.ZeroFreeGlobalAllocUnicode(valuePtr);
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

        private async void CheckWalletLock()
        {
            try
            {
                string lockTime = await SecureStorage.GetAsync(WalletName + "-" + "LockTime");

                if (string.IsNullOrEmpty(lockTime))
                {
                    IsLocked = false;

                    int.TryParse(await SecureStorage.GetAsync(WalletName + "-" + "PinAttempts"), out int attemptsCount);

                    if (attemptsCount < DefaultAttemptsCount)
                    {
                        string message = string.Format(CultureInfo.InvariantCulture, AppResources.AttemptsLeft, attemptsCount);
                        Warning = AppResources.InvalidPin + $"\r\n" + message;
                    }
                }
                else
                {
                    _ = StartLockTimer();
                }
            }
            catch (Exception e)
            {
                Log.Error(e, "Check wallet lock error");
            }
        }

        private async Task StartLockTimer()
        {
            try
            {
                string time = await SecureStorage.GetAsync(WalletName + "-" + "LockTime");

                DateTime unlockTime = Convert.ToDateTime(time);

                if (DateTime.Compare(DateTime.Now.ToLocalTime(), unlockTime) < 0)
                {
                    Cancellation = new CancellationTokenSource();

                    IsLocked = true;

                    var lockMinutes = Math.Ceiling(unlockTime.Subtract(DateTime.UtcNow.ToLocalTime()).TotalMinutes);

                    Warning = string.Format(CultureInfo.InvariantCulture, AppResources.TryAgainInMinutes, lockMinutes);

                    while (IsLocked)
                    {
                        if (Cancellation.IsCancellationRequested)
                        {
                            return;
                        }

                        await Task.Delay(CheckLockInterval);

                        var newTimeMin = Math.Ceiling(unlockTime.Subtract(DateTime.UtcNow.ToLocalTime()).TotalMinutes);

                        if (newTimeMin != lockMinutes)
                        {
                            Warning = string.Format(CultureInfo.InvariantCulture, AppResources.TryAgainInMinutes, newTimeMin);
                            lockMinutes = newTimeMin;
                        }

                        if (DateTime.Compare(DateTime.Now.ToLocalTime(), unlockTime) >= 0)
                        {
                            try
                            {
                                IsLocked = false;
                                await SecureStorage.SetAsync(WalletName + "-" + "LockTime", string.Empty);
                        
                                int.TryParse(await SecureStorage.GetAsync(WalletName + "-" + "PinAttempts"), out int attemptsCount);
                                attemptsCount++;
                                await SecureStorage.SetAsync(WalletName + "-" + "PinAttempts", attemptsCount.ToString());

                                if (attemptsCount < DefaultAttemptsCount)
                                {
                                    string message = string.Format(CultureInfo.InvariantCulture, AppResources.AttemptsLeft, attemptsCount);
                                    Warning = AppResources.InvalidPin + $"\r\n" + message;
                                }    
                            }
                            catch (Exception ex)
                            {
                                Log.Error(ex, AppResources.NotSupportSecureStorage);
                            }
                        }
                    }
                }
                else
                {
                    IsLocked = false;
                    await SecureStorage.SetAsync(WalletName + "-" + "LockTime", string.Empty);

                    int.TryParse(await SecureStorage.GetAsync(WalletName + "-" + "PinAttempts"), out int attemptsCount);
                    attemptsCount++;
                    await SecureStorage.SetAsync(WalletName + "-" + "PinAttempts", attemptsCount.ToString());

                    if (attemptsCount < DefaultAttemptsCount)
                    {
                        string message = string.Format(CultureInfo.InvariantCulture, AppResources.AttemptsLeft, attemptsCount);
                        Warning = AppResources.InvalidPin + $"\r\n" + message;
                    }
                }
            }
            catch(Exception e)
            {
                Log.Error(e, "Lock timer error");
            }
        }

        private async void UpdateAttemptsCounter()
        {
            try
            {
                if (IsLocked)
                    return;

                int.TryParse(await SecureStorage.GetAsync(WalletName + "-" + "PinAttempts"), out int attemptsCount);

                if (attemptsCount == 0)
                    return;

                attemptsCount--;
                await SecureStorage.SetAsync(WalletName + "-" + "PinAttempts", attemptsCount.ToString());

                string message = string.Format(CultureInfo.InvariantCulture, AppResources.AttemptsLeft, attemptsCount);
                Warning = AppResources.InvalidPin + $"\r\n" + message;

                if (attemptsCount == 0)
                {
                    IsLocked = true;

                    var lockTime = DateTime.UtcNow.ToLocalTime().AddMinutes(LockTime.Minutes);
                    await SecureStorage.SetAsync(WalletName + "-" + "LockTime", lockTime.ToString());
                    _ = StartLockTimer();
                }
            }
            catch(Exception e)
            {
                Log.Error(e, "Update attempts counter error");
            }
        }

        private async Task CheckBiometric()
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

