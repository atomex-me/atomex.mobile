using System;
using System.Threading.Tasks;
using atomex.ViewModel;
using Xamarin.Forms;

namespace atomex.Views.CreateSwap
{
    public partial class AmountPage : ContentPage
    {

        public AmountPage()
        {
            InitializeComponent();
        }
        public AmountPage(ConversionViewModel conversionViewModel)
        {
            InitializeComponent();
            BindingContext = conversionViewModel;
        }
        private void AmountEntryFocused(object sender, FocusEventArgs args)
        {
            AmountFrame.HasShadow = args.IsFocused;
        }

        private void AmountEntryTapped(object sender, EventArgs args)
        {
            Amount.Focus();
            if (!string.IsNullOrEmpty(Amount.Text))
                Amount.CursorPosition = Amount.Text.Length;
        }

        private async void OnAmountTextChanged(object sender, TextChangedEventArgs args)
        {
            if (!String.IsNullOrEmpty(args.NewTextValue))
            {
                if (!AmountHint.IsVisible)
                {
                    AmountHint.IsVisible = true;

                    _ = AmountHint.FadeTo(1, 500, Easing.Linear);
                    _ = Amount.TranslateTo(0, 10, 500, Easing.CubicOut);
                    _ = AmountHint.TranslateTo(0, -15, 500, Easing.CubicOut);
                }
            }
            else
            {
                await Task.WhenAll(
                    AmountHint.FadeTo(0, 500, Easing.Linear),
                    Amount.TranslateTo(0, 0, 500, Easing.CubicOut),
                    AmountHint.TranslateTo(0, -10, 500, Easing.CubicOut)
                );
         
                AmountHint.IsVisible = false;
            }
        }
    }
}
