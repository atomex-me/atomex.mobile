﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Runtime.InteropServices;
using System.Security;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using atomex.Common;
using atomex.CustomElements;
using atomex.Helpers;
using atomex.Models;
using atomex.Resources;
using atomex.Views;
using atomex.Views.SettingsOptions;
using Atomex;
using Atomex.Wallet;
using Plugin.Fingerprint;
using Plugin.Fingerprint.Abstractions;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Serilog;
using Xamarin.Essentials;
using Xamarin.Forms;

namespace atomex.ViewModel
{
    public class SettingsViewModel : BaseViewModel
    {
        private IAtomexApp _app { get; }
        private INavigationService _navigationService { get; set; }
        private MainViewModel _mainViewModel { get; }

        [Reactive] public bool IsLoading { get; set; }
        [Reactive] public string Warning { get; set; }
        [Reactive] public string WalletName { get; set; }
        [Reactive] public List<WalletInfo> Wallets { get; set; }
        [Reactive] public SecureString StoragePassword { get; set; }
        [Reactive] public bool BiometricSensorAvailibility { get; set; }
        [Reactive] public bool UseBiometric { get; set; }

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

                this.RaisePropertyChanged(nameof(Language));
            }
        }

        public ObservableCollection<Language> Languages { get; } = new ObservableCollection<Language>()
        {
            new Language { Name = "English", Code = "en", ShortName = "Eng", IsActive = false },
            new Language { Name = "Français", Code = "fr", ShortName = "Fra", IsActive = false },
            new Language { Name = "Русский", Code = "ru", ShortName = "Rus", IsActive = false },
            new Language { Name = "Türk", Code = "tr", ShortName = "Tur", IsActive = false }
        };

        public string Header => AppResources.EnterPin;
        private const string LanguageKey = nameof(LanguageKey);

        private const string YoutubeUrl = "https://www.youtube.com/c/BakingBad";
        private const string TwitterUrl = "https://twitter.com/atomex_official";
        private const string TelegramUrl = "https://t.me/atomex_official";
        private const string SupportUrl = "mailto:support@atomex.me";

        public CultureInfo CurrentCulture => AppResources.Culture ?? Thread.CurrentThread.CurrentUICulture;

        public SettingsViewModel(
            IAtomexApp app,
            MainViewModel mainViewModel,
            string walletName)
        {
            _app = app ?? throw new ArgumentNullException(nameof(_app));
            _mainViewModel = mainViewModel ?? throw new ArgumentNullException(nameof(MainViewModel));
            StoragePassword = new SecureString();
            WalletName = walletName;
            SetUserLanguage();
            Wallets = WalletInfo.AvailableWallets().ToList();
            _ = CheckBiometricSensor();
            _ = ResetUseBiometricSetting();

            this.WhenAnyValue(vm => vm.UseBiometric)
                .SubscribeInMainThread((flag) => UpdateUseBiometric(flag));
        }

        public void SetNavigationService(INavigationService service)
        {
            _navigationService = service ?? throw new ArgumentNullException(nameof(service));
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

        public async void UpdateUseBiometric(bool value)
        {
            try
            {
                UseBiometric = value;

                await Device.InvokeOnMainThreadAsync(async () =>
                {
                    if (_navigationService == null) return;

                    if (UseBiometric)
                    {
                        var availability = await CrossFingerprint.Current.GetAvailabilityAsync();

                        if (availability == FingerprintAvailability.Available)
                        {
                            _navigationService?.ShowPage(new AuthPage(this), TabNavigation.Settings);
                        }
                        else if (availability == FingerprintAvailability.NoPermission ||
                            availability == FingerprintAvailability.NoFingerprint ||
                            availability == FingerprintAvailability.Denied)
                        {
                            UseBiometric = false;
                            _navigationService?.ShowAlert(string.Empty, AppResources.NeedPermissionsForBiometricLogin, AppResources.AcceptButton);
                        }
                        else
                        {
                            UseBiometric = false;
                            _navigationService?.ShowAlert(AppResources.SorryLabel, AppResources.ImpossibleEnableBiometric, AppResources.AcceptButton);
                        }
                    }
                    else
                    {
                        await SecureStorage.SetAsync(WalletName, string.Empty);
                    }
                });
            }
            catch (Exception e)
            {
                Log.Error(e, "Update biometric settings error");
            }   
        }

        public bool CheckAccountExist()
        {
            if (StoragePassword == null || StoragePassword?.Length == 0)
                return false;

            try
            {
                var wallet = HdWallet.LoadFromFile(_app.Account.Wallet.PathToWallet, StoragePassword);
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

        private async Task EnableBiometric(string pswd)
        {
            IsLoading = true;
            bool accountExist = CheckAccountExist();

            IsLoading = false;
            if (accountExist)
            {
                try
                {
                    string walletName = Path.GetFileName(Path.GetDirectoryName(_app.Account.Wallet.PathToWallet));
                    await SecureStorage.SetAsync(WalletName, pswd);
                }
                catch (Exception ex)
                {
                    _navigationService?.ShowAlert(AppResources.Error, AppResources.NotSupportSecureStorage, AppResources.AcceptButton);
                    Log.Error(ex, AppResources.NotSupportSecureStorage);
                }

                Warning = string.Empty;
                StoragePassword?.Clear();
                _ = ResetUseBiometricSetting();
                _navigationService?.ClosePage(TabNavigation.Settings);
                _navigationService?.DisplaySnackBar(SnackbarMessage.MessageType.Regular, AppResources.BiometricAuthEnabled);
            }
            else
            {
                Warning = AppResources.InvalidPin;

                StoragePassword.Clear();
                this.RaisePropertyChanged(nameof(StoragePassword));

                var tabs = ((CustomTabbedPage)Application.Current.MainPage).Children;

                foreach (NavigationPage tab in tabs)
                {
                    if (tab.RootPage is SettingsPage)
                    {
                        try
                        {
                            Vibration.Vibrate();
                        }
                        catch (FeatureNotSupportedException ex)
                        {
                            Log.Error(ex, "Vibration not supported on device");
                        }

                        await tab.TranslateTo(-15, 0, 50);
                        await tab.TranslateTo(15, 0, 50);
                        await tab.TranslateTo(-10, 0, 50);
                        await tab.TranslateTo(10, 0, 50);
                        await tab.TranslateTo(-5, 0, 50);
                        await tab.TranslateTo(5, 0, 50);
                        tab.TranslationX = 0;
                        break;
                    }
                }
            }
        }

        private async Task CheckBiometricSensor()
        {
            var availability = await CrossFingerprint.Current.GetAvailabilityAsync();
            BiometricSensorAvailibility = availability != FingerprintAvailability.NoSensor;
        }

        private ReactiveCommand<Unit, Unit> _showLanguagesCommand;
        public ReactiveCommand<Unit, Unit> ShowLanguagesCommand => _showLanguagesCommand ??= ReactiveCommand.Create(() =>
            _navigationService?.ShowPage(new LanguagesPage(this), TabNavigation.Settings));

        private ReactiveCommand<Language, Unit> _changeLanguageCommand;
        public ReactiveCommand<Language, Unit> ChangeLanguageCommand => _changeLanguageCommand ??= ReactiveCommand.Create<Language>((value) =>
            {
                Language = value;
                _navigationService?.ClosePage(TabNavigation.Settings);
            });

        private void SetUserLanguage()
        {
            try
            {
                Language = Languages.Where(l => l.Code == Preferences.Get(LanguageKey, CurrentCulture.TwoLetterISOLanguageName)).Single();
            }
            catch (Exception e)
            {
                Log.Error(e, "Set user language error");
                Language = Languages.Where(l => l.Code == "en").Single();
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

        private ReactiveCommand<string, Unit> _addCharCommand;
        public ReactiveCommand<string, Unit> AddCharCommand => _addCharCommand ??= ReactiveCommand.Create<string>((value) =>
            {
                if (StoragePassword?.Length < 4)
                {
                    Warning = string.Empty;

                    foreach (char c in value)
                    {
                        StoragePassword.AppendChar(c);
                    }

                    this.RaisePropertyChanged(nameof(StoragePassword));
                    if (StoragePassword?.Length == 4)
                    {
                        _ = EnableBiometric(SecureStringToString(StoragePassword));
                    }
                }
            });

        private ReactiveCommand<Unit, Unit> _deleteCharCommand;
        public ReactiveCommand<Unit, Unit> DeleteCharCommand => _deleteCharCommand ??= ReactiveCommand.Create(() =>
            {
                if (StoragePassword?.Length != 0)
                {
                    StoragePassword.RemoveAt(StoragePassword.Length - 1);
                    this.RaisePropertyChanged(nameof(StoragePassword));
                }
            });

        private ReactiveCommand<Unit, Unit> _cancelCommand;
        public ReactiveCommand<Unit, Unit> CancelCommand => _cancelCommand ??= ReactiveCommand.Create(() => _navigationService?.ClosePage(TabNavigation.Settings));

        private String SecureStringToString(SecureString value)
        {
            IntPtr valuePtr = IntPtr.Zero;
            try
            {
                valuePtr = Marshal.SecureStringToGlobalAllocUnicode(value);
                return Marshal.PtrToStringUni(valuePtr);
            }
            finally
            {
                Marshal.ZeroFreeGlobalAllocUnicode(valuePtr);
            }
        }

        private ICommand _backCommand;
        public ICommand BackCommand => _backCommand ??= ReactiveCommand.Create(() =>
            {
                Warning = string.Empty;
                StoragePassword?.Clear();
                _ = ResetUseBiometricSetting();
            });

        private ReactiveCommand<Unit, Unit> _youtubeCommand;
        public ReactiveCommand<Unit, Unit> YoutubeCommand => _youtubeCommand ??= ReactiveCommand.CreateFromTask(() =>
            Launcher.OpenAsync(new Uri(YoutubeUrl)));

        private ReactiveCommand<Unit, Unit> _telegramCommand;
        public ReactiveCommand<Unit, Unit> TelegramCommand => _telegramCommand ??= ReactiveCommand.CreateFromTask(() =>
            Launcher.OpenAsync(new Uri(TelegramUrl)));

        private ReactiveCommand<Unit, Unit> _twitterCommand;
        public ReactiveCommand<Unit, Unit> TwitterCommand => _twitterCommand ??= ReactiveCommand.CreateFromTask(() =>
            Launcher.OpenAsync(new Uri(TwitterUrl)));

        private ReactiveCommand<Unit, Unit> _supportCommand;
        public ReactiveCommand<Unit, Unit> SupportCommand => _supportCommand ??= ReactiveCommand.CreateFromTask(() =>
            Launcher.OpenAsync(new Uri(SupportUrl)));

        private ReactiveCommand<Unit, Unit> _signOutCommand;
        public ReactiveCommand<Unit, Unit> SignOutCommand => _signOutCommand ??= ReactiveCommand.CreateFromTask(async () =>
            {
                var res = await _navigationService?.ShowAlert(AppResources.SignOut, AppResources.AreYouSure, AppResources.AcceptButton, AppResources.CancelButton);
                if (res)
                    _mainViewModel.Locked.Invoke(this, EventArgs.Empty);
            });

        private ReactiveCommand<string, Unit> _deleteWalletCommand;
        public ReactiveCommand<string, Unit> DeleteWalletCommand => _deleteWalletCommand ??= ReactiveCommand.Create<string>(async (name) =>
            {
                WalletInfo selectedWallet = Wallets.Where(w => w.Name == name).Single();

                var confirm = await _navigationService?.ShowAlert(AppResources.DeletingWallet, AppResources.DeletingWalletText, AppResources.UnderstandButton, AppResources.CancelButton);
                if (confirm)
                {
                    var confirm2 = await _navigationService?.ShowAlert(AppResources.DeletingWallet, string.Format(CultureInfo.InvariantCulture, AppResources.DeletingWalletConfirmationText, selectedWallet?.Name), AppResources.DeleteButton, AppResources.CancelButton);
                    if (confirm2)
                    {
                        DeleteWallet(selectedWallet.Path);
                        if (_app.Account.Wallet.PathToWallet.Equals(selectedWallet.Path))
                            _mainViewModel.Locked.Invoke(this, EventArgs.Empty);
                    }
                }
            });
    }
}

