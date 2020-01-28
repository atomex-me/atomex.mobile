using System;
using System.Collections.Generic;
using Xamarin.Forms;
using SkiaSharp;
using atomex.ViewModel;
using atomex.CustomElements;
using System.Linq;

namespace atomex
{
    public partial class Portfolio : ContentPage
    {
        private CurrenciesViewModel _currenciesViewModel;

        public Portfolio()
        {
            InitializeComponent();
        }

        public Portfolio(CurrenciesViewModel currenciesViewModel)
        {
            InitializeComponent();
            currenciesList.SeparatorVisibility = SeparatorVisibility.None;
            _currenciesViewModel = currenciesViewModel;

            _currenciesViewModel.QuotesUpdated += (s, a) =>
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
            List<CurrencyViewModel> currencies = _currenciesViewModel.Currencies;
            if (currencies != null)
            {
                currenciesList.ItemsSource = currencies;
                walletsBalance.Text = string.Format("{0:f1} $", _currenciesViewModel.TotalCost);
                if (_currenciesViewModel.TotalCost == 0)
                {
                    portfolioChart.IsVisible = false;
                }
                else
                {
                    portfolioChart.IsVisible = true;
                    var nonzeroWallets = currencies.Where(w => w.Amount != 0).ToList();
                    var entries = new Microcharts.Entry[nonzeroWallets.Count];
                    for (int i = 0; i < nonzeroWallets.Count; i++)
                    {
                        Random rnd = new Random();
                        entries[i] = new Microcharts.Entry(nonzeroWallets[i].PortfolioPercent)
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
