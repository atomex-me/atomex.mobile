using System;
using Xamarin.Forms;
using SkiaSharp;
using atomex.ViewModel;
using atomex.CustomElements;
using System.Linq;
using System.Collections.Generic;

namespace atomex
{
    public partial class Portfolio : ContentPage
    {
        private CurrenciesViewModel _currenciesViewModel;

        private INavigationService _navigationService { get; }

        private readonly List<string> chartColors = new List<string>
        {
            "#a43604",
            "#eb8b35",
            "#cdbba3",
            "#dedfe4",
            "#161f21",
            "#492b22",
            "#af7e68",
            "#bcc1b7",
            "#acaead",
            "#f3f1ec",
        };

        public Portfolio()
        {
            InitializeComponent();
        }

        public Portfolio(CurrenciesViewModel currenciesViewModel, INavigationService navigationService)
        {
            InitializeComponent();
            _currenciesViewModel = currenciesViewModel;
            _navigationService = navigationService;

            _currenciesViewModel.QuotesUpdated += (s, a) =>
            {
                Device.BeginInvokeOnMainThread(UpdateChart);
            };

            BindingContext = currenciesViewModel;

            UpdateChart();
        }

        private void UpdateChart()
        {
            try
            {
                if (_currenciesViewModel.CurrencyViewModels != null)
                {
                    if (_currenciesViewModel.TotalCost == 0)
                    {
                        var entry = new Microcharts.Entry[]
                        {
                            new Microcharts.Entry(100) { Color = SKColor.Parse("#dcdcdc") }
                        };
                        PortfolioChart.Chart = new CustomDonutChart() { Entries = entry, HoleRadius = 0.6f, LabelTextSize = 20, BackgroundColor = SKColor.Parse("#f3f2f7"), FontFamily = "Roboto-Thin" };
                    }
                    else
                    {
                        var nonzeroWallets = _currenciesViewModel.CurrencyViewModels.Where(w => w.AvailableAmount != 0).ToList();
                        var entries = new Microcharts.Entry[nonzeroWallets.Count];
                        for (int i = 0; i < nonzeroWallets.Count; i++)
                        {
                            entries[i] = new Microcharts.Entry(nonzeroWallets[i].PortfolioPercent)
                            {
                                Label = nonzeroWallets[i].CurrencyCode,
                                ValueLabel = string.Format("{0:0.#} %", nonzeroWallets[i].PortfolioPercent),
                                Color = SKColor.Parse(chartColors[i])
                            };
                        }
                        
                        PortfolioChart.Chart = new CustomDonutChart() { Entries = entries, HoleRadius = 0.6f, LabelTextSize = 20, BackgroundColor = SKColor.Parse("#f3f2f7"), FontFamily = "Roboto-Thin" };
                    }
                }
            }
            catch (Exception e) { }
        }

        private void OnListViewItemTapped(object sender, ItemTappedEventArgs e)
        {
            if (e.Item != null)
            {
                _navigationService.ShowCurrency(e.Item as CurrencyViewModel);
            }
        }
    }
}