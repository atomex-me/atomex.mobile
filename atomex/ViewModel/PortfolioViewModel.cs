using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using atomex.CustomElements;
using Atomex.Common;
using Microcharts;
using Serilog;
using SkiaSharp;
using Xamarin.Forms;

namespace atomex.ViewModel
{
    public class PortfolioViewModel : BaseViewModel
    {
        public INavigationService NavigationService { get; set; }

        public Chart PortfolioChart { get; private set; }

        private SKColor bgChartColor;

        private readonly List<string> chartColors = new List<string>
        {
            "#a43604",
            "#eb8b35",
            "#cdbba3",
            "#B8B6B9",
            "#492b22",
            "#972633",
            "#af7e68",
            "#213D68",
            "#5F4FA1",
            "#196457",
            "#9E7327",
            "#895700"
        };

        private decimal _portfolioValue;
        public decimal PortfolioValue
        {
            get => _portfolioValue;
            set { _portfolioValue = value; OnPropertyChanged(nameof(PortfolioValue)); }
        }

        private CurrenciesViewModel _currenciesViewModel;
        public CurrenciesViewModel CurrenciesViewModel
        {
            get => _currenciesViewModel;
            set { _currenciesViewModel = value; OnPropertyChanged(nameof(CurrenciesViewModel)); }
        }

        public PortfolioViewModel(CurrenciesViewModel currenciesViewModel, string appTheme)
        {
            CurrenciesViewModel = currenciesViewModel;

            string bgColorName = "AdditionalBackgroundColor";
            if (appTheme == OSAppTheme.Dark.ToString())
                bgColorName = "AdditionalBackgroundColorDark";

            if (Application.Current.Resources.TryGetValue(bgColorName, out var bgColor))
                SKColor.TryParse(bgColor.ToString(), out bgChartColor);

            SubscribeToUpdates();
        }

        private void SubscribeToUpdates()
        {
            CurrenciesViewModel.CurrencyViewModels.ForEach(c =>
            {
                c.AmountUpdated += OnAmountUpdatedEventHandler;
            });

            OnAmountUpdatedEventHandler(this, EventArgs.Empty);
        }

        private void OnAmountUpdatedEventHandler(object sender, EventArgs args)
        {
            // update total portfolio value
            PortfolioValue = CurrenciesViewModel.CurrencyViewModels.Sum(c => c.TotalAmountInBase);

            // update currency portfolio percent
            CurrenciesViewModel?.CurrencyViewModels
                .ForEachDo(c => c.PortfolioPercent = PortfolioValue != 0
                ? c.TotalAmountInBase / PortfolioValue
                : 0);

            UpdateChart();
        }

        private void UpdateChart()
        {
            try
            {
                if (PortfolioValue == 0)
                {
                    var entry = new Microcharts.Entry[]
                    {
                        new Microcharts.Entry(100) { Color = SKColor.Parse("#dcdcdc") }
                    };

                    PortfolioChart = new CustomDonutChart() { Entries = entry, HoleRadius = 0.6f, LabelTextSize = 20, BackgroundColor = bgChartColor, FontFamily = "Roboto-Bold" };
                }
                else
                {
                    var nonzeroWallets = CurrenciesViewModel.CurrencyViewModels.Where(w => w.TotalAmount != 0).ToList();
                    var entries = new Microcharts.Entry[nonzeroWallets.Count];
                    for (int i = 0; i < nonzeroWallets.Count; i++)
                    {
                        entries[i] = new Microcharts.Entry((float)nonzeroWallets[i].PortfolioPercent)
                        {
                            Label = nonzeroWallets[i].CurrencyCode,
                            TextColor = SKColor.Parse(chartColors[i]),
                            ValueLabel = string.Format("{0:P2}", nonzeroWallets[i].PortfolioPercent),
                            Color = SKColor.Parse(chartColors[i])
                        };
                    }

                    PortfolioChart = new CustomDonutChart() { Entries = entries, HoleRadius = 0.6f, LabelTextSize = 20, BackgroundColor = bgChartColor, FontFamily = "Roboto-Bold" };
                }

                OnPropertyChanged(nameof(PortfolioChart));
            }
            catch (Exception e)
            {
                Log.Error(e, "Update chart error");
            }
        }

        private ICommand _selectCurrencyCommand;
        public ICommand SelectCurrencyCommand => _selectCurrencyCommand ??= new Command<CurrencyViewModel>(async (value) => await OnCurrencyTapped(value));

        private async Task OnCurrencyTapped(CurrencyViewModel currency)
        {
            if (currency != null)
                await NavigationService.ShowCurrency(currency);
        }
    }
}
