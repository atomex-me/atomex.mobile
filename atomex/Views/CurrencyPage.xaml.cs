using Xamarin.Forms;
using System;
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


        private async void ScrollToCenter(object sender, EventArgs e)
        {
            await TabScrollView.ScrollToAsync((VisualElement) sender, ScrollToPosition.Center, true);
        }

        private async void ScrollToEnd(object sender, EventArgs e)
        {
            await TabScrollView.ScrollToAsync((VisualElement) sender, ScrollToPosition.End, true);
        }

        private async void ScrollToSearch(object sender, EventArgs e)
        {
            await Page.ScrollToAsync((VisualElement) sender, ScrollToPosition.Start, true);
        }
    }
}