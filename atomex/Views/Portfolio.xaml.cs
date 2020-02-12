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

            //walletsViewModel.WalletsUpdated += (s, a) =>
            //{
            //    Device.BeginInvokeOnMainThread(UpdateChart);
            //};

            UpdateChart();
        }

        private void UpdateChart()
        {
            if (_currenciesViewModel.Currencies != null)
            {
                if (_currenciesViewModel.TotalCost == 0)
                {
                    portfolioChart.IsVisible = false;
                }
                else
                {
                    portfolioChart.IsVisible = true;
                    var nonzeroWallets = _currenciesViewModel.Currencies.Where(w => w.AvailableAmount != 0).ToList();
                    var entries = new Microcharts.Entry[nonzeroWallets.Count];
                    for (int i = 0; i < nonzeroWallets.Count; i++)
                    {
                        Random rnd = new Random();
                        entries[i] = new Microcharts.Entry(nonzeroWallets[i].PortfolioPercent)
                        {
                            Label = nonzeroWallets[i].Name,
                            ValueLabel = string.Format("{0:f2}", nonzeroWallets[i].AvailableAmount),
                            Color = SKColor.FromHsv(rnd.Next(256), rnd.Next(256), rnd.Next(256))
                        };
                    }
                    portfolioChart.Chart = new CustomDonutChart() { Entries = entries, HoleRadius = 0.6f, LabelTextSize = 32, FontFamily = "Roboto" };
                }
            }
        }
    }
}
