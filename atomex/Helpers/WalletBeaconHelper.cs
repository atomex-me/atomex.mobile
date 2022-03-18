using System;
using Beacon.Sdk;
using Beacon.Sdk.WalletBeaconClient;
using Serilog.Extensions.Logging;

namespace atomex.Helpers
{
    public static class WalletBeaconHelper
    {
        const string path = "test1.db";
        private static readonly WalletBeaconClientFactory factory = new();
        private static IWalletBeaconClient walletBeaconClient = null;

        public static IWalletBeaconClient WalletBeaconClient
        {
            get
            {
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
                    DatabaseConnectionString = $"Filename={path}"
                };

                walletBeaconClient = factory.Create(options, new SerilogLoggerFactory());

                return walletBeaconClient;
            }
        }
    }
}
