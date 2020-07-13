using System;
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
            _currenciesViewModel = currenciesViewModel;

            _currenciesViewModel.QuotesUpdated += (s, a) =>
            {
                Device.BeginInvokeOnMainThread(UpdateChart);   
            };

            BindingContext = currenciesViewModel;

            UpdateChart();
        }

        private void UpdateChart()
        {
            if (_currenciesViewModel.CurrencyViewModels != null)
            {
                if (_currenciesViewModel.TotalCost == 0)
                {
                    var entry = new Microcharts.Entry[]
                    {
                        new Microcharts.Entry(100)
                        {
                             Color = SKColor.Parse("#dcdcdc")
                        }
                    };

                    if (portfolioChart.Chart == null)
                        portfolioChart.Chart = new CustomDonutChart() { Entries = entry, HoleRadius = 0.6f, LabelTextSize = 26, FontFamily = "Roboto-Thin" };
                    else
                    {
                        var donutChart = portfolioChart.Chart as CustomDonutChart;
                        donutChart.Entries = entry;
                    }
                }
                else
                {
                    var nonzeroWallets = _currenciesViewModel.CurrencyViewModels.Where(w => w.AvailableAmount != 0).ToList();
                    var entries = new Microcharts.Entry[nonzeroWallets.Count];
                    for (int i = 0; i < nonzeroWallets.Count; i++)
                    {
                        Random rnd = new Random();
                        entries[i] = new Microcharts.Entry(nonzeroWallets[i].PortfolioPercent)
                        {
                            Label = nonzeroWallets[i].CurrencyCode,
                            ValueLabel = string.Format("{0:0.#} %", nonzeroWallets[i].PortfolioPercent),
                            Color = SKColor.FromHsv(rnd.Next(256), rnd.Next(256), rnd.Next(256))
                        };
                    }

                    if (portfolioChart.Chart == null)
                        portfolioChart.Chart = new CustomDonutChart() { Entries = entries, HoleRadius = 0.6f, LabelTextSize = 26, FontFamily = "Roboto-Thin" };
                    else
                    {
                        var donutChart = portfolioChart.Chart as CustomDonutChart;
                        donutChart.Entries = entries;
                    }
                }
            }
        }
    }
}
