using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security;
using System.Threading;
using System.Threading.Tasks;
using atomex.Common;
using atomex.Helpers;
using atomex.Models;
using atomex.Resources;
using atomex.Views.Popup;
using atomex.Views.SettingsOptions;
using Atomex;
using Atomex.Wallet;
using Atomex.Wallet.Abstract;
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

        public UserSettings Settings { get; }

        private IAccount _account;

        private const string LanguageKey = nameof(LanguageKey);

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

        //private string _pathToUserSettings;
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
            set { _language = value; OnPropertyChanged(nameof(Language)); }
        }

        public ObservableCollection<Language> Languages { get; } = new ObservableCollection<Language>()
        {
            new Language { Name = "English", Code = "en" },
            new Language { Name = "Русский", Code = "ru" }
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

        public int PeriodOfInactivityInMin
        {
            get { return Settings.PeriodOfInactivityInMin; }
            set
            {
                if (Settings.PeriodOfInactivityInMin != value)
                {
                    Settings.PeriodOfInactivityInMin = value;
                    _account.UseUserSettings(Settings);
                    //Apply();
                    OnPropertyChanged(nameof(PeriodOfInactivityInMin));
                }
            }
        }

        public bool AutoSignOut
        {
            get { return Settings.AutoSignOut; }
            set
            {
                if (Settings.AutoSignOut != value)
                {
                    Settings.AutoSignOut = value;
                    _account.UseUserSettings(Settings);
                    //Apply();
                    OnPropertyChanged(nameof(AutoSignOut));
                }
            }
        }

        public CultureInfo CurrentCulture => AppResources.Culture ?? Thread.CurrentThread.CurrentUICulture;

        public SettingsViewModel(IAtomexApp app, string walletName)
        {
            AtomexApp = app ?? throw new ArgumentNullException(nameof(AtomexApp));
            _account = app.Account;
            SetUserLanguage();
            Settings = app.Account.UserSettings;
            WalletName = walletName;
            Wallets = WalletInfo.AvailableWallets().ToList();
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

        private Command _enableBiometricCommand;
        public Command EnableBiometricCommand => _enableBiometricCommand ??= new Command<string>(async (value) => await EnableBiometric(value));

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
            _ = Application.Current.MainPage.Navigation.PopPopupAsync();
        }

        async Task CheckBiometricSensor()
        {
            var availability = await CrossFingerprint.Current.GetAvailabilityAsync();
            BiometricSensorAvailibility = availability != FingerprintAvailability.NoSensor;
        }

        //private Command _showLanguagesCommand;
        //public Command ShowLanguagesCommand => _showLanguagesCommand ??= new Command(() => ShowLanguages());

        //private async void ShowLanguages()
        //{
        //    var optionsPage = new LanguagesPage(this, selected =>
        //    {
        //        Language = selected;
        //    });

        //    await Application.Current.MainPage. Navigation.PushAsync(optionsPage);
        //}

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

        private Command _changeLanguageCommand;
        public Command ChangeLanguageCommand => _changeLanguageCommand ??= new Command<Language>((value) => ChangeLanguage(value));

        private void ChangeLanguage(Language language)
        {
            LocalizationResourceManager.Instance.SetCulture(CultureInfo.GetCultureInfo(language.Code));
        }

        //private void Apply()
        //{  
        //    _account.UserSettings.SaveToFile(_pathToUserSettings);
        //}
    }
}

