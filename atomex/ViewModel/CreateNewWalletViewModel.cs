using System;
using System.Collections.Generic;
using System.IO;
using System.Security;
using atomex.ViewModel;
using Atomex;
using Atomex.Common;
using Atomex.Core;
using Atomex.Cryptography;
using Atomex.Wallet;
using NBitcoin;
using Nethereum.HdWallet;
using Serilog;

namespace atomex
{
    public class CreateNewWalletViewModel : BaseViewModel
    {

        private IAtomexApp _app;

        public enum Action
        {
            Create,
            Restore
        }

        public Action CurrentAction { get; set; }

        public List<Atomex.Core.Network> Networks { get; } = new List<Atomex.Core.Network>
        {
            Atomex.Core.Network.MainNet,
            Atomex.Core.Network.TestNet
        };

        public IEnumerable<KeyValuePair<string, Wordlist>> Languages { get; } = new List<KeyValuePair<string, Wordlist>>
        {
            new KeyValuePair<string, Wordlist>("English", Wordlist.English),
            new KeyValuePair<string, Wordlist>("French", Wordlist.French),
            new KeyValuePair<string, Wordlist>("Japanese", Wordlist.Japanese),
            new KeyValuePair<string, Wordlist>("Spanish", Wordlist.Spanish),
            new KeyValuePair<string, Wordlist>("Portuguese Brazil", Wordlist.PortugueseBrazil),
            new KeyValuePair<string, Wordlist>("Chinese Traditional", Wordlist.ChineseTraditional),
            new KeyValuePair<string, Wordlist>("Chinese Simplified", Wordlist.ChineseSimplified)
        };

        public IEnumerable<KeyValuePair<string, int>> WordCountToEntropyLength { get; } = new List<KeyValuePair<string, int>>
        {
            new KeyValuePair<string, int>("12", 128),
            new KeyValuePair<string, int>("15", 160),
            new KeyValuePair<string, int>("18", 192),
            new KeyValuePair<string, int>("21", 224),
            new KeyValuePair<string, int>("24", 256)
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

        private Wordlist _language = Wordlist.English;
        public Wordlist Language
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

        private int _entropyLength = 192;
        public int EntropyLength
        {
            get => _entropyLength;
            set
            {
                if (_entropyLength != value)
                {
                    _entropyLength = value;
                    Mnemonic = string.Empty;
                }
            }
        }

        private string _mnemonic;
        public string Mnemonic
        {
            get => _mnemonic;
            set { _mnemonic = value; OnPropertyChanged(nameof(Mnemonic)); }
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
                OnPropertyChanged(nameof(DerivedPassword)); }
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

        public CreateNewWalletViewModel()
        {}

        public CreateNewWalletViewModel(IAtomexApp app)
        {
            _app = app;

            Network = Atomex.Core.Network.MainNet;

            //Language = Wordlist.English;
            //foreach (KeyValuePair<string, Wordlist> kvp in Languages)
            //    kvp.Key
            Console.WriteLine(Language);
            Console.WriteLine(Languages);
            Console.WriteLine(EntropyLength);
        }

        public string SaveWalletName()
        {
            if (string.IsNullOrEmpty(WalletName))
            {
                return "Wallet name must be not empty";
            }

            if (WalletName.IndexOfAny(Path.GetInvalidFileNameChars()) != -1 ||
                WalletName.IndexOf('.') != -1)
            {
                return "Invalid wallet name";
            }

            //var pathToWallet = $"{WalletInfo.CurrentWalletDirectory}/{WalletName}/{WalletInfo.DefaultWalletFileName}";

            var documents = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            var library = Path.Combine(documents, "..", "Library");
            var pathToWallet = Path.Combine(library, $"{WalletName}.wallet");

            try
            {
                var _ = Path.GetFullPath(pathToWallet);
            }
            catch (Exception)
            {
                return "Invalid wallet name";
            }

            if (File.Exists(pathToWallet))
            {
                return "Wallet with the same name already exists";
            }

            return null;

        }
        public void GenerateMnemonic()
        {
            var entropy = Rand.SecureRandomBytes(EntropyLength / 8);
            Console.WriteLine(EntropyLength);
            Console.WriteLine(entropy);
            
            Mnemonic = new Mnemonic(Language, entropy).ToString();
        }

        public string WriteMnemonic()
        {
            if (string.IsNullOrEmpty(Mnemonic))
            {
                return "Mnemonic phrase can not be empty";
            }

            try
            {
                var unused = new Mnemonic(Mnemonic, Language);
                return null;
            }
            catch (Exception e)
            {
                if (e.Message.Contains("Word count should be"))
                    return "Word count should be 12,15,18,21 or 24.";
                else if (e.Message.Contains("is not in the wordlist"))
                    return "Word " + e.Message.Split(' ')[1] + " is not in the wordlist for this language";
                else
                    return "Invalid mnemonic phrase";
            }
        }

        private SecureString GenerateSecureString(string str)
        {
            var secureString = new SecureString();
            foreach (char c in str)
            {
                secureString.AppendChar(c);
            }
            return secureString;
        }

        public void SetPassword(string pswdType, string pswd)
        {
            SecureString secureString = GenerateSecureString(pswd);
            if (pswdType == "StoragePassword")
            {
                StoragePassword = secureString;
                return;
            }
            if (pswdType == "DerivedPassword")
            {
                DerivedPassword = secureString;
                return;
            }
            if (pswdType == "StoragePasswordConfirmation")
            {
                StoragePasswordConfirmation = secureString;
                return;
            }
            if (pswdType == "DerivedPasswordConfirmation")
            {
                DerivedPasswordConfirmation = secureString;
                return;
            }
        }

        public string CheckDerivedPassword()
        {
            if (DerivedPassword != null && DerivedPassword.Length > 0)
            {
                if (DerivedPasswordScore < (int)PasswordAdvisor.PasswordScore.Medium)
                {
                    return "Password has insufficient complexity";
                }

                if (DerivedPasswordConfirmation != null &&
                    !DerivedPassword.SecureEqual(DerivedPasswordConfirmation) || DerivedPasswordConfirmation == null)
                {
                    return "Passwords do not match";
                }
            }
            return null;
        }

        public string CheckStoragePassword()
        {
            if (StoragePasswordScore < (int)PasswordAdvisor.PasswordScore.Medium)
            {
                return "Password has insufficient complexity";
            }

            if (StoragePassword != null &&
                StoragePasswordConfirmation != null &&
                !StoragePassword.SecureEqual(StoragePasswordConfirmation) || StoragePasswordConfirmation == null)
            {
                return "Passwords do not match";
            }
            return null;
        }

        public async void CreateHdWallet()
        {
            Wallet = new HdWallet(
                mnemonic: Mnemonic,
                wordList: Language,
                passPhrase: DerivedPassword,
                network: Network);

            //await wallet.EncryptAsync(StoragePassword);

            //wallet.SaveToFile($"/{WalletName}.wallet", StoragePassword);

            // todo: Load wallet
        }

        public async void ConnectToWallet()
        {
            try
            {

                await Wallet.EncryptAsync(StoragePassword);

                Wallet.SaveToFile(Wallet.PathToWallet, StoragePassword);

                var account = new Account(
                    wallet: Wallet,
                    password: StoragePassword,
                    currenciesProvider: _app.CurrenciesProvider,
                    symbolsProvider: _app.SymbolsProvider);
            }
            catch (Exception e)
            {
                // todo: warning
                Log.Error(e, "Create wallet error");
            }
        }

        public void Clear()
        {
            WalletName = string.Empty;
            Language = Wordlist.English;
            Mnemonic = string.Empty;
            DerivedPassword = null;
            DerivedPasswordConfirmation = null;
            DerivedPasswordScore = 0;
            StoragePassword = null;
            StoragePasswordConfirmation = null;
            StoragePasswordScore = 0;
        }
    }
}

