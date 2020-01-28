using System;
using Xamarin.Forms;
using Xamarin.Essentials;
using atomex.ViewModel;

namespace atomex
{
    public partial class ReceivePage : ContentPage
    {
        private CurrencyViewModel _currency;

        public ReceivePage()
        {
            InitializeComponent();
        }

        public ReceivePage(CurrencyViewModel selectedCurrency)
        {
            InitializeComponent();
            _currency = selectedCurrency;
            BindingContext = _currency;
        }

        async void OnCopyButtonClicked(object sender, EventArgs args) {
            if (_currency != null)
            {
                await Clipboard.SetTextAsync(_currency.Address);
                await DisplayAlert("Адрес скопирован", _currency.Address, "OK");
            }
            else
            {
                await DisplayAlert("Ошибка", "Ошибка при копировании", "OK");
            }
        }

        async void OnShareButtonClicked(object sender, EventArgs args)
        {
            await Share.RequestAsync(new ShareTextRequest
            {
                Text = "Мой публичный адрес для получения " + _currency.Name + ":\r\n" + _currency.Address,
                Uri = _currency.Address,
                Title = "Address sharing"
            });
        }
    }
}
