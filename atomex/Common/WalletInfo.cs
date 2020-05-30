using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Atomex.Core;
using Serilog;
using Xamarin.Forms;

namespace atomex.Common
{
    public class WalletInfo
    {
        public const string DefaultWalletsDirectory = "wallets";
        public const string DefaultWalletFileName = "atomex.wallet";

        public string Name { get; set; }
        public string Path { get; set; }
        public Network Network { get; set; }
        public string Description => Network == Network.MainNet
            ? Name
            : $"{Name} [TEST]";



        public static IEnumerable<WalletInfo> AvailableWallets()
        {
            var result = new List<WalletInfo>();

            string walletsFolder = null;
            string pathToDocuments;
            switch (Device.RuntimePlatform)
            {
                case Device.iOS:
                    pathToDocuments = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                    walletsFolder = System.IO.Path.Combine(pathToDocuments, "..", "Library", DefaultWalletsDirectory);
                    break;
                case Device.Android:
                    pathToDocuments = Environment.GetFolderPath(Environment.SpecialFolder.Personal);
                    walletsFolder = System.IO.Path.Combine(pathToDocuments, DefaultWalletsDirectory);
                    break;
                default:
                    break;
            }

            if (!Directory.Exists(walletsFolder))
            {
                return result;
            }

            var walletsDirectory = new DirectoryInfo(walletsFolder);
            foreach (var directory in walletsDirectory.GetDirectories())
            {
                var walletFile = directory
                    .GetFiles(DefaultWalletFileName)
                    .FirstOrDefault();

                if (walletFile != null)
                {
                    try
                    {
                        Network type;

                        using (var stream = walletFile.OpenRead())
                        {
                            type = stream.ReadByte() == 0
                                ? Network.MainNet
                                : Network.TestNet;
                        }

                        result.Add(new WalletInfo
                        {
                            Name = directory.Name,
                            Path = walletFile.FullName,
                            Network = type
                        });
                    }
                    catch (Exception)
                    {
                        Log.Warning("Wallet file {@fullName} scan error", walletFile.FullName);
                    }
                }
            }

            result.Sort((a, b) => a.Network.CompareTo(b.Network));

            return result;
        }
    }
}

