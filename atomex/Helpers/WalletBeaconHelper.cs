using System;
using System.IO;
using Beacon.Sdk;
using Beacon.Sdk.WalletBeaconClient;
using Serilog.Extensions.Logging;
using FileSystem = Atomex.Common.FileSystem;

namespace atomex.Helpers
{
    public static class WalletBeaconHelper
    {
        const string ConnectionString = "FileName=/data/user/0/com.atomex.android/files/wallets/test/data.db;Password=0KxOw3d7w8U+mENO7JqrY05jKUg9X6X9D1+oWWqfW8o=;Mode=Exclusive";
        //const string path = "test1.db";
        private static readonly WalletBeaconClientFactory factory = new();
        private static IWalletBeaconClient walletBeaconClient = null;

        public static IWalletBeaconClient WalletBeaconClient
        {
            get
            {
                var fileSystem = FileSystem.Current;
                //var path = Path.Combine(FileSystem.Current.PathToDocuments, "wallet_beacon", "client");
                var path = Path.Combine(FileSystem.Current.PathToDocuments, "test.db");


                //var path = Path.Combine(Path.GetDirectoryName(Wallet.PathToWallet), DefaultDataFileName)
                if (walletBeaconClient != null)
                    return walletBeaconClient;

                var options = new BeaconOptions
                {
                    AppName = "Atomex Mobile",
                    AppUrl = "", //string?
                    IconUrl = "", // string?
                    KnownRelayServers = new[]
                {
                    "beacon-node-0.papers.tech:8448",
                    "beacon-node-1.diamond.papers.tech",
                    "beacon-node-1.sky.papers.tech",
                    "beacon-node-2.sky.papers.tech",
                    "beacon-node-1.hope.papers.tech",
                    "beacon-node-1.hope-2.papers.tech",
                    "beacon-node-1.hope-3.papers.tech",
                    "beacon-node-1.hope-4.papers.tech",
                },
                    DatabaseConnectionString = $"Filename={path}" //$"{ConnectionString}"
                };

                walletBeaconClient = factory.Create(options, new SerilogLoggerFactory());

                return walletBeaconClient;
            }
        }
    }
}
