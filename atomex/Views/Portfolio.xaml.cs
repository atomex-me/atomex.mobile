using System;
using System.Collections.Generic;
using Xamarin.Forms;
using atomex.Models;
using SkiaSharp;
using atomex.ViewModel;
using atomex.CustomElements;
using System.Linq;

namespace atomex
{
    public partial class Portfolio : ContentPage
    {
        private WalletsViewModel WalletsViewModel;

        public Portfolio()
        {
            InitializeComponent();
        }

        public Portfolio(WalletsViewModel walletsViewModel)
        {
            InitializeComponent();
            walletsList.SeparatorVisibility = SeparatorVisibility.None;
            WalletsViewModel = walletsViewModel;

            walletsViewModel.QuotesUpdated += (s, a) =>
            {
                Device.BeginInvokeOnMainThread(UpdateChart);   
            };

            //walletsViewModel.WalletsUpdated += (s, a) =>
            //{
            //    Device.BeginInvokeOnMainThread(UpdateChart);
            //};

            UpdateChart();
        }

        private void UpdateChart() {
            List<Wallet> wallets = WalletsViewModel.Wallets;
            if (wallets != null)
            {
                walletsList.ItemsSource = wallets;
                walletsBalance.Text = string.Format("{0:f1} $", WalletsViewModel.TotalCost);
                if (WalletsViewModel.TotalCost == 0)
                {
                    portfolioChart.IsVisible = false;
                }
                else
                {
                    portfolioChart.IsVisible = true;
                    var nonzeroWallets = wallets.Where(w => w.Amount != 0).ToList();
                    var entries = new Microcharts.Entry[nonzeroWallets.Count];
                    for (int i = 0; i < nonzeroWallets.Count; i++)
                    {
                        Random rnd = new Random();
                        entries[i] = new Microcharts.Entry(nonzeroWallets[i].PercentInPortfolio)
                        {
                            Label = nonzeroWallets[i].Name,
                            ValueLabel = string.Format("{0:f2}", nonzeroWallets[i].Amount),
                            Color = SKColor.FromHsv(rnd.Next(256), rnd.Next(256), rnd.Next(256))
                        };
                    }
                    portfolioChart.Chart = new CustomDonutChart() { Entries = entries, HoleRadius = 0.6f, LabelTextSize = 32, FontFamily = "Roboto" };
                }
            }
        }
    }
}
