using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Security;
using System.Security.Cryptography;
using System.Threading.Tasks;
using System.Windows.Input;
using atomex.Common;
using atomex.Models;
using atomex.Resources;
using atomex.ViewModel;
using atomex.Views;
using atomex.Views.CreateNewWallet;
using Atomex;
using Atomex.Common;
using Atomex.Cryptography;
using Atomex.Wallet;
using NBitcoin;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Serilog;
using Xamarin.Essentials;
using Xamarin.Forms;
using Network = Atomex.Core.Network;

namespace atomex
{
    public class CreateNewWalletViewModel : BaseViewModel
    {
        private IAtomexApp _app { get; set; }
        private INavigationService _navigationService { get; set; }

        public enum Action
        {
            Create,
            Restore
        }

        public enum PasswordType
        {
            DerivedPassword,
            DerivedPasswordConfirmation,
            StoragePassword,
            StoragePasswordConfirmation
        }

        private Action _currentAction;
        public Action CurrentAction
        {
            get => _currentAction;
            set
            {
                _currentAction = value;
                Title = _currentAction == Action.Restore
                    ? AppResources.RestoreWalletPageTitle
                    : AppResources.CreateNewWalletPageTitle;

                this.RaisePropertyChanged(nameof(Title));
                this.RaisePropertyChanged(nameof(CurrentAction));
            }
        }

        [Reactive] public string Header { get; set; }
        [Reactive] public string Title { get; set; }
        [Reactive] public string Warning { get; set; }
        [Reactive] public bool IsLoading { get; set; }

        public List<Network> Networks { get; } = new List<Network>
        {
            Network.MainNet,
            Network.TestNet
        };

        public List<CustomWordlist> Languages { get; } = new List<CustomWordlist>
        {
            new CustomWordlist { Name = "English", Wordlist = Wordlist.English },
            new CustomWordlist { Name = "French", Wordlist = Wordlist.French },
            new CustomWordlist { Name = "Japanese", Wordlist = Wordlist.Japanese },
            new CustomWordlist { Name = "Spanish", Wordlist = Wordlist.Spanish },
            new CustomWordlist { Name = "Portuguese Brazil", Wordlist = Wordlist.PortugueseBrazil },
            new CustomWordlist { Name = "Chinese Traditional", Wordlist = Wordlist.ChineseTraditional },
            new CustomWordlist { Name = "Chinese Simplified", Wordlist = Wordlist.ChineseSimplified },
        };

        public List<CustomEntropy> WordCountToEntropyLength { get; } = new List<CustomEntropy>
        {
            new CustomEntropy { WordCount = "12", Length = 128 },
            new CustomEntropy { WordCount = "15", Length = 160 },
            new CustomEntropy { WordCount = "18", Length = 192 },
            new CustomEntropy { WordCount = "21", Length = 224 },
            new CustomEntropy { WordCount = "24", Length = 256 }
        };

        [Reactive] public Network Network { get; set; }
        [Reactive] public string WalletName { get; set; }
        [Reactive] public string PathToWallet { get; set; }

        private CustomWordlist _language;
        public CustomWordlist Language
        {
            get => _language;
            set
            {
                if (_language != value)
                {
                    _language = value;
                    Mnemonic = string.Empty;
                }
            }
        }

        private CustomEntropy _entropy;
        public CustomEntropy Entropy
        {
            get => _entropy;
            set
            {
                if (_entropy != value)
                {
                    _entropy = value;
                    Mnemonic = string.Empty;
                }
            }
        }

        private string _mnemonic;
        public string Mnemonic
        {
            get => _mnemonic;
            set
            {
                if (_mnemonic != value)
                {
                    _mnemonic = value;

                    MnemonicSubstr = !string.IsNullOrEmpty(_mnemonic)
                        ? new ObservableCollection<string>(_mnemonic.Split(' '))
                        : new ObservableCollection<string>();

                    this.RaisePropertyChanged(nameof(Mnemonic));
                    this.RaisePropertyChanged(nameof(MnemonicSubstr));

                    ResetMnemonicCollections();
                }
            }
        }

        [Reactive] public ObservableCollection<string> MnemonicSubstr { get; set; }
        [Reactive] public ObservableCollection<string> SourceMnemonicSubstr { get; set; }
        [Reactive] public ObservableCollection<string> TargetMnemonicSubstr { get; set; }
        [Reactive] public bool MnemonicVerified { get; set; }
        [Reactive] public bool DerivedPswdVerified { get; set; }
        [Reactive] public bool IsEnteredStoragePassword { get; set; }

        private const int DefaultAttemptsCount = 5;

        public void ResetMnemonicCollections()
        {
            Random rnd = new Random();
            SourceMnemonicSubstr = new ObservableCollection<string>(MnemonicSubstr.OrderBy(x => rnd.Next()));
            TargetMnemonicSubstr = new ObservableCollection<string>();
            MnemonicVerified = false;
            DerivedPswdVerified = false;
            Warning = string.Empty;
        }

        public void UpdateMnemonicCollections(string word, bool addition)
        {
            if (addition)
            {
                TargetMnemonicSubstr.Add(word);
                SourceMnemonicSubstr.Remove(word);
            }
            else
            {
                SourceMnemonicSubstr.Add(word);
                TargetMnemonicSubstr.Remove(word);
            }
            if (SourceMnemonicSubstr.Count == 0)
            {
                string delimiter = " ";
                string targetMnemonic = TargetMnemonicSubstr.Aggregate((i, j) => i + delimiter + j);
  
                if (targetMnemonic != _mnemonic)
                {
                    MnemonicVerified = false;
                    DerivedPswdVerified = false;
                    Warning = AppResources.WrongWordOrder;
                }
                else
                {
                    MnemonicVerified = true;
                    if (!_useDerivedKeyPswd)
                        DerivedPswdVerified = true;
                    else
                    {
                        DerivedPswdVerified = false;
                        DerivedPasswordConfirmation = null;
                        this.RaisePropertyChanged(nameof(DerivedPasswordConfirmation));
                    }
                }
            }
            else
            {
                MnemonicVerified = false;
                DerivedPswdVerified = false;
                Warning = string.Empty;
            }
            this.RaisePropertyChanged(nameof(MnemonicVerified));
            this.RaisePropertyChanged(nameof(DerivedPswdVerified));
            this.RaisePropertyChanged(nameof(TargetMnemonicSubstr));
            this.RaisePropertyChanged(nameof(SourceMnemonicSubstr));
        }

        private bool _useDerivedKeyPswd;
        public bool UseDerivedKeyPswd
        {
            get => _useDerivedKeyPswd;
            set
            {
                if (_useDerivedKeyPswd != value)
                {
                    _useDerivedKeyPswd = value;

                    if (!_useDerivedKeyPswd)
                        ClearDerivedPswd();

                    this.RaisePropertyChanged(nameof(UseDerivedKeyPswd));
                }
            }
        }

        private SecureString _derivedPassword;
        public SecureString DerivedPassword
        {
            get => _derivedPassword;
            set
            {
                _derivedPassword = value;
                DerivedPasswordScore = (int)PasswordAdvisor.CheckStrength(DerivedPassword);
                this.RaisePropertyChanged(nameof(DerivedPassword));
            }
        }

        [Reactive] public int DerivedPasswordScore { get; set; }
        [Reactive] public SecureString DerivedPasswordConfirmation { get; set; }
        [Reactive] public SecureString StoragePassword { get; set; }
        [Reactive] public SecureString StoragePasswordConfirmation { get; set; }

        private HdWallet _wallet { get; set; }

        public CreateNewWalletViewModel(IAtomexApp app, INavigationService navigationService)
        {
            _app = app ?? throw new ArgumentNullException(nameof(_app));
            _navigationService = navigationService ?? throw new ArgumentNullException(nameof(_navigationService));
            Network = Network.MainNet;
            Language = Languages.FirstOrDefault();
            Entropy = WordCountToEntropyLength.FirstOrDefault();
            WalletName = string.Empty;
            Mnemonic = string.Empty;
            StoragePassword = new SecureString();
            DerivedPassword = new SecureString();
            DerivedPasswordConfirmation = new SecureString();
            StoragePasswordConfirmation = new SecureString();
        }

        private void SaveWalletName()
        {
            WalletName = WalletName.Trim();
            if (string.IsNullOrEmpty(WalletName))
            {
                Warning = AppResources.EmptyWalletName;
            }

            if (WalletName.IndexOfAny(Path.GetInvalidFileNameChars()) != -1 ||
                WalletName.IndexOf('.') != -1)
            {
                Warning = AppResources.InvalidWalletName;
            }

            string walletsFolder = null;
            string pathToDocuments;
            switch (Device.RuntimePlatform)
            {
                case Device.iOS:
                    pathToDocuments = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                    walletsFolder = Path.Combine(pathToDocuments, "..", "Library", WalletInfo.DefaultWalletsDirectory);
                    break;
                case Device.Android:
                    pathToDocuments = Environment.GetFolderPath(Environment.SpecialFolder.Personal);
                    walletsFolder = Path.Combine(pathToDocuments, WalletInfo.DefaultWalletsDirectory);
                    break;
                default:
                    break;
            }
            if (!Directory.Exists(walletsFolder))
            {
                Directory.CreateDirectory(walletsFolder);
            }
            var pathToWallet = Path.Combine(walletsFolder, $"{WalletName}", WalletInfo.DefaultWalletFileName);

            try
            {
                _ = Path.GetFullPath(pathToWallet);
            }
            catch (Exception)
            {
                Warning = AppResources.InvalidWalletName;
            }

            if (File.Exists(pathToWallet))
            {
                Warning = AppResources.WalletAlreadyExists;
            }

            PathToWallet = pathToWallet;
        }

        private void GenerateMnemonic()
        {
            var entropy = Rand.SecureRandomBytes(Entropy.Length / 8);
            Mnemonic = new Mnemonic(Language.Wordlist, entropy).ToString();
        }

        private void WriteMnemonic()
        {
            Mnemonic = Mnemonic.ToLower();
            if (string.IsNullOrEmpty(Mnemonic))
            {
                Warning = AppResources.EmptyMnemonicError;
            }

            try
            {
                var unused = new Mnemonic(Mnemonic, Language.Wordlist);
                return;
            }
            catch (Exception e)
            {
                if (e.Message.Contains("Word count should be"))
                {
                    Warning = AppResources.MnemonicWordCountError;
                    return;
                }

                if (e.Message.Contains("is not in the wordlist"))
                {
                    Warning = AppResources.Word + " " + e.Message.Split(' ')[1] + " " + AppResources.isNotInWordlist;
                    return;
                }

                Warning = AppResources.InvalidMnemonic;
            }
        }

        private SecureString GenerateSecureString(string str)
        {
            var secureString = new SecureString();
            if (!string.IsNullOrEmpty(str))
            {
                foreach (char c in str)
                {
                    secureString.AppendChar(c);
                }
            }
            return secureString;
        }

        private void SetPassword(PasswordType pswdType, string pswd)
        {
            SecureString secureString = GenerateSecureString(pswd);
            switch (pswdType)
            {
                case PasswordType.StoragePassword:
                    StoragePassword = secureString;
                    break;
                case PasswordType.DerivedPassword:
                    DerivedPassword = secureString;
                    break;
                case PasswordType.StoragePasswordConfirmation:
                    StoragePasswordConfirmation = secureString;
                    break;
                case PasswordType.DerivedPasswordConfirmation:
                    DerivedPasswordConfirmation = secureString;
                    break;
                default:
                    break;
            }    
        }

        private void CheckDerivedPassword()
        {
            if (DerivedPassword != null && DerivedPassword.Length > 0)
            {
                if (DerivedPasswordScore < (int)PasswordAdvisor.PasswordScore.Medium)
                {
                    Warning = AppResources.PasswordHasInsufficientComplexity;
                    return;
                }

                if (DerivedPasswordConfirmation != null &&
                    !DerivedPassword.SecureEqual(DerivedPasswordConfirmation) || DerivedPasswordConfirmation == null)
                {
                    Warning = AppResources.PasswordsDoNotMatch;
                    return;
                }

                Warning = string.Empty;
            }
            else
            {
                Warning = AppResources.PasswordHasInsufficientComplexity;
            }
        }

        private void VerificateDerivedPassword()
        {
            Device.InvokeOnMainThreadAsync(() =>
            {
                if (DerivedPasswordConfirmation != null &&
                !DerivedPassword.SecureEqual(DerivedPasswordConfirmation) || DerivedPasswordConfirmation == null)
                {
                    Warning = AppResources.InvalidPassword;
                    DerivedPswdVerified = false;
                    this.RaisePropertyChanged(nameof(DerivedPswdVerified));
                    return;
                }

                DerivedPswdVerified = true;

                this.RaisePropertyChanged(nameof(DerivedPswdVerified));

                _navigationService?.DisplaySnackBar(SnackbarMessage.MessageType.Regular, AppResources.Verified);

                Warning = string.Empty;
            });
        }

        private bool IsValidStoragePassword()
        {
            if (StoragePassword != null &&
                StoragePasswordConfirmation != null &&
                !StoragePassword.SecureEqual(StoragePasswordConfirmation) || StoragePasswordConfirmation == null)
            {
                Warning = AppResources.PasswordsDoNotMatch;
                return false;
            }

            Warning = string.Empty;
            return true;
        }

        private async Task ConnectToWallet()
        {
            try
            {
                IsLoading = true;

                Account account = null;

                await _wallet.EncryptAsync(StoragePassword);

                if (!_wallet.SaveToFile(_wallet.PathToWallet, StoragePassword))
                    throw new Exception("Can't create wallet file");

                var clientType = Device.RuntimePlatform switch
                {
                    Device.iOS => ClientType.iOS,
                    Device.Android => ClientType.Android,
                    _ => ClientType.Unknown,
                };

                try
                {
                    account = await Task.Run(() =>
                    {
                        return account = new Account(
                            wallet: _wallet,
                            password: StoragePassword,
                            currenciesProvider: _app.CurrenciesProvider);
                    });

                    if (account != null)
                    {
                        try
                        {
                            await SecureStorage.SetAsync(WalletName, string.Empty);
                            await SecureStorage.SetAsync(WalletName + "-" + "AuthType", "Pin");
                            await SecureStorage.SetAsync(WalletName + "-" + "PinAttempts", DefaultAttemptsCount.ToString());
                        }
                        catch (Exception ex)
                        {
                            Log.Error(ex, AppResources.NotSupportSecureStorage);
                        }

                        MainViewModel mainViewModel = null;

                        await Task.Run(() =>
                        {
                            mainViewModel = new MainViewModel(
                                app: _app,
                                account: account);

                            if (CurrentAction == Action.Restore)
                                mainViewModel.InitCurrenciesScan();
                        });

                        Application.Current.MainPage = new MainPage(mainViewModel);
                    }
                    else
                    {
                        _navigationService?.ShowAlert(AppResources.Error, AppResources.CreateWalletError, AppResources.AcceptButton);
                    }
                }
                catch (CryptographicException e)
                {
                    _ = ShakePage();
                    Log.Error(e, "Create wallet error");
                }
            }
            catch (Exception e)
            {
                _ = ShakePage();
                Log.Error(e, "Create wallet error");
            }

            IsLoading = false;
            StoragePassword.Clear();
            this.RaisePropertyChanged(nameof(StoragePassword));
        }

        private ICommand _setTestNetTypeCommand;
        public ICommand SetTestNetTypeCommand => _setTestNetTypeCommand ??= ReactiveCommand.Create(() =>
            {
                Network = Network.TestNet;
                _navigationService?.ShowPage(new WalletNamePage(this));
            });

        private ICommand _setMainNetTypeCommand;
        public ICommand SetMainNetTypeCommand => _setMainNetTypeCommand ??= ReactiveCommand.Create(() =>
            {
                Network = Network.MainNet;
                _navigationService?.ShowPage(new WalletNamePage(this));
            });

        private ICommand _mnemonicPageCommand;
        public ICommand MnemonicPageCommand => _mnemonicPageCommand ??= ReactiveCommand.Create(() =>
            {
                SaveWalletName();
                if (Warning != string.Empty)
                    return;
                ClearDerivedPswd();
                if (CurrentAction == Action.Restore)
                    _navigationService?.ShowPage(new WriteMnemonicPage(this));
                else
                    _navigationService?.ShowPage(new CreateMnemonicPage(this));
            });

        private ICommand _setMnemonicCommand;
        public ICommand SetMnemonicCommand => _setMnemonicCommand ??= ReactiveCommand.Create(() =>
            {
                if (string.IsNullOrEmpty(Mnemonic))
                {
                    GenerateMnemonic();
                }
                else
                {
                    if (UseDerivedKeyPswd)
                    {
                        CheckDerivedPassword();

                        if (Warning != string.Empty)
                            return;
                    }
                    ResetMnemonicCollections();
                    _navigationService?.ShowPage(new VerificationMnemonicPage(this));
                }
            });

        private ICommand _writeDerivedPasswordPageCommand;
        public ICommand WriteDerivedPasswordPageCommand => _writeDerivedPasswordPageCommand ??= ReactiveCommand.Create(() =>
            {
                WriteMnemonic();

                if (Warning != string.Empty)
                    return;
                _navigationService?.ShowPage(new WriteDerivedKeyPasswordPage(this));
            });

        private ICommand _createStoragePasswordPageCommand;
        public ICommand CreateStoragePasswordPageCommand => _createStoragePasswordPageCommand ??= ReactiveCommand.Create(() =>
            {
                _wallet = new HdWallet(
                    mnemonic: Mnemonic,
                    wordList: Language.Wordlist,
                    passPhrase: DerivedPassword,
                    network: Network)
                {
                    PathToWallet = PathToWallet
                };
                ClearStoragePswd();
                Warning = string.Empty;

                _navigationService?.ShowPage(new AuthPage(this));
            });

        private ICommand _derivedPswdChangedCommand;
        public ICommand DerivedPswdChangedCommand => _derivedPswdChangedCommand ??= ReactiveCommand.Create<string>((value) => SetPassword(PasswordType.DerivedPassword, value));

        private ICommand _derivedPswdConfirmationChangedCommand;
        public ICommand DerivedPswdConfirmationChangedCommand => _derivedPswdConfirmationChangedCommand ??= new Command<string>((value) => SetPassword(PasswordType.DerivedPasswordConfirmation, value));

        private ICommand _storagePswdChangedCommand;
        public ICommand StoragePswdChangedCommand => _storagePswdChangedCommand ??= ReactiveCommand.Create<string>((value) => SetPassword(PasswordType.StoragePassword, value));

        private ICommand _storagePswdConfirmationChangedCommand;
        public ICommand StoragePswdConfirmationChangedCommand => _storagePswdConfirmationChangedCommand ??= ReactiveCommand.Create<string>((value) => SetPassword(PasswordType.StoragePasswordConfirmation, value));

        private ICommand _useDerivedPswdInfoCommand;
        public ICommand UseDerivedPswdInfoCommand => _useDerivedPswdInfoCommand ??= ReactiveCommand.Create(() =>
            _navigationService?.ShowAlert(string.Empty, AppResources.DerivedPasswordDescriptionText, AppResources.AcceptButton));

        private ICommand _addWordToVerificationCommand;
        public ICommand AddWordToVerificationCommand => _addWordToVerificationCommand ??= ReactiveCommand.Create<string>((word) =>
            {
                UpdateMnemonicCollections(word, true);
                Device.InvokeOnMainThreadAsync(() =>
                {
                    if (DerivedPswdVerified)
                        _navigationService?.DisplaySnackBar(SnackbarMessage.MessageType.Regular, AppResources.Verified);
                });
            });

        private ICommand _deleteWordFromVerificationCommand;
        public ICommand DeleteWordFromVerificationCommand => _deleteWordFromVerificationCommand ??= ReactiveCommand.Create<string>((word) => UpdateMnemonicCollections(word, false));

        private ICommand _verificateDerivedPswdCommand;
        public ICommand VerificateDerivedPswdCommand => _verificateDerivedPswdCommand ??= new Command(() => VerificateDerivedPassword());

        private ICommand _clearWarningCommand;
        public ICommand ClearWarningCommand => _clearWarningCommand ??= new Command(() => Warning = string.Empty);

        private ICommand _clearMnemonicCommand;
        public ICommand ClearMnemonicCommand => _clearMnemonicCommand ??= new Command(() => Mnemonic = string.Empty);

        private ICommand _createWalletCommand;
        public ICommand CreateWalletCommand => _createWalletCommand ??= ReactiveCommand.CreateFromTask(async () => await ConnectToWallet());

        private ICommand _addCharCommand;
        public ICommand AddCharCommand => _addCharCommand ??= ReactiveCommand.Create<string>((value) =>
            {
                if (!IsEnteredStoragePassword)
                {
                    if (StoragePassword?.Length < 4)
                    {
                        foreach (char c in value)
                            StoragePassword.AppendChar(c);

                        this.RaisePropertyChanged(nameof(StoragePassword));

                        if (StoragePassword?.Length == 4)
                        {
                            IsEnteredStoragePassword = true;

                            Header = AppResources.ReEnterPin;
                            this.RaisePropertyChanged(nameof(Header));
                        }
                    }
                }
                else
                {
                    if (StoragePasswordConfirmation?.Length < 4)
                    {
                        foreach (char c in value)
                        {
                            StoragePasswordConfirmation.AppendChar(c);
                        }

                        this.RaisePropertyChanged(nameof(StoragePasswordConfirmation));

                        if (StoragePasswordConfirmation?.Length == 4)
                        {
                            if (IsValidStoragePassword())
                            {
                                _ = ConnectToWallet();
                            }
                            else
                            {
                                _ = ShakePage();
                                ClearStoragePswd();
                            }
                        }
                    }
                }
            });

        private ICommand _deleteCharCommand;
        public ICommand DeleteCharCommand => _deleteCharCommand ??= ReactiveCommand.Create(() =>
            {
                if (!IsEnteredStoragePassword)
                {
                    if (StoragePassword?.Length != 0)
                    {
                        StoragePassword.RemoveAt(StoragePassword.Length - 1);
                        this.RaisePropertyChanged(nameof(StoragePassword));
                    }
                }
                else
                {
                    if (StoragePasswordConfirmation?.Length != 0)
                    {
                        StoragePasswordConfirmation.RemoveAt(StoragePasswordConfirmation.Length - 1);
                        this.RaisePropertyChanged(nameof(StoragePasswordConfirmation));
                    }
                }
            });

        private ICommand _cancelCommand;
        public ICommand CancelCommand => _cancelCommand ??= ReactiveCommand.Create(() => _navigationService?.ClosePage());

        private async Task ShakePage()
        {
            try
            {
                Vibration.Vibrate();
            }
            catch (FeatureNotSupportedException ex)
            {
                Log.Error(ex, "Vibration not supported on device");
            }

            var view = Application.Current.MainPage;
            await view.TranslateTo(-15, 0, 50);
            await view.TranslateTo(15, 0, 50);
            await view.TranslateTo(-10, 0, 50);
            await view.TranslateTo(10, 0, 50);
            await view.TranslateTo(-5, 0, 50);
            await view.TranslateTo(5, 0, 50);
            view.TranslationX = 0;
        }

        private void ClearDerivedPswd()
        {
            Warning = string.Empty;
            DerivedPassword.Clear();
            DerivedPasswordConfirmation.Clear();
            DerivedPasswordScore = 0;
        }

        private void ClearStoragePswd()
        {
            Warning = string.Empty;
            IsEnteredStoragePassword = false;
            StoragePassword.Clear();
            StoragePasswordConfirmation.Clear();
            Header = AppResources.CreatePin;
            
            this.RaisePropertyChanged(nameof(Header));
            this.RaisePropertyChanged(nameof(StoragePassword));
            this.RaisePropertyChanged(nameof(StoragePasswordConfirmation));
        }
    }
}

