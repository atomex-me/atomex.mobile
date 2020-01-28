using System;
using Xamarin.Forms;
using atomex.ViewModel;
using Xamarin.Essentials;

namespace atomex
{
    public partial class SendPage : ContentPage
    {
        private CurrencyViewModel _currency;

        public SendPage()
        {
            InitializeComponent();
        }

        public SendPage(CurrencyViewModel selectedCurrency)
        {
            InitializeComponent();
            if (selectedCurrency != null)
            {
                _currency = selectedCurrency;
                BindingContext = _currency;
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
            if (_currency != null)
            {
                Amount.Text = _currency.Amount.ToString();
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
                var transaction = new Transaction();
                transaction.To = Address.Text;
                
               
                transaction.Amount = float.Parse(Amount.Text);
                await Navigation.PushAsync(new AcceptSendPage(_currency, transaction));
            }
        }


    }
}
