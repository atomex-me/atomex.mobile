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

        public void AmountEntryFocused(object sender, FocusEventArgs e)
        {
            //e.VisualElement.Focus();
            InvalidAmountFrame.IsVisible = false;
        }

        public void AddressEntryFocused(object sender, FocusEventArgs e)
        {
            //e.VisualElement.Focus();
            InvalidAddressFrame.IsVisible = false;
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
            decimal amount = Convert.ToDecimal(Amount.Text);
            if (String.IsNullOrWhiteSpace(Address.Text) || String.IsNullOrWhiteSpace(Amount.Text))
            {
                await DisplayAlert("Warning", "Все поля должны быть заполнены", "OK");
            }
            else if (!currencyViewModel.Currency.IsValidAddress(Address.Text)) {
                InvalidAddressFrame.IsVisible = true;
            }
            else if (amount <= 0)
            {
                InvalidAmountFrame.IsVisible = true;
                InvalidAmountLabel.Text = "Amount must be greater than 0 " + currencyViewModel.Name;
            }
            else if (amount > currencyViewModel.Amount)
            {
                InvalidAmountFrame.IsVisible = true;
                InvalidAmountLabel.Text = "Insufficient funds";
            }
            else
            {
                await Navigation.PushAsync(new AcceptSendPage(currencyViewModel, Address.Text, Amount.Text));
            }
        }
    }
}
