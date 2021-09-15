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
using atomex.Services;
using atomex.ViewModel;
using atomex.Views;
using atomex.Views.CreateNewWallet;
using Atomex;
using Atomex.Common;
using Atomex.Cryptography;
using Atomex.Wallet;
using NBitcoin;
using Serilog;
using Xamarin.Essentials;
using Xamarin.Forms;
using Network = Atomex.Core.Network;

namespace atomex
{
    public class CreateNewWalletViewModel : BaseViewModel
    {
        public IAtomexApp AtomexApp { get; private set; }

        public INavigation Navigation { get; set; }

        private IToastService _toastService;

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
                if (_currentAction == Action.Restore)
                    _title = AppResources.RestoreWalletPageTitle;
                else
                    _title = AppResources.CreateNewWalletPageTitle;
                OnPropertyChanged(nameof(Title));
                OnPropertyChanged(nameof(CurrentAction));
            }
        }

        private string _header;
        public string Header
        {
            get => _header;
            set { _header = value; OnPropertyChanged(nameof(Header)); }
        }

        private string _title;
        public string Title
        {
            get => _title;
            set { _title = value; OnPropertyChanged(nameof(Title)); }
        }

        private string _warning;
        public string Warning
        {
            get => _warning;
            set { _warning = value; OnPropertyChanged(nameof(Warning)); }
        }

        private float _opacity = 1f;
        public float Opacity
        {
            get => _opacity;
            set { _opacity = value; OnPropertyChanged(nameof(Opacity)); }
        }

        private bool _isLoading = false;
        public bool IsLoading
        {
            get => _isLoading;
            set
            {
                if (_isLoading == value)
                    return;

                _isLoading = value;

                if (_isLoading)
                    Opacity = 0.3f;
                else
                    Opacity = 1f;

                OnPropertyChanged(nameof(IsLoading));
            }
        }

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

        private Network _network;
        public Network Network
        {
            get => _network;
            set { _network = value; OnPropertyChanged(nameof(Network)); }
        }

        private string _walletName;
        public string WalletName
        {
            get => _walletName;
            set { _walletName = value; OnPropertyChanged(nameof(WalletName)); }
        }

        public string PathToWallet { get; set; }

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

                    if (!string.IsNullOrEmpty(_mnemonic))
                        _mnemonicSubstr = new ObservableCollection<string>(_mnemonic.Split(' '));
                    else
                        _mnemonicSubstr = new ObservableCollection<string>();

                    OnPropertyChanged(nameof(Mnemonic));
                    OnPropertyChanged(nameof(MnemonicSubstr));

                    ResetMnemonicCollections();
                }
            }
        }

        private ObservableCollection<string> _mnemonicSubstr;
        public ObservableCollection<string> MnemonicSubstr
        {
            get => _mnemonicSubstr;
            set { _mnemonicSubstr = value; OnPropertyChanged(nameof(MnemonicSubstr));}
        }

        private ObservableCollection<string> _sourceMnemonicSubstr;
        public ObservableCollection<string> SourceMnemonicSubstr
        {
            get => _sourceMnemonicSubstr;
            set { _sourceMnemonicSubstr = value; OnPropertyChanged(nameof(SourceMnemonicSubstr)); }
        }

        private ObservableCollection<string> _targetMnemonicSubstr;
        public ObservableCollection<string> TargetMnemonicSubstr
        {
            get => _targetMnemonicSubstr;
            set { _targetMnemonicSubstr = value; OnPropertyChanged(nameof(TargetMnemonicSubstr)); }
        }

        private bool _mnemonicVerified;
        public bool MnemonicVerified
        {
            get => _mnemonicVerified;
            set { _mnemonicVerified = value; OnPropertyChanged(nameof(MnemonicVerified)); }
        }

        private bool _derivedPswdVerified = false;
        public bool DerivedPswdVerified
        {
            get => _derivedPswdVerified;
            set { _derivedPswdVerified = value; OnPropertyChanged(nameof(DerivedPswdVerified)); }
        }

        private bool _isEnteredStoragePassword = false;
        public bool IsEnteredStoragePassword
        {
            get => _isEnteredStoragePassword;
            set { _isEnteredStoragePassword = value; OnPropertyChanged(nameof(IsEnteredStoragePassword)); }
        }

        private readonly int DefaultAttemptsCount = 5;

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
                _targetMnemonicSubstr.Add(word);
                _sourceMnemonicSubstr.Remove(word);
            }
            else
            {
                _sourceMnemonicSubstr.Add(word);
                _targetMnemonicSubstr.Remove(word);
            }
            if (_sourceMnemonicSubstr.Count == 0)
            {
                string delimiter = " ";
                string targetMnemonic = _targetMnemonicSubstr.Aggregate((i, j) => i + delimiter + j);
  
                if (targetMnemonic != _mnemonic)
                {
                    _mnemonicVerified = false;
                    _derivedPswdVerified = false;
                    Warning = AppResources.WrongWordOrder;
                }
                else
                {
                    _mnemonicVerified = true;
                    if (!_useDerivedKeyPswd)
                        _derivedPswdVerified = true;
                    else
                    {
                        _derivedPswdVerified = false;
                        _derivedPasswordConfirmation = null;
                        OnPropertyChanged(nameof(DerivedPasswordConfirmation));
                    }
                }
            }
            else
            {
                _mnemonicVerified = false;
                _derivedPswdVerified = false;
                Warning = string.Empty;
            }
            OnPropertyChanged(nameof(MnemonicVerified));
            OnPropertyChanged(nameof(DerivedPswdVerified));
            OnPropertyChanged(nameof(TargetMnemonicSubstr));
            OnPropertyChanged(nameof(SourceMnemonicSubstr));
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

                    OnPropertyChanged(nameof(UseDerivedKeyPswd));
                }
            }
        }

        private int _derivedPasswordScore;
        public int DerivedPasswordScore
        {
            get => _derivedPasswordScore;
            set { _derivedPasswordScore = value; OnPropertyChanged(nameof(DerivedPasswordScore)); }
        }

        private SecureString _derivedPassword;
        public SecureString DerivedPassword
        {
            get => _derivedPassword;
            set
            {
                _derivedPassword = value;
                DerivedPasswordScore = (int)PasswordAdvisor.CheckStrength(DerivedPassword);
                OnPropertyChanged(nameof(DerivedPassword));
            }
        }

        private SecureString _derivedPasswordConfirmation;
        public SecureString DerivedPasswordConfirmation
        {
            get => _derivedPasswordConfirmation;
            set { _derivedPasswordConfirmation = value; OnPropertyChanged(nameof(DerivedPasswordConfirmation)); }
        }

        private SecureString _storagePassword;
        public SecureString StoragePassword
        {
            get => _storagePassword;
            set { _storagePassword = value; OnPropertyChanged(nameof(StoragePassword)); }
        }

        private SecureString _storagePasswordConfirmation;
        public SecureString StoragePasswordConfirmation
        {
            get => _storagePasswordConfirmation;
            set { _storagePasswordConfirmation = value; OnPropertyChanged(nameof(StoragePasswordConfirmation)); }
        }

        private HdWallet Wallet { get; set; }

        public CreateNewWalletViewModel(IAtomexApp app, INavigation navigation)
        {
            AtomexApp = app ?? throw new ArgumentNullException(nameof(AtomexApp));
            Navigation = navigation;
            _toastService = DependencyService.Get<IToastService>();
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
            if (DerivedPasswordConfirmation != null &&
                !DerivedPassword.SecureEqual(DerivedPasswordConfirmation) || DerivedPasswordConfirmation == null)
            {
                Warning = AppResources.InvalidPassword;
                _derivedPswdVerified = false;
                OnPropertyChanged(nameof(DerivedPswdVerified));
                return;
            }

            _derivedPswdVerified = true;

            OnPropertyChanged(nameof(DerivedPswdVerified));

            _toastService?.Show(AppResources.Verified, ToastPosition.Center, Application.Current.RequestedTheme.ToString());

            Warning = string.Empty;
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

        private void CreateHdWallet()
        {
            Wallet = new HdWallet(
                mnemonic: Mnemonic,
                wordList: Language.Wordlist,
                passPhrase: DerivedPassword,
                network: Network
                )
            {
                PathToWallet = PathToWallet
            };
        }

        private async Task ConnectToWallet()
        {
            try
            {
                IsLoading = true;

                Account account = null;

                await Wallet.EncryptAsync(StoragePassword);

                Wallet.SaveToFile(Wallet.PathToWallet, StoragePassword);

                ClientType clientType;

                switch (Device.RuntimePlatform)
                {
                    case Device.iOS:
                        clientType = ClientType.iOS;
                        break;
                    case Device.Android:
                        clientType = ClientType.Android;
                        break;
                    default:
                        clientType = ClientType.Unknown;
                        break;
                }

                try
                {

                    account = await Task.Run(() =>
                    {
                        return account = new Account(
                        wallet: Wallet,
                        password: StoragePassword,
                        currenciesProvider: AtomexApp.CurrenciesProvider,
                        clientType);
                    });

                    if (account != null)
                    {
                        try
                        {
                            await SecureStorage.SetAsync(WalletName, string.Empty);
                            await SecureStorage.SetAsync(WalletName + "-" + "AuthVersion", "1.1");
                            await SecureStorage.SetAsync(WalletName + "-" + "PinAttempts", DefaultAttemptsCount.ToString());
                        }
                        catch (Exception ex)
                        {
                            Log.Error(ex, AppResources.NotSupportSecureStorage);
                        }

                        string appTheme = Application.Current.RequestedTheme.ToString().ToLower();

                        MainViewModel mainViewModel = null;

                        await Task.Run(() =>
                        {
                            mainViewModel = new MainViewModel(
                                AtomexApp,
                                account,
                                WalletName,
                                appTheme,
                                CurrentAction == Action.Restore ? true : false);
                        });

                        Application.Current.MainPage = new MainPage(mainViewModel);
                    }
                    else
                    {
                        await Application.Current.MainPage.DisplayAlert(AppResources.Error, AppResources.CreateWalletError, AppResources.AcceptButton);
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
            OnPropertyChanged(nameof(StoragePassword));
        }

        private ICommand _setTestNetTypeCommand;
        public ICommand SetTestNetTypeCommand => _setTestNetTypeCommand ??= new Command(async () => await SetTestNetType());

        private ICommand _setMainNetTypeCommand;
        public ICommand SetMainNetTypeCommand => _setMainNetTypeCommand ??= new Command(async () => await SetMainNetType());

        private ICommand _mnemonicPageCommand;
        public ICommand MnemonicPageCommand => _mnemonicPageCommand ??= new Command(async () => await MnemonicPage());

        private ICommand _setMnemonicCommand;
        public ICommand SetMnemonicCommand => _setMnemonicCommand ??= new Command(async () => await SetMnemonic());

        private ICommand _writeDerivedPasswordPageCommand;
        public ICommand WriteDerivedPasswordPageCommand => _writeDerivedPasswordPageCommand ??= new Command(async () => await WriteDerivedPasswordPage());

        private ICommand _createStoragePasswordPageCommand;
        public ICommand CreateStoragePasswordPageCommand => _createStoragePasswordPageCommand ??= new Command(async () => await CreateStoragePasswordPage());

        private ICommand _derivedPswdChangedCommand;
        public ICommand DerivedPswdChangedCommand => _derivedPswdChangedCommand ??= new Command<string>((value) => SetPassword(PasswordType.DerivedPassword, value));

        private ICommand _derivedPswdConfirmationChangedCommand;
        public ICommand DerivedPswdConfirmationChangedCommand => _derivedPswdConfirmationChangedCommand ??= new Command<string>((value) => SetPassword(PasswordType.DerivedPasswordConfirmation, value));

        private ICommand _storagePswdChangedCommand;
        public ICommand StoragePswdChangedCommand => _storagePswdChangedCommand ??= new Command<string>((value) => SetPassword(PasswordType.StoragePassword, value));

        private ICommand _storagePswdConfirmationChangedCommand;
        public ICommand StoragePswdConfirmationChangedCommand => _storagePswdConfirmationChangedCommand ??= new Command<string>((value) => SetPassword(PasswordType.StoragePasswordConfirmation, value));

        private ICommand _useDerivedPswdInfoCommand;
        public ICommand UseDerivedPswdInfoCommand => _useDerivedPswdInfoCommand ??= new Command(() => OnUseDerivedPswdRowTapped());

        private ICommand _addWordToVerificationCommand;
        public ICommand AddWordToVerificationCommand => _addWordToVerificationCommand ??= new Command<string>((value) => OnSourceWordTapped(value));

        private ICommand _deleteWordFromVerificationCommand;
        public ICommand DeleteWordFromVerificationCommand => _deleteWordFromVerificationCommand ??= new Command<string>((value) => OnTargetWordTapped(value));

        private ICommand _verificateDerivedPswdCommand;
        public ICommand VerificateDerivedPswdCommand => _verificateDerivedPswdCommand ??= new Command(() => VerificateDerivedPassword());

        private ICommand _clearWarningCommand;
        public ICommand ClearWarningCommand => _clearWarningCommand ??= new Command(() => ClearWarning());

        private ICommand _clearMnemonicCommand;
        public ICommand ClearMnemonicCommand => _clearMnemonicCommand ??= new Command(() => ClearMnemonic());

        private ICommand _createWalletCommand;
        public ICommand CreateWalletCommand => _createWalletCommand ??= new Command(async () => await ConnectToWallet());

        private ICommand _addCharCommand;
        public ICommand AddCharCommand => _addCharCommand ??= new Command<string>((value) => AddChar(value));

        private ICommand _deleteCharCommand;
        public ICommand DeleteCharCommand => _deleteCharCommand ??= new Command(() => RemoveChar());

        private ICommand _backCommand;
        public ICommand BackCommand => _backCommand ??= new Command(async () => await BackButtonClicked());

        private async Task BackButtonClicked()
        {
            await Navigation.PopAsync();
        }

        private void AddChar(string str)
        {
            if (!IsEnteredStoragePassword)
            {
                if (StoragePassword?.Length < 4)
                {
                    foreach (char c in str)
                    {
                        StoragePassword.AppendChar(c);
                    }

                    OnPropertyChanged(nameof(StoragePassword));

                    if (StoragePassword?.Length == 4)
                    {
                        IsEnteredStoragePassword = true;

                        Header = AppResources.ReEnterPin;
                        OnPropertyChanged(nameof(Header));
                    }
                }
            }
            else
            {
                if (StoragePasswordConfirmation?.Length < 4)
                {
                    foreach (char c in str)
                    {
                        StoragePasswordConfirmation.AppendChar(c);
                    }

                    OnPropertyChanged(nameof(StoragePasswordConfirmation));

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
        }

        private void RemoveChar()
        {
            if (!IsEnteredStoragePassword)
            {
                if (StoragePassword?.Length != 0)
                {
                    StoragePassword.RemoveAt(StoragePassword.Length - 1);
                    OnPropertyChanged(nameof(StoragePassword));
                }
            }
            else
            {
                if (StoragePasswordConfirmation?.Length != 0)
                {
                    StoragePasswordConfirmation.RemoveAt(StoragePasswordConfirmation.Length - 1);
                    OnPropertyChanged(nameof(StoragePasswordConfirmation));
                }
            }
        }

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

        private async Task SetMainNetType()
        {
            Network = Network.MainNet;
            await Navigation.PushAsync(new WalletNamePage(this));
        }

        private async Task SetTestNetType()
        {
            Network = Network.TestNet;
            await Navigation.PushAsync(new WalletNamePage(this));
        }

        private async Task MnemonicPage()
        {
            SaveWalletName();
            if (Warning != string.Empty)
                return;
            ClearDerivedPswd();
            if (CurrentAction == Action.Restore)
                await Navigation.PushAsync(new WriteMnemonicPage(this));
            else
                await Navigation.PushAsync(new CreateMnemonicPage(this));
        }

        async Task SetMnemonic()
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
                await Navigation.PushAsync(new VerificationMnemonicPage(this));
            }
        }

        private async void OnUseDerivedPswdRowTapped()
        {
            await Application.Current.MainPage.DisplayAlert("", AppResources.DerivedPasswordDescriptionText, AppResources.AcceptButton);
        }

        private void OnSourceWordTapped(string word)
        {
            UpdateMnemonicCollections(word, true);
            if (DerivedPswdVerified)
                _toastService?.Show(AppResources.Verified, ToastPosition.Center, Application.Current.RequestedTheme.ToString());
        }

        private void OnTargetWordTapped(string word)
        {
            UpdateMnemonicCollections(word, false);
        }

        private async Task WriteDerivedPasswordPage()
        {
            WriteMnemonic();

            if (Warning != string.Empty)
                return;

            await Navigation.PushAsync(new WriteDerivedKeyPasswordPage(this));
        }

        private async Task CreateStoragePasswordPage()
        {
            CreateHdWallet();
            ClearStoragePswd();
            await Navigation.PushAsync(new AuthPage(this));
        }

        private void ClearWarning()
        {
            Warning = string.Empty;
        }

        private void ClearMnemonic()
        {
            Mnemonic = string.Empty;
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

            OnPropertyChanged(nameof(Header));
            OnPropertyChanged(nameof(StoragePassword));
            OnPropertyChanged(nameof(StoragePasswordConfirmation));
        }
    }
}

