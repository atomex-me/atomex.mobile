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
using atomex.Views.CreateNewWallet;
using atomex.Views.SettingsOptions;
using Atomex;
using atomex.Views;
using Plugin.LatestVersion;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Serilog;
using Xamarin.Essentials;
using Xamarin.Forms;

namespace atomex.ViewModels
{
    public class StartViewModel : BaseViewModel
    {
        private IAtomexApp _app { get; set; }
        private INavigationService _navigationService { get; set; }

        [Reactive] public bool HasWallets { get; set; }
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

        private const string LanguageKey = nameof(LanguageKey);

        public ObservableCollection<Language> Languages { get; } = new ObservableCollection<Language>()
        {
            new Language {Name = "English", Code = "en", ShortName = "Eng", IsActive = false},
            new Language {Name = "Français", Code = "fr", ShortName = "Fra", IsActive = false},
            new Language {Name = "Русский", Code = "ru", ShortName = "Rus", IsActive = false},
            new Language {Name = "Türk", Code = "tr", ShortName = "Tur", IsActive = false}
        };

        private void SetCulture(Language language)
        {
            try
            {
                LocalizationResourceManager.Instance
                    .SetCulture(CultureInfo.GetCultureInfo(language?.Code ?? "en"));
            }
            catch(Exception e)
            {
                Log.Error(e, "Set culture error");
            }
        }

        public CultureInfo CurrentCulture => AppResources.Culture ?? Thread.CurrentThread.CurrentUICulture;
        

        public StartViewModel(IAtomexApp app)
        {
            _app = app ?? throw new ArgumentNullException(nameof(app));
            HasWallets = WalletInfo.AvailableWallets().Any();
            InitUserLanguage();
            _ = CheckLatestVersion();
        }

        public void SetNavigationService(INavigationService service)
        {
            _navigationService = service ?? throw new ArgumentNullException(nameof(service));
        }

        private void InitUserLanguage()
        {
            try
            {
                string language = Preferences.Get(LanguageKey, "en");
                Language = Languages.Single(l =>
                    l.Code == language);
            }
            catch (Exception e)
            {
                Language = Languages.Single(l => l.Code == "en");
                Log.Error(e, "Init user language error");
            }
        }

        private ICommand _createNewWalletCommand;

        public ICommand CreateNewWalletCommand => _createNewWalletCommand ??= ReactiveCommand.Create(() =>
        {
            CreateNewWalletViewModel createNewWalletViewModel = new CreateNewWalletViewModel(_app, _navigationService);
            createNewWalletViewModel.CurrentAction = CreateNewWalletViewModel.Action.Create;
            _navigationService?.ShowPage(new WalletTypePage(createNewWalletViewModel));
        });

        private ICommand _restoreWalletCommand;

        public ICommand RestoreWalletCommand => _restoreWalletCommand ??= new Command(() =>
        {
            CreateNewWalletViewModel createNewWalletViewModel = new CreateNewWalletViewModel(_app, _navigationService);
            createNewWalletViewModel.CurrentAction = CreateNewWalletViewModel.Action.Restore;
            _navigationService?.ShowPage(new WalletTypePage(createNewWalletViewModel));
        });

        private ICommand _showMyWalletsCommand;

        public ICommand ShowMyWalletsCommand => _showMyWalletsCommand ??= ReactiveCommand.Create(() =>
            _navigationService?.ShowPage(new MyWalletsPage(new MyWalletsViewModel(_app, _navigationService))));

        private ICommand _showLanguagesCommand;

        public ICommand ShowLanguagesCommand => _showLanguagesCommand ??=
            ReactiveCommand.Create(() => _navigationService?.ShowPage(new LanguagesPage(this)));

        private ICommand _changeLanguageCommand;

        public ICommand ChangeLanguageCommand => _changeLanguageCommand ??= ReactiveCommand.Create<Language>((value) =>
        {
            Language = value;
            _navigationService?.ClosePage();
        });

        private async Task CheckLatestVersion()
        {
            try
            {
                var isLatest = await CrossLatestVersion.Current.IsUsingLatestVersion();

                if (!isLatest)
                {
                    var update = await _navigationService?.ShowAlert(AppResources.UpdateAvailable, AppResources.UpdateApp,
                        AppResources.Yes, AppResources.No);

                    if (update)
                        await CrossLatestVersion.Current.OpenAppInStore();
                }
            }
            catch (Exception e)
            {
                Log.Error(e, "Check latest app version error");
            }
        }
    }
}