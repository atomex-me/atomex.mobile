using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Runtime.InteropServices;
using System.Security;
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

namespace atomex.ViewModels
{
    public class SettingsViewModel : BaseViewModel
    {
        private readonly IAtomexApp _app;
        private INavigationService NavigationService { get; set; }
        private MainViewModel _mainViewModel;

        [Reactive] public bool IsLoading { get; set; }
        [Reactive] public string Warning { get; set; }
        [Reactive] public List<WalletInfo> Wallets { get; set; }
        [Reactive] public SecureString StoragePassword { get; set; }
        [Reactive] public bool BiometricSensorAvailibility { get; set; }
        [Reactive] public bool UseBiometric { get; set; }
        private string _walletName;

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
            new Language {Name = "English", Code = "en", ShortName = "Eng", IsActive = false},
            new Language {Name = "Français", Code = "fr", ShortName = "Fra", IsActive = false},
            new Language {Name = "Русский", Code = "ru", ShortName = "Rus", IsActive = false},
            new Language {Name = "Türk", Code = "tr", ShortName = "Tur", IsActive = false}
        };

        public string Header => AppResources.EnterPin;
        private const string LanguageKey = nameof(LanguageKey);


        public SettingsViewModel(
            IAtomexApp app,
            MainViewModel mainViewModel)
        {
            _app = app ?? throw new ArgumentNullException(nameof(app));
            _mainViewModel = mainViewModel ?? throw new ArgumentNullException(nameof(mainViewModel));
            _walletName = GetWalletName();
            StoragePassword = new SecureString();
            Wallets = WalletInfo.AvailableWallets().ToList();
            InitUserLanguage();
            _ = CheckBiometricSensor();
            _ = ResetUseBiometricSetting();

            this.WhenAnyValue(vm => vm.UseBiometric)
                .WhereNotNull()
                .SubscribeInMainThread((flag) => UpdateUseBiometric(flag));
        }

        public void SetNavigationService(INavigationService service)
        {
            NavigationService = service ?? throw new ArgumentNullException(nameof(service));
        }

        private void InitUserLanguage()
        {
            try
            {
                var language = Preferences.Get(LanguageKey, "en");
                Language = Languages.Single(l =>
                    l.Code == language);
            }
            catch (Exception e)
            {
                Language = Languages.Single(l => l.Code == "en");
                Log.Error(e, "Init user language error");
            }
        }

        private async Task ResetUseBiometricSetting()
        {
            try
            {
                var value = await SecureStorage.GetAsync(_walletName);
                UseBiometric = !string.IsNullOrEmpty(value);
            }
            catch (Exception ex)
            {
                UseBiometric = false;
                Log.Error(ex, "Device doesn't support secure storage on device");
            }
        }

        private async void UpdateUseBiometric(bool value)
        {
            try
            {
                if (NavigationService == null) return;

                UseBiometric = value;

                await Device.InvokeOnMainThreadAsync(async () =>
                {
                    if (UseBiometric)
                    {
                        var availability = await CrossFingerprint.Current.GetAvailabilityAsync();

                        if (availability == FingerprintAvailability.Available)
                        {
                            NavigationService?.ShowPage(new AuthPage(this), TabNavigation.Settings);
                        }
                        else if (availability == FingerprintAvailability.NoPermission ||
                                 availability == FingerprintAvailability.NoFingerprint ||
                                 availability == FingerprintAvailability.Denied)
                        {
                            UseBiometric = false;
                            NavigationService?.ShowAlert(
                                title: string.Empty,
                                text: AppResources.NeedPermissionsForBiometricLogin,
                                cancel: AppResources.AcceptButton);
                        }
                        else
                        {
                            UseBiometric = false;
                            NavigationService?.ShowAlert(
                                title: AppResources.SorryLabel,
                                text: AppResources.ImpossibleEnableBiometric,
                                cancel: AppResources.AcceptButton);
                        }
                    }
                    else
                    {
                        await SecureStorage.SetAsync(_walletName, string.Empty);
                    }
                });
            }
            catch (Exception e)
            {
                Log.Error(e, "Update biometric settings error");
            }
        }

        private bool CheckAccountExist()
        {
            if (StoragePassword == null || StoragePassword?.Length == 0)
                return false;

            try
            {
                var wallet = HdWallet.LoadFromFile(
                    pathToWallet: _app.Account.Wallet.PathToWallet,
                    password: StoragePassword);
                return true;
            }
            catch (Exception e)
            {
                Log.Error(e, "Enable biometric login error");
                return false;
            }
        }

        private void DeleteWallet(string path)
        {
            try
            {
                var directoryPath = Path.GetDirectoryName(path);
                if (!Directory.Exists(directoryPath)) return;

                Directory.Delete(directoryPath, true);
                Wallets = WalletInfo
                    .AvailableWallets()
                    .ToList();
            }
            catch (Exception e)
            {
                Log.Error(e, "Delete wallet error");
            }
        }

        private async Task EnableBiometric(string pswd)
        {
            try
            {
                IsLoading = true;
                var accountExist = CheckAccountExist();

                IsLoading = false;
                if (accountExist)
                {
                    try
                    {
                        await SecureStorage.SetAsync(_walletName, pswd);
                    }
                    catch (Exception ex)
                    {
                        NavigationService?.ShowAlert(
                            title: AppResources.Error,
                            text: AppResources.NotSupportSecureStorage,
                            cancel: AppResources.AcceptButton);
                        Log.Error(ex, "Device not support secure storage");
                    }

                    await Device.InvokeOnMainThreadAsync(() =>
                    {
                        Warning = string.Empty;
                        StoragePassword?.Clear();
                        _ = ResetUseBiometricSetting();
                        NavigationService?.ClosePage(TabNavigation.Settings);
                        NavigationService?.DisplaySnackBar(
                            messageType: SnackbarMessage.MessageType.Regular,
                            text: AppResources.BiometricAuthEnabled);
                    });
                }
                else
                {
                    Warning = AppResources.InvalidPin;

                    StoragePassword.Clear();
                    this.RaisePropertyChanged(nameof(StoragePassword));

                    var tabs = ((CustomTabbedPage) Application.Current.MainPage).Children;

                    foreach (var page in tabs)
                    {
                        var tab = (NavigationPage) page;
                        if (tab.RootPage is not SettingsPage) continue;
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
            catch (Exception e)
            {
                Log.Error(e, "Enable biometric error");
            }
        }

        private async Task CheckBiometricSensor()
        {
            try
            {
                var availability = await CrossFingerprint.Current.GetAvailabilityAsync();
                BiometricSensorAvailibility = availability != FingerprintAvailability.NoSensor;
            }
            catch (Exception e)
            {
                Log.Error(e, "Check biometric sensor error");
            }
        }

        private ReactiveCommand<Unit, Unit> _showLanguagesCommand;

        public ReactiveCommand<Unit, Unit> ShowLanguagesCommand => _showLanguagesCommand ??= ReactiveCommand.Create(
            () =>
                NavigationService?.ShowPage(new LanguagesPage(this), TabNavigation.Settings));

        private ReactiveCommand<Language, Unit> _changeLanguageCommand;

        public ReactiveCommand<Language, Unit> ChangeLanguageCommand => _changeLanguageCommand ??=
            ReactiveCommand.Create<Language>((value) =>
            {
                Language = value;
                NavigationService?.ClosePage(TabNavigation.Settings);
            });

        private void SetCulture(Language language)
        {
            try
            {
                LocalizationResourceManager.Instance.SetCulture(CultureInfo.GetCultureInfo(language?.Code ?? "en"));
            }
            catch (Exception e)
            {
                Log.Error(e, "Set culture error");
            }
        }

        private ReactiveCommand<string, Unit> _addCharCommand;

        public ReactiveCommand<string, Unit> AddCharCommand => _addCharCommand ??= ReactiveCommand.Create<string>(
            (value) =>
            {
                try
                {
                    if (!(StoragePassword?.Length < 4)) return;
                    Warning = string.Empty;

                    foreach (var c in value)
                        StoragePassword.AppendChar(c);
                    this.RaisePropertyChanged(nameof(StoragePassword));
                
                    if (StoragePassword?.Length == 4)
                        _ = EnableBiometric(SecureStringToString(StoragePassword));
                }
                catch (Exception e)
                {
                    Log.Error(e, "Append password char error");
                }
            });

        private ReactiveCommand<Unit, Unit> _deleteCharCommand;

        public ReactiveCommand<Unit, Unit> DeleteCharCommand => _deleteCharCommand ??= ReactiveCommand.Create(() =>
        {
            try
            {
                if (StoragePassword?.Length == 0) return;
            
                StoragePassword!.RemoveAt(StoragePassword.Length - 1);
                this.RaisePropertyChanged(nameof(StoragePassword));
            }
            catch (Exception e)
            {
                Log.Error(e, "Remove password char error");
            }
        });

        private ReactiveCommand<Unit, Unit> _cancelCommand;

        public ReactiveCommand<Unit, Unit> CancelCommand => _cancelCommand ??=
            ReactiveCommand.Create(() => NavigationService?.ClosePage(TabNavigation.Settings));

        private String SecureStringToString(SecureString value)
        {
            var valuePtr = IntPtr.Zero;
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
            Launcher.OpenAsync(new Uri(Constants.YoutubeUrl)));

        private ReactiveCommand<Unit, Unit> _telegramCommand;

        public ReactiveCommand<Unit, Unit> TelegramCommand => _telegramCommand ??= ReactiveCommand.CreateFromTask(() =>
            Launcher.OpenAsync(new Uri(Constants.TelegramUrl)));

        private ReactiveCommand<Unit, Unit> _twitterCommand;

        public ReactiveCommand<Unit, Unit> TwitterCommand => _twitterCommand ??= ReactiveCommand.CreateFromTask(() =>
            Launcher.OpenAsync(new Uri(Constants.TwitterUrl)));

        private ReactiveCommand<Unit, Unit> _supportCommand;

        public ReactiveCommand<Unit, Unit> SupportCommand => _supportCommand ??= ReactiveCommand.CreateFromTask(() =>
            Launcher.OpenAsync(new Uri(Constants.SupportUrl)));

        private ReactiveCommand<Unit, Unit> _signOutCommand;

        public ReactiveCommand<Unit, Unit> SignOutCommand => _signOutCommand ??= ReactiveCommand.CreateFromTask(
            async () =>
            {
                try
                {
                    var res = await NavigationService.ShowAlert(
                        title: AppResources.SignOut,
                        text: AppResources.AreYouSure,
                        accept: AppResources.AcceptButton,
                        cancel: AppResources.CancelButton);
                    if (res)
                        _mainViewModel?.Locked?.Invoke(this, EventArgs.Empty);
                }
                catch (Exception e)
                {
                    Log.Error(e, "Sign out error");
                }
            });

        private ReactiveCommand<string, Unit> _deleteWalletCommand;

        public ReactiveCommand<string, Unit> DeleteWalletCommand => _deleteWalletCommand ??=
            ReactiveCommand.CreateFromTask<string>(async (name) =>
            {
                try
                {
                    var selectedWallet = Wallets
                        .Single(w => w.Name == name);

                    var confirm = await NavigationService.ShowAlert(
                        title: AppResources.DeletingWallet,
                        text: AppResources.DeletingWalletText,
                        accept: AppResources.UnderstandButton,
                        cancel: AppResources.CancelButton);
                    if (confirm)
                    {
                        var acceptAlert = await NavigationService.ShowAlert(
                            title: AppResources.DeletingWallet,
                            text: string.Format(CultureInfo.InvariantCulture,
                                AppResources.DeletingWalletConfirmationText,
                                selectedWallet?.Name),
                            accept: AppResources.DeleteButton,
                            cancel: AppResources.CancelButton);
                        if (acceptAlert)
                        {
                            DeleteWallet(selectedWallet?.Path);
                            if (_app.Account.Wallet.PathToWallet.Equals(selectedWallet.Path))
                                _mainViewModel?.Locked?.Invoke(this, EventArgs.Empty);
                        }
                    }
                }
                catch (Exception e)
                {
                    Log.Error(e, "Delete wallet error");
                }
            });

        private string GetWalletName()
        {
            return new DirectoryInfo(_app.Account.Wallet.PathToWallet).Parent!.Name;
        }
    }
}