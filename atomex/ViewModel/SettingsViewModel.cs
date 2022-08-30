using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using atomex.Common;
using atomex.CustomElements;
using atomex.Helpers;
using atomex.Models;
using atomex.Resources;
using atomex.Services;
using atomex.Views;
using atomex.Views.SettingsOptions;
using Atomex;
using atomex.ViewModel.WalletBeacon;
using Atomex.Wallet;
using Plugin.Fingerprint;
using Plugin.Fingerprint.Abstractions;
using Serilog;
using Xamarin.Essentials;
using Xamarin.Forms;
using atomex.Views.WalletBeacon;

namespace atomex.ViewModel
{
    public class SettingsViewModel : BaseViewModel
    {
        public IAtomexApp AtomexApp { get; private set; }

        public INavigation Navigation { get; set; }

        protected IToastService ToastService { get; set; }

        public MainViewModel MainViewModel { get; }

        private const string LanguageKey = nameof(LanguageKey);

        private string YoutubeUrl = "https://www.youtube.com/c/BakingBad";

        private string TwitterUrl = "https://twitter.com/atomex_official";

        private string TelegramUrl = "https://t.me/atomex_official";

        private string SupportUrl = "mailto:support@atomex.me";


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


        private string _warning;
        public string Warning
        {
            get => _warning;
            set { _warning = value; OnPropertyChanged(nameof(Warning)); }
        }

        private bool _biometricSensorAvailibility;
        public bool BiometricSensorAvailibility
        {
            get => _biometricSensorAvailibility;
            set { _biometricSensorAvailibility = value; OnPropertyChanged(nameof(BiometricSensorAvailibility)); }
        }

        private string _walletName;
        public string WalletName
        {
            get => _walletName;
            set { _walletName = value; OnPropertyChanged(nameof(WalletName)); }
        }

        private List<WalletInfo> _wallets;
        public List<WalletInfo> Wallets
        {
            get => _wallets;
            set { _wallets = value; OnPropertyChanged(nameof(Wallets)); }
        }

        private Language _language;
        public Language Language
        {
            get => _language;
            set
            {
                if (_language == value)
                    return;

                if (_language != null)
                    _language.IsActive = false;

                _language = value;

                SetCulture(_language);

                _language.IsActive = true;

                OnPropertyChanged(nameof(Language));
            }
        }

        public ObservableCollection<Language> Languages { get; } = new ObservableCollection<Language>()
        {
            new Language { Name = "English", Code = "en", ShortName = "Eng", IsActive = false },
            new Language { Name = "Français", Code = "fr", ShortName = "Fra", IsActive = false },
            new Language { Name = "Русский", Code = "ru", ShortName = "Rus", IsActive = false },
            new Language { Name = "Türk", Code = "tr", ShortName = "Tur", IsActive = false }
        };

        public string Header => AppResources.EnterPin;

        private SecureString _storagePassword;

        public SecureString StoragePassword
        {
            get => _storagePassword;
            set { _storagePassword = value; OnPropertyChanged(nameof(StoragePassword)); }
        }

        private bool _useBiometric;

        public bool UseBiometric
        {
            get { return _useBiometric; }
            set { _ = UpdateUseBiometric(value); }
        }

        public CultureInfo CurrentCulture => AppResources.Culture ?? Thread.CurrentThread.CurrentUICulture;

        public SettingsViewModel(IAtomexApp app, MainViewModel mainViewModel, string walletName)
        {
            AtomexApp = app ?? throw new ArgumentNullException(nameof(AtomexApp));
            SetUserLanguage();
            WalletName = walletName;
            Wallets = WalletInfo.AvailableWallets().ToList();
            MainViewModel = mainViewModel ?? throw new ArgumentNullException(nameof(MainViewModel));
            ToastService = DependencyService.Get<IToastService>();
            _ = CheckBiometricSensor();
            _ = ResetUseBiometricSetting();
            StoragePassword = new SecureString();
        }

        public async Task ResetUseBiometricSetting()
        {
            try
            {
                string value = await SecureStorage.GetAsync(WalletName);
                if (string.IsNullOrEmpty(value))
                    UseBiometric = false;
                else
                    UseBiometric = true;
            }
            catch (Exception ex)
            {
                UseBiometric = false;
                Log.Error(ex, AppResources.NotSupportSecureStorage);
            }
        }

        public async Task UpdateUseBiometric(bool value)
        {
            if (_useBiometric != value)
            {
                _useBiometric = value;

                if (_useBiometric)
                {
                    var availability = await CrossFingerprint.Current.GetAvailabilityAsync();

                    if (availability == FingerprintAvailability.Available)
                    {
                        await Navigation.PushAsync(new AuthPage(this));
                    }
                    else if (availability == FingerprintAvailability.NoPermission ||
                        availability == FingerprintAvailability.NoFingerprint ||
                        availability == FingerprintAvailability.Denied)
                    {
                        _useBiometric = false;
                        await Application.Current.MainPage.DisplayAlert("", AppResources.NeedPermissionsForBiometricLogin, AppResources.AcceptButton);
                    }
                    else
                    {
                        _useBiometric = false;
                        await Application.Current.MainPage.DisplayAlert(AppResources.SorryLabel, AppResources.ImpossibleEnableBiometric, AppResources.AcceptButton);
                    }
                }
                else
                {
                    try
                    {
                        await SecureStorage.SetAsync(WalletName, string.Empty);
                    }
                    catch (Exception ex)
                    {
                        await Application.Current.MainPage.DisplayAlert(AppResources.Error, AppResources.NotSupportSecureStorage, AppResources.AcceptButton);
                        Log.Error(ex, AppResources.NotSupportSecureStorage);
                    }
                }
                OnPropertyChanged(nameof(UseBiometric));
            }
        }

        public bool CheckAccountExist()
        {
            if (StoragePassword == null || StoragePassword.Length == 0)
                return false;

            try
            {
                var wallet = HdWallet.LoadFromFile(AtomexApp.Account.Wallet.PathToWallet, StoragePassword);
                return true;
            }
            catch (Exception e)
            {
                Log.Error(e, "Enable biometric login error");
                return false;
            }
        }

        public void DeleteWallet(string path)
        {
            try
            {
                string directoryPath = Path.GetDirectoryName(path);
                if (Directory.Exists(directoryPath))
                {
                    Directory.Delete(directoryPath, true);
                    Wallets = WalletInfo.AvailableWallets().ToList();
                }
            }
            catch (Exception e)
            {
                Log.Error(e, "Delete wallet error");
            }
        }

        private async Task EnableBiometric(string pswd)
        {
            IsLoading = true;

            bool accountExist = CheckAccountExist();

            IsLoading = false;
            if (accountExist)
            {
                try
                {
                    string walletName = Path.GetFileName(Path.GetDirectoryName(AtomexApp.Account.Wallet.PathToWallet));
                    await SecureStorage.SetAsync(WalletName, pswd);
                }
                catch (Exception ex)
                {
                    await Application.Current.MainPage.DisplayAlert(AppResources.Error, AppResources.NotSupportSecureStorage, AppResources.AcceptButton);
                    Log.Error(ex, AppResources.NotSupportSecureStorage);
                }

                Warning = string.Empty;
                StoragePassword?.Clear();
                _ = ResetUseBiometricSetting();
                await Navigation.PopAsync();
                ToastService?.Show(AppResources.BiometricAuthEnabled, ToastPosition.Top, Application.Current.RequestedTheme.ToString());
            }
            else
            {
                Warning = AppResources.InvalidPin;
                
                StoragePassword.Clear();
                OnPropertyChanged(nameof(StoragePassword));

                var tabs = ((CustomTabbedPage)Application.Current.MainPage).Children;

                foreach (NavigationPage tab in tabs)
                {
                    if (tab.RootPage is SettingsPage)
                    {
                        try
                        {
                            Vibration.Vibrate();
                        }
                        catch (FeatureNotSupportedException ex)
                        {
                            Log.Error(ex, "Vibration not supported on device");
                        }

                        await tab.TranslateTo(-15, 0, 50);
                        await tab.TranslateTo(15, 0, 50);
                        await tab.TranslateTo(-10, 0, 50);
                        await tab.TranslateTo(10, 0, 50);
                        await tab.TranslateTo(-5, 0, 50);
                        await tab.TranslateTo(5, 0, 50);
                        tab.TranslationX = 0;
                        break;
                    }
                }
            }
        }

        async Task CheckBiometricSensor()
        {
            var availability = await CrossFingerprint.Current.GetAvailabilityAsync();
            BiometricSensorAvailibility = availability != FingerprintAvailability.NoSensor;
        }

        private ICommand _showLanguagesCommand;
        public ICommand ShowLanguagesCommand => _showLanguagesCommand ??= new Command(async () => await ShowLanguages());

        private ICommand _changeLanguageCommand;
        public ICommand ChangeLanguageCommand => _changeLanguageCommand ??= new Command<Language>(async (value) => await ChangeLanguage(value));

        async Task ShowLanguages()
        {
            await Navigation.PushAsync(new LanguagesPage(this));
        }

        async Task ChangeLanguage(Language value)
        {
            Language = value;
            await Navigation.PopAsync();
        }

        private void SetUserLanguage()
        {
            try
            {
                Language = Languages.Where(l => l.Code == Preferences.Get(LanguageKey, CurrentCulture.TwoLetterISOLanguageName)).Single();
            }
            catch(Exception e)
            {
                Log.Error(e, "Set user language error");
                Language = Languages.Where(l => l.Code == "en").Single();
            }
        }

        private void SetCulture(Language language)
        {
            try
            {
                LocalizationResourceManager.Instance.SetCulture(CultureInfo.GetCultureInfo(language.Code));
            }
            catch
            {
                LocalizationResourceManager.Instance.SetCulture(CultureInfo.GetCultureInfo("en"));
            }
        }

        private ICommand _addCharCommand;
        public ICommand AddCharCommand => _addCharCommand ??= new Command<string>((value) => AddChar(value));

        private ICommand _deleteCharCommand;
        public ICommand DeleteCharCommand => _deleteCharCommand ??= new Command(() => RemoveChar());

        private ICommand _cancelCommand;
        public ICommand CancelCommand => _cancelCommand ??= new Command(async () => await OnCancelButtonTapped());

        private async Task OnCancelButtonTapped()
        {
            await Navigation.PopAsync();
        }

        private void AddChar(string str)
        {
            if (StoragePassword?.Length < 4)
            {
                Warning = string.Empty;

                foreach (char c in str)
                {
                    StoragePassword.AppendChar(c);
                }

                OnPropertyChanged(nameof(StoragePassword));
                if (StoragePassword?.Length == 4)
                {
                    _ = EnableBiometric(SecureStringToString(StoragePassword));
                }
            }
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

        private void RemoveChar()
        {
            if (StoragePassword?.Length != 0)
            {
                StoragePassword.RemoveAt(StoragePassword.Length - 1);
                OnPropertyChanged(nameof(StoragePassword));
            }
        }

        private ICommand _backCommand;
        public ICommand BackCommand => _backCommand ??= new Command(() => OnBackButtonTapped());

        private void OnBackButtonTapped()
        {
            Warning = string.Empty;
            StoragePassword?.Clear();
            _ = ResetUseBiometricSetting();
        }

        private ICommand _youtubeCommand;
        public ICommand YoutubeCommand => _youtubeCommand ??= new Command( () => OnYoutubeTapped());

        private ICommand _telegramCommand;
        public ICommand TelegramCommand => _telegramCommand ??= new Command(() => OnTelegramTapped());

        private ICommand _twitterCommand;
        public ICommand TwitterCommand => _twitterCommand ??= new Command(() => OnTwitterTapped());

        private ICommand _supportCommand;
        public ICommand SupportCommand => _supportCommand ??= new Command(() => OnSupportTapped());

        private ICommand _signOutCommand;
        public ICommand SignOutCommand => _signOutCommand ??= new Command(() => SignOut());

        private ICommand _deleteWalletCommand;
        public ICommand DeleteWalletCommand => _deleteWalletCommand ??= new Command<string>((name) => OnWalletTapped(name));

        private ICommand _dappsDevicesCommand;
        public ICommand ShowDappsDevicesCommand => _dappsDevicesCommand ??= new Command(async () => await OnDappsDevicesClicked());
        
        private async void SignOut()
        {
            var res = await Application.Current.MainPage.DisplayAlert(AppResources.SignOut, AppResources.AreYouSure, AppResources.AcceptButton, AppResources.CancelButton);
            if (res)
                MainViewModel.Locked.Invoke(this, EventArgs.Empty);
        }

        private async void OnWalletTapped(string name)
        {
            WalletInfo selectedWallet = Wallets.Where(w => w.Name == name).Single();

            var confirm = await Application.Current.MainPage.DisplayAlert(AppResources.DeletingWallet, AppResources.DeletingWalletText, AppResources.UnderstandButton, AppResources.CancelButton);
            if (confirm)
            {
                var confirm2 = await Application.Current.MainPage.DisplayAlert(AppResources.DeletingWallet, string.Format(CultureInfo.InvariantCulture, AppResources.DeletingWalletConfirmationText, selectedWallet?.Name), AppResources.DeleteButton, AppResources.CancelButton);
                if (confirm2)
                {
                    DeleteWallet(selectedWallet.Path);
                    if (AtomexApp.Account.Wallet.PathToWallet.Equals(selectedWallet.Path))
                        MainViewModel.Locked.Invoke(this, EventArgs.Empty);
                }
            }
        }

        private void OnYoutubeTapped()
        {
            Launcher.OpenAsync(new Uri(YoutubeUrl));
        }

        private void OnTwitterTapped()
        {
            Launcher.OpenAsync(new Uri(TwitterUrl));
        }

        private void OnSupportTapped()
        {
            Launcher.OpenAsync(new Uri(SupportUrl));
        }

        private void OnTelegramTapped()
        {
            Launcher.OpenAsync(new Uri(TelegramUrl));
        }

        private async Task OnDappsDevicesClicked()
        {
            try
            {
                var walletBeaconClient = WalletBeaconHelper.WalletBeaconClient;
                var dappsViewModel = new DappsViewModel(AtomexApp, walletBeaconClient, Navigation);

                Device.BeginInvokeOnMainThread(async () =>
                {
                    await Navigation.PushAsync(new DappsPage(dappsViewModel));
                });

                if (!walletBeaconClient.LoggedIn)
                {
                    await walletBeaconClient.InitAsync().ConfigureAwait(false);

                    walletBeaconClient.Connect();
                }

                if (WalletBeaconHelper.oldDappsViewModel != null)
                {
                    walletBeaconClient.OnDappsListChanged -= WalletBeaconHelper.oldDappsViewModel.OnDappConnectedEventHandler;
                    walletBeaconClient.OnBeaconMessageReceived -= WalletBeaconHelper.oldDappsViewModel.OnBeaconMessageRecievedHandler;
                }

                walletBeaconClient.OnDappsListChanged += dappsViewModel.OnDappConnectedEventHandler;
                walletBeaconClient.OnBeaconMessageReceived += dappsViewModel.OnBeaconMessageRecievedHandler;

                WalletBeaconHelper.oldDappsViewModel = dappsViewModel;
            } catch (Exception ex) {
                Log.Error(ex, "OnDappsDevicesClicked");
            }
        }
    }
}

