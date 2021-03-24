using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Threading;
using atomex.Common;
using atomex.Helpers;
using atomex.Models;
using atomex.Resources;
using atomex.ViewModel;
using Atomex;
using Serilog;
using Xamarin.Essentials;

namespace atomex
{
    public class StartViewModel : BaseViewModel
    {
        public IAtomexApp AtomexApp { get; private set; }

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

                ChangeLanguage(_language);

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

        private void ChangeLanguage(Language language)
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
    }
}

