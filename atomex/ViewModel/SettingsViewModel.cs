using System;
using System.Security;
using System.Threading.Tasks;
using atomex.Resources;
using atomex.Views.Popup;
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

        //private string _pathToUserSettings;

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

        public SettingsViewModel(IAtomexApp app)
        {
            AtomexApp = app ?? throw new ArgumentNullException(nameof(AtomexApp));
            _account = app.Account;
            Settings = app.Account.UserSettings;
            _ = GetUseBiometricSetting();
        }

        private async Task GetUseBiometricSetting()
        {
            try
            {
                string value = await SecureStorage.GetAsync("UseBiometric");
                bool.TryParse(value, out var useBiometric);
                UseBiometric = useBiometric;
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
                        await Application.Current.MainPage.Navigation.PushPopupAsync(new BiometricSettingPopup(this));
                    }
                    if (availability == FingerprintAvailability.NoPermission ||
                        availability == FingerprintAvailability.NoFingerprint)
                    {
                        _useBiometric = false;
                        await Application.Current.MainPage.DisplayAlert("", AppResources.NeedPermissionsForBiometricLogin, AppResources.AcceptButton);
                    }
                }
                else
                {
                    try
                    {
                        await SecureStorage.SetAsync("UseBiometric", false.ToString());
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

        //private void Apply()
        //{  
        //    _account.UserSettings.SaveToFile(_pathToUserSettings);
        //}
    }
}

