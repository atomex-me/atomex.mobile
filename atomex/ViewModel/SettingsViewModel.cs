using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using atomex.Common;
using atomex.Helpers;
using atomex.Models;
using atomex.Resources;
using atomex.Views.Popup;
using atomex.Views.SettingsOptions;
using Atomex;
using Atomex.Wallet;
using Plugin.Fingerprint;
using Plugin.Fingerprint.Abstractions;
using Rg.Plugins.Popup.Extensions;
using Serilog;
using Xamarin.Essentials;
using Xamarin.Forms;

namespace atomex.ViewModel
{
    public class SettingsViewModel : BaseViewModel
    {
        public IAtomexApp AtomexApp { get; private set; }

        public INavigation Navigation { get; set; }

        public MainViewModel MainViewModel { get; }

        public UserSettings Settings { get; }

        private const string LanguageKey = nameof(LanguageKey);

        private string YoutubeUrl = "https://www.youtube.com/c/BakingBad";

        private string TwitterUrl = "https://twitter.com/atomex_official";

        private string TelegramUrl = "https://t.me/atomex_official";

        private string SupportUrl = "mailto:support@atomex.me";

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

        private SecureString _password;

        public SecureString Password
        {
            get => _password;
            set { _password = value; OnPropertyChanged(nameof(Password)); }
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
            Settings = app.Account.UserSettings;
            WalletName = walletName;
            Wallets = WalletInfo.AvailableWallets().ToList();
            MainViewModel = mainViewModel;
            _ = CheckBiometricSensor();
            _ = ResetUseBiometricSetting();
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
                        if (Application.Current.MainPage is MainPage)
                            await Application.Current.MainPage.Navigation.PushPopupAsync(new BiometricSettingPopup(this));
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

        public bool CheckAccountExist()
        {
            if (Password == null || Password.Length == 0)
                return false;

            try
            {
                var wallet = HdWallet.LoadFromFile(AtomexApp.Account.Wallet.PathToWallet, Password);
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

        private void ClearWarning()
        {
            Warning = string.Empty;
        }

        private ICommand _pswdChangedCommand;
        public ICommand PswdChangedCommand => _pswdChangedCommand ??= new Command<string>((value) => SetPassword(value));

        private ICommand _enableBiometricCommand;
        public ICommand EnableBiometricCommand => _enableBiometricCommand ??= new Command<string>(async (value) => await EnableBiometric(value));

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
                    ClosePopup();
                }
                catch (Exception ex)
                {
                    ClosePopup();
                    await Application.Current.MainPage.DisplayAlert(AppResources.Error, AppResources.NotSupportSecureStorage, AppResources.AcceptButton);
                    Log.Error(ex, AppResources.NotSupportSecureStorage);
                }
            }
            else
            {
                Warning = AppResources.InvalidPassword;
            }
        }

        private Command _closePopupCommand;
        public Command ClosePopupCommand => _closePopupCommand ??= new Command(() => ClosePopup());

        public void ClosePopup()
        {
            SetPassword(string.Empty);
            Warning = string.Empty;
            _ = ResetUseBiometricSetting();
            _ = Navigation.PopPopupAsync();
        }

        async Task CheckBiometricSensor()
        {
            var availability = await CrossFingerprint.Current.GetAvailabilityAsync();
            BiometricSensorAvailibility = availability != FingerprintAvailability.NoSensor;
        }

        private ICommand _clearWarningCommand;
        public ICommand ClearWarningCommand => _clearWarningCommand ??= new Command(() => ClearWarning());

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
                Language = Languages.Where(l => l.Code == Preferences.Get(LanguageKey, CurrentCulture.TwoLetterISOLanguageName)).Single(); ;
            }
            catch(Exception e)
            {
                Log.Error(e, "Set user language error");
                Language = Languages.Where(l => l.Code == "en").Single(); ;
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
    }
}

