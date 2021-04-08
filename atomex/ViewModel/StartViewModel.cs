using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using atomex.Common;
using atomex.Helpers;
using atomex.Models;
using atomex.Resources;
using atomex.ViewModel;
using atomex.Views.CreateNewWallet;
using atomex.Views.SettingsOptions;
using Atomex;
using Serilog;
using Xamarin.Essentials;
using Xamarin.Forms;

namespace atomex
{
    public class StartViewModel : BaseViewModel
    {
        public IAtomexApp AtomexApp { get; private set; }

        public INavigation Navigation { get; set; }

        private const string LanguageKey = nameof(LanguageKey);

        private bool _hasWallets;
        public bool HasWallets
        {
            get => _hasWallets;
            private set { _hasWallets = value; OnPropertyChanged(nameof(HasWallets)); }
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
            new Language { Name = "Русский", Code = "ru", ShortName = "Rus", IsActive = false },
            new Language { Name = "Türk", Code = "tr", ShortName = "Tur", IsActive = false }
        };

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

        public CultureInfo CurrentCulture => AppResources.Culture ?? Thread.CurrentThread.CurrentUICulture;

        public StartViewModel(IAtomexApp app)
        {
            AtomexApp = app ?? throw new ArgumentNullException(nameof(AtomexApp));
            HasWallets = WalletInfo.AvailableWallets().Count() > 0;
            SetUserLanguage();
        }

        private void SetUserLanguage()
        {
            try
            {
                string language = Preferences.Get(LanguageKey, CurrentCulture.TwoLetterISOLanguageName);
                Language = Languages.Where(l => l.Code == Preferences.Get(LanguageKey, CurrentCulture.TwoLetterISOLanguageName)).Single();
                LocalizationResourceManager.Instance.SetCulture(CultureInfo.GetCultureInfo(language));
            }
            catch (Exception e)
            {
                LocalizationResourceManager.Instance.SetCulture(CultureInfo.GetCultureInfo("en"));
                Language = Languages.Where(l => l.Code == "en").Single();
                Log.Error(e, "Not found user language error");
            }
        }

        private ICommand _createNewWalletCommand;
        public ICommand CreateNewWalletCommand => _createNewWalletCommand ??= new Command(async () => await CreateNewWallet());

        private ICommand _restoreWalletCommand;
        public ICommand RestoreWalletCommand => _restoreWalletCommand ??= new Command(async () => await RestoreWallet());

        private ICommand _showMyWalletsCommand;
        public ICommand ShowMyWalletsCommand => _showMyWalletsCommand ??= new Command(async () => await ShowMyWallets());

        private ICommand _showLanguagesCommand;
        public ICommand ShowLanguagesCommand => _showLanguagesCommand ??= new Command(async () => await ShowLanguages());

        private ICommand _changeLanguageCommand;
        public ICommand ChangeLanguageCommand => _changeLanguageCommand ??= new Command<Language>(async (value) => await ChangeLanguage(value));

        private async Task CreateNewWallet()
        {
            CreateNewWalletViewModel createNewWalletViewModel = new CreateNewWalletViewModel(AtomexApp, Navigation);
            createNewWalletViewModel.CurrentAction = CreateNewWalletViewModel.Action.Create;
            await Navigation.PushAsync(new WalletTypePage(createNewWalletViewModel));
        }

        private async Task RestoreWallet()
        {
            CreateNewWalletViewModel createNewWalletViewModel = new CreateNewWalletViewModel(AtomexApp, Navigation);
            createNewWalletViewModel.CurrentAction = CreateNewWalletViewModel.Action.Restore;
            await Navigation.PushAsync(new WalletTypePage(createNewWalletViewModel));
        }

        private async Task ShowMyWallets()
        {
            await Navigation.PushAsync(new MyWalletsPage(new MyWalletsViewModel(AtomexApp, Navigation)));
        }

        private async Task ShowLanguages()
        {
            await Navigation.PushAsync(new LanguagesPage(this));
        }

        private async Task ChangeLanguage(Language value)
        {
            Language = value;
            await Navigation.PopAsync();
        }
    }
}

