using System;
using atomex.Resources;
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
            Amount.Placeholder = AppResources.AmountEntryPlaceholder + ", " + conversionViewModel.FromCurrencyViewModel.CurrencyCode;
            BindingContext = _conversionViewModel;
        }
        private void AmountEntryFocused(object sender, FocusEventArgs args)
        {
            InvalidAmountFrame.IsVisible = false;
            AmountFrame.HasShadow = args.IsFocused;
            if (!args.IsFocused)
            {
                if (String.IsNullOrWhiteSpace(Amount.Text))
                {
                    _conversionViewModel.Amount = 0;
                    return;
                }
                _conversionViewModel.Amount = Convert.ToDecimal(Amount.Text);
            }
        }

        private void OnAmountTextChanged(object sender, TextChangedEventArgs args)
        {
            if (!String.IsNullOrEmpty(args.NewTextValue))
            {
                if (!AmountHint.IsVisible)
                {
                    AmountHint.IsVisible = true;
                    AmountHint.Text = Amount.Placeholder;
                    Amount.VerticalTextAlignment = TextAlignment.Start;
                }
                _conversionViewModel.Amount = decimal.Parse(args.NewTextValue);
            }
            else
            {
                AmountHint.IsVisible = false;
                _conversionViewModel.Amount = 0;
                Amount.VerticalTextAlignment = TextAlignment.Center;
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
                await DisplayAlert(AppResources.Warning, AppResources.EnterAmountLabel, AppResources.AcceptButton);
                return;
            }

            decimal amount = Convert.ToDecimal(Amount.Text);

            if (amount <= 0)
            {
                InvalidAmountFrame.IsVisible = true;
                InvalidAmountLabel.Text = AppResources.ErrorZeroAmount + _conversionViewModel.FromCurrencyViewModel.CurrencyCode;
                return;
            }
            if (amount > _conversionViewModel.MaxAmount)
            {
                InvalidAmountFrame.IsVisible = true;
                InvalidAmountLabel.Text = AppResources.InsufficientFunds;
                return;
            }
            if (_conversionViewModel.IsNoLiquidity)
            {
                await DisplayAlert(AppResources.Warning, AppResources.ErrorNoLiquidity, AppResources.AcceptButton);
                return;
            }
            await Navigation.PushAsync(new ConfirmationPage(_conversionViewModel));
        }
    }
}
