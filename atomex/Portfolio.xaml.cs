using System;
using System.Collections.Generic;

using Xamarin.Forms;
using atomex.Models;
using SkiaSharp;
using atomex.ViewModel;
using atomex.CustomElements;

namespace atomex
{
    public partial class Portfolio : ContentPage
    {
        WalletsViewModel WalletsViewModel;

        public Portfolio()
        {
            InitializeComponent();
        }

        public Portfolio(WalletsViewModel WalletsViewModel)
        {
            InitializeComponent();
            UpdateChart(WalletsViewModel);
        }

        private void UpdateChart(WalletsViewModel _WalletsViewModel) {
            WalletsViewModel = _WalletsViewModel;
            List<Wallet> wallets = WalletsViewModel.Wallets;
            if (wallets != null)
            {
                walletsList.SeparatorVisibility = SeparatorVisibility.None;
                walletsList.ItemsSource = wallets;
                WalletsViewModel.TotalCost = 0;
                for (int i = 0; i < wallets.Count; i++)
                {
                    wallets[i].Cost = wallets[i].Amount * wallets[i].Price;
                    WalletsViewModel.TotalCost += wallets[i].Cost;
                }
            }

            var entries = new Microcharts.Entry[wallets.Count];
            for (int i = 0; i < wallets.Count; i++)
            {
                Random rnd = new Random();
                wallets[i].PercentInPortfolio = wallets[i].Cost / WalletsViewModel.TotalCost * 100;
                entries[i] = new Microcharts.Entry(wallets[i].PercentInPortfolio)
                {
                    Label = wallets[i].Name,
                    ValueLabel = string.Format("{0:f2}", wallets[i].Amount),
                    Color = SKColor.FromHsv(rnd.Next(256), rnd.Next(256), rnd.Next(256))
                };
            }

            portfolioChart.Chart = new CustomDonutChart() { Entries = entries, HoleRadius = 0.6f, LabelTextSize = 32, FontFamily = "Roboto" };

            walletsBalance.Text = string.Format("{0:f1} $", WalletsViewModel.TotalCost);
        }

        void Test(object sender, EventArgs args)
        {
            List<Wallet> newWallets = new List<Wallet>
            {
                new Wallet
                {
                    Name = "BTC",
                    FullName = "Bitcoin",
                    Amount = 1.99f,
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
            WalletsViewModel.Wallets = newWallets;
            UpdateChart(WalletsViewModel);
        }
    }
}
