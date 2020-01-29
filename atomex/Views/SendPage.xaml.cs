using System;
using Xamarin.Forms;
using atomex.ViewModel;
using Xamarin.Essentials;

namespace atomex
{
    public partial class SendPage : ContentPage
    {
        private CurrencyViewModel currencyViewModel;

        public SendPage()
        {
            InitializeComponent();
        }

        public SendPage(CurrencyViewModel selectedCurrency)
        {
            InitializeComponent();
            if (selectedCurrency != null)
            {
                currencyViewModel = selectedCurrency;
                BindingContext = currencyViewModel;
            }
        }

        public void EntryFocused(object sender, FocusEventArgs e)
        {
            //e.VisualElement.Focus();

            //var entry = sender as Entry;
            //if (entry != null)
            //{
            //    //entry.
            //}
        }

        void OnSetMaxAmountButtonClicked(object sender, EventArgs args) {
            if (currencyViewModel != null)
            {
                Amount.Text = currencyViewModel.Amount.ToString();
            }
        }

        async void OnScanButtonClicked(object sender, EventArgs args) {

            var optionsPage = new ScanningQrPage(selected =>
            {
                Address.Text = selected;
            });

            await Navigation.PushAsync(optionsPage);
        }

        async void OnPasteButtonClicked(object sender, EventArgs args) {
            if (Clipboard.HasText)
            {
                var text = await Clipboard.GetTextAsync();
                Address.Text = text;
            }
            else
            {
                await DisplayAlert("Ошибка", "Буфер обмена пуст", "OK");
            }
        }

        async void OnNextButtonClicked(object sender, EventArgs args)
        {
            if (String.IsNullOrWhiteSpace(Address.Text) || String.IsNullOrWhiteSpace(Amount.Text))
            {
                await DisplayAlert("Warning", "Все поля должны быть заполнены", "OK");
            }
            else
            {
                await Navigation.PushAsync(new AcceptSendPage(currencyViewModel, Address.Text, Amount.Text));
            }
        }


    }
}
