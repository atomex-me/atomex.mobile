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
                walletsList.SeparatorVisibility = SeparatorVisibility.None;
                walletsList.ItemsSource = wallets;
                WalletsViewModel.TotalCost = 0;
                for (int i = 0; i < wallets.Count; i++)
                {
                    WalletsViewModel.TotalCost += wallets[i].Cost;
                }
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
                        wallets[i].PercentInPortfolio = wallets[i].Cost / WalletsViewModel.TotalCost * 100;
                        //if (WalletsViewModel.TotalCost != 0)
                        //{
                        //    wallets[i].PercentInPortfolio = wallets[i].Cost / WalletsViewModel.TotalCost * 100;
                        //}
                        //else
                        //{
                        //    wallets[i].PercentInPortfolio = 0;
                        //}
                        entries[i] = new Microcharts.Entry((float)wallets[i].PercentInPortfolio)
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
