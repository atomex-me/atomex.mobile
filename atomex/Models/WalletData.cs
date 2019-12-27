using System.Collections.Generic;

namespace atomex.Models
{
    public static class WalletData
    {
        public static List<Wallet> Wallets { get; set; }

        static WalletData()
        {
            Wallets = new List<Wallet>
            {
                new Wallet
                {
                    Name = "BTC",
                    FullName = "Bitcoin",
                    Amount = 0.223f,
                    ImageUrl = "BtcIcon",
                    Price = 8731.2f,
                    Address = "btc#u8h2d2-8637tbjdhhsjk"
                },
                new Wallet
                {
                    Name = "ETH",
                    FullName = "Ethereum",
                    Amount = 1.256f,
                    ImageUrl = "EthIcon",
                    Price = 186.4f,
                    Address = "eth#8su7s62gk^4hbdj#$7"
                },
                new Wallet
                {
                    Name = "XTZ",
                    FullName = "Tezos",
                    Amount = 678.352f,
                    ImageUrl = "XtzIcon",
                    Price = 1.14f,
                    Address = "xtz#h6s02ks92d9@jd3$ss0l"
                }
            };
        }
    }
}