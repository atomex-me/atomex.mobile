using Xamarin.Forms;
using atomex.ViewModel.CurrencyViewModels;
using System;

namespace atomex
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
            await TabScrollView.ScrollToAsync((VisualElement)sender, ScrollToPosition.Center, true);
        }
        private async void ScrollToEnd(object sender, EventArgs e)
        {
            await TabScrollView.ScrollToAsync((VisualElement)sender, ScrollToPosition.End, true);
        }
    }
}