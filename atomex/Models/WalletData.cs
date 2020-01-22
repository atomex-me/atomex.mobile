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
                    Amount = 0.223M,
                    Price = 8731.2M,
                    Address = "btc#u8h2d2-8637tbjdhhsjk"
                },
                new Wallet
                {
                    Name = "ETH",
                    FullName = "Ethereum",
                    Amount = 1.256M,
                    Price = 186.4M,
                    Address = "eth#8su7s62gk^4hbdj#$7"
                },
                new Wallet
                {
                    Name = "XTZ",
                    FullName = "Tezos",
                    Amount = 678.352M,
                    Price = 1.14M,
                    Address = "xtz#h6s02ks92d9@jd3$ss0l"
                },
                new Wallet
                {
                    Name = "LTC",
                    FullName = "Litecoin",
                    Amount = 3.6M,
                    Price = 52.2M,
                    Address = "ltc#7u93J0kDt79Dk@4jkllas"
                }
            };
        }
    }
}