using System;
using System.IO;
using Atomex;
using Atomex.Wallet;
using Atomex.Wallet.Abstract;

namespace atomex.ViewModel
{
    public class SettingsViewModel : BaseViewModel
    {
        public IAtomexApp AtomexApp { get; private set; }

        public UserSettings Settings { get; }

        private IAccount _account;

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

        private string _pathToUserSettings;

        public SettingsViewModel(IAtomexApp app)
        {
            AtomexApp = app ?? throw new ArgumentNullException(nameof(AtomexApp));
            _account = app.Account;
            Settings = app.Account.UserSettings;
        }

        //private void Apply()
        //{  
        //    _account.UserSettings.SaveToFile(_pathToUserSettings);
        //}
    }
}

