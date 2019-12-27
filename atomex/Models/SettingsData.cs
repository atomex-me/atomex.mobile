namespace atomex.Models
{
    public static class SettingsData
    {
        public static int PeriodOfInactive { get; set; }
        public static bool ShowWarnings { get; set; }
        public static bool AutoSignOut { get; set; }
        public static int BalanceUpdateInterval { get; set; }

        static SettingsData() {
            PeriodOfInactive = 10;
            ShowWarnings = false;
            AutoSignOut = true;
            BalanceUpdateInterval = 60;
        }
    }
}
