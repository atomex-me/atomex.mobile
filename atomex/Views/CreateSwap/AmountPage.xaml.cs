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
            //if (!args.IsFocused)
            //{
            //    try
            //    {
            //        decimal.TryParse(Amount.Text?.Replace(",", "."), NumberStyles.Any, CultureInfo.InvariantCulture, out decimal amount);
            //        //BlockActions(true);
            //        await _conversionViewModel.UpdateAmountAsync(amount);
            //        //BlockActions(false);
            //        Amount.Text = _conversionViewModel.Amount.ToString();
            //    }
            //    catch (FormatException)
            //    {
            //        _conversionViewModel.Amount = 0;
            //        Amount.Text = "0";
            //    }
            //}
        }

        private void AmountEntryTapped(object sender, EventArgs args)
        {
            Amount.Focus();
            //Amount.CursorPosition = _conversionViewModel.Amount.ToString().Length;
        }

        private async void OnAmountTextChanged(object sender, TextChangedEventArgs args)
        {
            if (!String.IsNullOrEmpty(args.NewTextValue))
            {
                if (!AmountHint.IsVisible)
                {
                    AmountHint.IsVisible = true;
                    //AmountHint.Text = Amount.Placeholder;

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

        //private async void OnMaxAmountButtonClicked(object sender, EventArgs args)
        //{
        //    MaxButton.IsVisible = MaxButton.IsEnabled = false;
        //    Loader.IsRunning = Loader.IsVisible = true;
        //    BlockActions(true);
        //    await _conversionViewModel.OnMaxClick();
        //    BlockActions(false);
        //    Amount.Text = _conversionViewModel.Amount.ToString();
        //    Loader.IsRunning = Loader.IsVisible = false;
        //    MaxButton.IsVisible = MaxButton.IsEnabled = true;
        //}

        //private void BlockActions(bool flag)
        //{
        //    FeeCalculationLoader.IsVisible = FeeCalculationLoader.IsRunning = flag;
            
        //    if (flag)
        //        Content.Opacity = 0.5;
        //    else
        //        Content.Opacity = 1;
        //}
    }
}
