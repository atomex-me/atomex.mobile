using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Security;
using System.Security.Cryptography;
using System.Threading.Tasks;
using atomex.Common;
using atomex.Models;
using atomex.Resources;
using atomex.ViewModel;
using Atomex;
using Atomex.Common;
using Atomex.Cryptography;
using Atomex.Wallet;
using NBitcoin;
using Serilog;
using Xamarin.Essentials;
using Xamarin.Forms;

namespace atomex
{
    public class CreateNewWalletViewModel : BaseViewModel
    {

        public IAtomexApp AtomexApp { get; private set; }

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

        public List<Atomex.Core.Network> Networks { get; } = new List<Atomex.Core.Network>
        {
            Atomex.Core.Network.MainNet,
            Atomex.Core.Network.TestNet
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

        private Atomex.Core.Network _network;
        public Atomex.Core.Network Network
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

                    _mnemonicSubstr = new ObservableCollection<string>(_mnemonic.Split(' '));

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

        private int _storagePasswordScore;
        public int StoragePasswordScore
        {
            get => _storagePasswordScore;
            set { _storagePasswordScore = value; OnPropertyChanged(nameof(StoragePasswordScore)); }
        }

        private SecureString _storagePassword;
        public SecureString StoragePassword
        {
            get => _storagePassword;
            set
            {
                _storagePassword = value;
                StoragePasswordScore = (int)PasswordAdvisor.CheckStrength(StoragePassword);
                OnPropertyChanged(nameof(StoragePassword));
            }
        }

        private SecureString _storagePasswordConfirmation;
        public SecureString StoragePasswordConfirmation
        {
            get => _storagePasswordConfirmation;
            set { _storagePasswordConfirmation = value; OnPropertyChanged(nameof(StoragePasswordConfirmation)); }
        }

        private HdWallet Wallet { get; set; }

        public CreateNewWalletViewModel(IAtomexApp app)
        {
            AtomexApp = app ?? throw new ArgumentNullException(nameof(AtomexApp));
            Network = Atomex.Core.Network.MainNet;
            Language = Languages.FirstOrDefault();
            Entropy = WordCountToEntropyLength.FirstOrDefault();
        }

        public void SaveWalletName()
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

        public void GenerateMnemonic()
        {
            var entropy = Rand.SecureRandomBytes(Entropy.Length / 8);
            Mnemonic = new Mnemonic(Language.Wordlist, entropy).ToString();
        }

        public void WriteMnemonic()
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

        public void SetPassword(PasswordType pswdType, string pswd)
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

        public void CheckDerivedPassword()
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

        public void VerificateDerivedPassword()
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

            Warning = string.Empty;
        }

        private void CheckStoragePassword()
        {
            if (StoragePasswordScore < (int)PasswordAdvisor.PasswordScore.Medium)
            {
                Warning = AppResources.PasswordHasInsufficientComplexity;
                return;
            }

            if (StoragePassword != null &&
                StoragePasswordConfirmation != null &&
                !StoragePassword.SecureEqual(StoragePasswordConfirmation) || StoragePasswordConfirmation == null)
            {
                Warning = AppResources.PasswordsDoNotMatch;
                return;
            }

            Warning = string.Empty;
        }

        public void CreateHdWallet()
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

        private Command _createWalletCommand;
        public Command CreateWalletCommand => _createWalletCommand ??= new Command(async () => await ConnectToWallet());

        private async Task ConnectToWallet()
        {
            try
            {
                IsLoading = true;

                CheckStoragePassword();

                if (Warning != string.Empty)
                {
                    IsLoading = false;
                    return;
                }

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
                    account = new Account(
                        wallet: Wallet,
                        password: StoragePassword,
                        currenciesProvider: AtomexApp.CurrenciesProvider,
                        clientType);

                    if (account != null)
                    {
                        try
                        {
                            await SecureStorage.SetAsync(WalletName, string.Empty);
                        }
                        catch (Exception ex)
                        {
                            Log.Error(ex, AppResources.NotSupportSecureStorage);
                        }

                        MainViewModel mainViewModel = null;

                        await Task.Run(() =>
                        {
                            mainViewModel = new MainViewModel(
                                AtomexApp,
                                account,
                                WalletName,
                                CurrentAction == Action.Restore ? true : false);
                        });

                        Application.Current.MainPage = new MainPage(mainViewModel);
                    }
                    else
                    {
                        IsLoading = false;
                        await Application.Current.MainPage.DisplayAlert(AppResources.Error, AppResources.CreateWalletError, AppResources.AcceptButton);
                    }
                }
                catch (CryptographicException e)
                {
                    IsLoading = false;
                    Log.Error(e, "Create wallet error");
                }
            }
            catch (Exception e)
            {
                IsLoading = false;
                Log.Error(e, "Create wallet error");
            }
        }

        public void Clear()
        {
            WalletName = string.Empty;
            Language = Languages.FirstOrDefault();
            Entropy = WordCountToEntropyLength.FirstOrDefault();
            Mnemonic = string.Empty;
            ClearDerivedPswd();
            ClearStoragePswd();
        }

        public void ClearDerivedPswd()
        {
            Warning = string.Empty;
            DerivedPassword = null;
            DerivedPasswordConfirmation = null;
            DerivedPasswordScore = 0;
        }

        public void ClearStoragePswd()
        {
            Warning = string.Empty;
            StoragePassword = null;
            StoragePasswordConfirmation = null;
            StoragePasswordScore = 0;
        }
    }
}

