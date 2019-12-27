using System;
using atomex.Models;

namespace atomex.ViewModel
{
    public class SettingsViewModel : BaseViewModel
    {
        private bool showWarnings;
        private bool autoSignOut;
        private int periodOfInactive;
        private int balanceUpdateInterval;

        public int PeriodOfInactive
        {
            get { return periodOfInactive; }
            set
            {
                if (periodOfInactive != value)
                {
                    periodOfInactive = value;
                    OnPropertyChanged(nameof(PeriodOfInactive));
                }
            }
        }

        public bool ShowWarnings
        {
            get { return showWarnings; }
            set
            {
                if (showWarnings != value)
                {
                    showWarnings = value;
                    OnPropertyChanged(nameof(ShowWarnings));
                }
            }
        }

        public bool AutoSignOut
        {
            get { return autoSignOut; }
            set
            {
                if (autoSignOut != value)
                {
                    autoSignOut = value;
                    OnPropertyChanged(nameof(AutoSignOut));
                }
            }
        }

        public int BalanceUpdateInterval
        {
            get { return balanceUpdateInterval; }
            set
            {
                if (balanceUpdateInterval != value)
                {
                    balanceUpdateInterval = value;
                    OnPropertyChanged(nameof(BalanceUpdateInterval));
                }
            }
        }

        public SettingsViewModel()
        {
            ShowWarnings = SettingsData.ShowWarnings;
            AutoSignOut = SettingsData.AutoSignOut;
            PeriodOfInactive = SettingsData.PeriodOfInactive;
            BalanceUpdateInterval = SettingsData.BalanceUpdateInterval;
        }
    }
}

