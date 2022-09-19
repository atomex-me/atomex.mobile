using Xamarin.Forms;
using System;
using System.Linq;
using atomex.ViewModels.CurrencyViewModels;

namespace atomex.Views
{
    public partial class CurrencyPage : ContentPage
    {
        private readonly CurrencyViewModel _currencyViewModel;
        public CurrencyPage(CurrencyViewModel currencyViewModel)
        {
            InitializeComponent();
            BindingContext = currencyViewModel;
            _currencyViewModel = currencyViewModel;
        }
        
        protected override void OnAppearing()
        {
            var tab = _currencyViewModel
                .SelectedTab
                .ToString(); 
            var button = this.FindByName<Button>(tab);
            if (button == null) return;
            
            ScrollTo(button);
        }

        private void ScrollToActiveTab(object sender, EventArgs e)
        {
            var button = sender as VisualElement;
            ScrollTo(button);
        }

        private async void ScrollTo(VisualElement element)
        {
            var lastElement = TabButtons
                .Children
                .Where(t => t.IsVisible)
                ?.LastOrDefault();
            
            await TabScrollView.ScrollToAsync(
                element, 
                element?.Id == lastElement?.Id 
                    ? ScrollToPosition.End
                    : ScrollToPosition.Center, 
                true);
        }

        private async void ScrollToSearch(object sender, EventArgs e)
        {
            await Page.ScrollToAsync((VisualElement) sender, ScrollToPosition.Start, true);
        }
    }
}