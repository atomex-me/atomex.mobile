using Xamarin.Forms;
using System;
using System.Linq;
using atomex.ViewModels.CurrencyViewModels;

namespace atomex.Views
{
    public partial class CurrencyPage : ContentPage
    {
        public CurrencyPage(CurrencyViewModel currencyViewModel)
        {
            InitializeComponent();
            BindingContext = currencyViewModel;
        }
        
        protected override void OnAppearing()
        {
            var context = (CurrencyViewModel) BindingContext;
            var tab = context
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