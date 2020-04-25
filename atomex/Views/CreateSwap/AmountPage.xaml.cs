using System;
using atomex.ViewModel;
using Xamarin.Forms;

namespace atomex.Views.CreateSwap
{
    public partial class AmountPage : ContentPage
    {

        private ConversionViewModel _conversionViewModel;

        public AmountPage()
        {
            InitializeComponent();
        }
        public AmountPage(ConversionViewModel conversionViewModel)
        {
            InitializeComponent();
            _conversionViewModel = conversionViewModel;
            Amount.Placeholder = "Amount, " + conversionViewModel.FromCurrencyViewModel.CurrencyCode;
            BindingContext = _conversionViewModel;
        }
        private void AmountEntryFocused(object sender, FocusEventArgs e)
        {
            InvalidAmountFrame.IsVisible = false;
            AmountFrame.HasShadow = true;
        }

        private void AmountEntryUnfocused(object sender, FocusEventArgs e)
        {
            AmountFrame.HasShadow = false;
            if (String.IsNullOrWhiteSpace(Amount.Text))
            {
                _conversionViewModel.Amount = 0;
                return;
            }

            _conversionViewModel.Amount = Convert.ToDecimal(Amount.Text);
        }

        private void OnAmountTextChanged(object sender, TextChangedEventArgs args)
        {
            if (!String.IsNullOrEmpty(args.NewTextValue))
            {
                if (!AmountHint.IsVisible)
                {
                    AmountHint.IsVisible = true;
                    AmountHint.Text = Amount.Placeholder;
                }
                _conversionViewModel.Amount = decimal.Parse(args.NewTextValue);
            }
            else
            {
                AmountHint.IsVisible = false;
                _conversionViewModel.Amount = 0;
            }
        }

        private void OnSetMaxAmountButtonClicked(object sender, EventArgs args)
        {
            InvalidAmountFrame.IsVisible = false;
            Amount.Text = _conversionViewModel.MaxAmount.ToString();
            _conversionViewModel.Amount = _conversionViewModel.MaxAmount;
        }
        private async void OnNextButtonClicked(object sender, EventArgs args)
        {
            if (String.IsNullOrWhiteSpace(Amount.Text))
            {
                await DisplayAlert("Warning", "Enter amount", "Ok");
                return;
            }

            decimal amount = Convert.ToDecimal(Amount.Text);

            if (amount <= 0)
            {
                InvalidAmountFrame.IsVisible = true;
                InvalidAmountLabel.Text = "Amount must be greater than 0 " + _conversionViewModel.FromCurrencyViewModel.CurrencyCode;
                return;
            }
            if (amount > _conversionViewModel.MaxAmount)
            {
                InvalidAmountFrame.IsVisible = true;
                InvalidAmountLabel.Text = "Insufficient funds";
                return;
            }
            if (_conversionViewModel.IsNoLiquidity)
            {
                await DisplayAlert("Warning", "Not enough liquidity to convert a specified amount", "Ok");
                return;
            }
            await Navigation.PushAsync(new ConfirmationPage(_conversionViewModel));
        }
    }
}
