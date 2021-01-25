using System;
using Xamarin.Forms;
using SkiaSharp;
using atomex.ViewModel;
using atomex.CustomElements;
using System.Linq;
using System.Collections.Generic;
using Serilog;

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
            "#B8B6B9",
            "#161f21",
            "#492b22",
            "#af7e68",
            "#bcc1b7",
            "#acaead",
            "#5F4FA1"
        };

        private SKColor bgChartColor;

        public Portfolio()
        {
            InitializeComponent();
        }

        public Portfolio(CurrenciesViewModel currenciesViewModel, INavigationService navigationService)
        {
            InitializeComponent();
            _currenciesViewModel = currenciesViewModel;
            _navigationService = navigationService;

            string bgColorName = "AdditionalBackgroundColor";
            if (Application.Current.RequestedTheme == OSAppTheme.Dark)
                bgColorName = "AdditionalBackgroundColorDark";

            if (Application.Current.Resources.TryGetValue(bgColorName, out var bgColor))
                SKColor.TryParse(bgColor.ToString(), out bgChartColor);

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
                    if (_currenciesViewModel.TotalAmountInBase == 0)
                    {
                        
                        var entry = new Microcharts.Entry[]
                        {
                            new Microcharts.Entry(100) { Color = SKColor.Parse("#dcdcdc") }
                        };
                        PortfolioChart.Chart = new CustomDonutChart() { Entries = entry, HoleRadius = 0.6f, LabelTextSize = 20, BackgroundColor = bgChartColor, FontFamily = "Roboto-Bold" };
                    }
                    else
                    {
                        var nonzeroWallets = _currenciesViewModel.CurrencyViewModels.Where(w => w.TotalAmount != 0).ToList();
                        var entries = new Microcharts.Entry[nonzeroWallets.Count];
                        for (int i = 0; i < nonzeroWallets.Count; i++)
                        {
                            entries[i] = new Microcharts.Entry(nonzeroWallets[i].PortfolioPercent)
                            {
                                Label = nonzeroWallets[i].CurrencyCode,
                                TextColor = SKColor.Parse(chartColors[i]),
                                ValueLabel = string.Format("{0:0.#} %", nonzeroWallets[i].PortfolioPercent),
                                Color = SKColor.Parse(chartColors[i])
                            };
                        }
                        PortfolioChart.Chart = new CustomDonutChart() { Entries = entries, HoleRadius = 0.6f, LabelTextSize = 20, BackgroundColor = bgChartColor, FontFamily = "Roboto-Bold" };
                    }
                }
            }
            catch (Exception e)
            {
                Log.Error(e, "Update chart error");
            }
        }

        private void ItemSelected(object sender, SelectionChangedEventArgs e)
        {
            if (e.CurrentSelection.Count > 0)
                _navigationService.ShowCurrency(e.CurrentSelection.First() as CurrencyViewModel);
            var collectionView = sender as CollectionView;
            if (collectionView != null)
                collectionView.SelectedItem = null;
            ////VisualStateManager.GoToState((Grid)sender, "UnSelected");
        }
    }
}