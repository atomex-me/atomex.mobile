using System;
using System.Collections.Generic;
using atomex.ViewModel;
using Atomex;
using Xamarin.Forms;

namespace atomex
{
    public partial class ConversionsListPage : ContentPage
    {

        private ConversionViewModel _conversionViewModel;

        private IAtomexApp _app;

        public ConversionsListPage()
        {
            InitializeComponent();
        }

        public ConversionsListPage(IAtomexApp app, ConversionViewModel conversionViewModel)
        {
            InitializeComponent();
            if (conversionViewModel != null)
            {
                _app = app;
                _conversionViewModel = conversionViewModel;
                BindingContext = conversionViewModel;
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
