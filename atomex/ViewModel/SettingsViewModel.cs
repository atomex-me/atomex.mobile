using Atomex.Wallet;
using Atomex.Wallet.Abstract;

namespace atomex.ViewModel
{
    public class SettingsViewModel : BaseViewModel
    {

        public UserSettings Settings { get; }

        private IAccount Account;

        public int PeriodOfInactivityInMin
        {
            get { return Settings.PeriodOfInactivityInMin; }
            set
            {
                if (Settings.PeriodOfInactivityInMin != value)
                {
                    Settings.PeriodOfInactivityInMin = value;
                    Account.UseUserSettings(Settings);
                    OnPropertyChanged(nameof(PeriodOfInactivityInMin));
                }
            }
        }

        public bool ShowActiveSwapWarning
        {
            get { return Settings.ShowActiveSwapWarning; }
            set
            {
                if (Settings.ShowActiveSwapWarning != value)
                {
                    Settings.ShowActiveSwapWarning = value;
                    Account.UseUserSettings(Settings);
                    OnPropertyChanged(nameof(ShowActiveSwapWarning));
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
                    Account.UseUserSettings(Settings);
                    OnPropertyChanged(nameof(AutoSignOut));
                }
            }
        }

        public int BalanceUpdateIntervalInSec
        {
            get { return Settings.BalanceUpdateIntervalInSec; }
            set
            {
                if (Settings.BalanceUpdateIntervalInSec != value)
                {
                    Settings.BalanceUpdateIntervalInSec = value;
                    Account.UseUserSettings(Settings);
                    OnPropertyChanged(nameof(BalanceUpdateIntervalInSec));
                }
            }
        }

        public SettingsViewModel(IAccount account)
        {
            Account = account;
            Settings = account.UserSettings;
        }
    }
}

