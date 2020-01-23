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
                    var entries = new Microcharts.Entry[wallets.Count];
                    for (int i = 0; i < wallets.Count; i++)
                    {
                        Random rnd = new Random();
                        entries[i] = new Microcharts.Entry(wallets[i].PercentInPortfolio)
                        {
                            Label = wallets[i].Name,
                            ValueLabel = string.Format("{0:f2}", wallets[i].Amount),
                            Color = SKColor.FromHsv(rnd.Next(256), rnd.Next(256), rnd.Next(256))
                        };
                    }
                    portfolioChart.Chart = new CustomDonutChart() { Entries = entries, HoleRadius = 0.6f, LabelTextSize = 32, FontFamily = "Roboto" };
                }
            }
        }
    }
}
