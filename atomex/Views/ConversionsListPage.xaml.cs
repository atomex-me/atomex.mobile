using System;
using System.Collections.Generic;
using atomex.ViewModel;
using Atomex;
using Xamarin.Forms;

namespace atomex
{
    public partial class ConversionsListPage : ContentPage
    {
        private CurrencyViewModel _currencyViewModel;

        private ConversionViewModel _conversionViewModel;

        private IAtomexApp _app;

        public ConversionsListPage()
        {
            InitializeComponent();
        }

        public ConversionsListPage(IAtomexApp app, CurrencyViewModel currencyViewModel, ConversionViewModel conversionViewModel)
        {
            InitializeComponent();
            if (currencyViewModel != null)
            {
                _app = app;
                _currencyViewModel = currencyViewModel;
                _conversionViewModel = conversionViewModel;
                BindingContext = _currencyViewModel;
            }
        }
        private async void OnListViewItemTapped(object sender, ItemTappedEventArgs e)
        {
            if (e.Item != null)
            {
                //await Navigation.PushAsync(new TransactionInfoPage(e.Item as TransactionViewModel));
            }
        }
        private async void OnCreateSwapButtonClicked(object sender, EventArgs args)
        {
            await Navigation.PushAsync(new ConversionFirstStepPage(_app, _conversionViewModel));
        }
    }
}
